using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using trackerApi.DbContext;
using trackerApi.Models;

namespace trackerApi.Services;

/// <summary>
/// // For initial login:
/// var loginToken = tokenService.GenerateToken(user: user);
/// 
/// TODO: Verify the next line of code
/// For refresh token:
/// var refreshToken = tokenService.GenerateToken(username: existingUsername);
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly AppDbContext _dbContext;

    public TokenService(
        IConfiguration configuration, 
        IUserService userService,
        AppDbContext dbContext)
    {
        _configuration = configuration;
        _userService = userService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates JWT token using User entity (for login scenarios)
    /// </summary>
    public async Task<string> GenerateToken(User? user = null, string? username = null, bool isRefreshToken = false)
    {
        GetSigningCredentials(_configuration, out IConfigurationSection jwtSettings, out SigningCredentials credentials);

        var claims = new List<Claim>();

        if (user != null)
        {
            // Claims for initial login token
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, user.Username));
        }
        else if (!string.IsNullOrEmpty(username))
        {
            // We need to look up the user by username to get their ID
            var userFromDb = await _userService.GetUserByUsername(username) ?? throw new ArgumentException("User not found");

            // Claims for refresh token - now including the NameIdentifier
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userFromDb.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, username));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64));
        }
        else
        {
            throw new ArgumentException("Either user or username must be provided");
        }

        // ... rest of the existing code ...
        var expirationMinutes = double.Parse(
            jwtSettings["ExpirationInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Optimized JWT generation for refresh scenarios - no additional DB calls
    /// </summary>
    public Task<string> GenerateTokenFromUserData(Guid userId, string username)
    {
        GetSigningCredentials(_configuration, out IConfigurationSection jwtSettings, out SigningCredentials credentials);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var expirationMinutes = double.Parse(
            jwtSettings["ExpirationInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    /// <summary>
    /// Optimized refresh token rotation: INSERT new + UPDATE old in single transaction
    /// </summary>
    public async Task<string> RotateRefreshTokenAsync(Guid oldTokenId, Guid userId, string? deviceInfo = null)
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var newRefreshToken = Convert.ToBase64String(randomBytes);

        // Use transaction to ensure atomicity (if supported by provider)
        var isInMemoryDatabase = _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        
        if (isInMemoryDatabase)
        {
            // In-memory database doesn't support transactions, do operations sequentially
            // Create new refresh token entity
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = newRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(30), // 30 days expiration
                CreatedAt = DateTimeOffset.UtcNow,
                DeviceInfo = deviceInfo,
                IsRevoked = false
            };

            // INSERT new token
            _dbContext.RefreshTokens.Add(refreshTokenEntity);
            
            // UPDATE old token to revoked
            var oldToken = await _dbContext.RefreshTokens.FindAsync(oldTokenId);
            if (oldToken != null)
            {
                oldToken.IsRevoked = true;
            }

            await _dbContext.SaveChangesAsync();
            return newRefreshToken;
        }
        else
        {
            // Use transaction for real databases
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Create new refresh token entity
                var refreshTokenEntity = new RefreshToken
                {
                    UserId = userId,
                    Token = newRefreshToken,
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(30), // 30 days expiration
                    CreatedAt = DateTimeOffset.UtcNow,
                    DeviceInfo = deviceInfo,
                    IsRevoked = false
                };

                // INSERT new token
                _dbContext.RefreshTokens.Add(refreshTokenEntity);
                
                // UPDATE old token to revoked (more efficient than separate query)
                await _dbContext.RefreshTokens
                    .Where(rt => rt.Id == oldTokenId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.IsRevoked, true));

                // Commit both operations
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return newRefreshToken;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string? deviceInfo = null)
    {
        // Generate a cryptographically secure random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        // Create refresh token entity
        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30), // 30 days expiration
            CreatedAt = DateTimeOffset.UtcNow,
            DeviceInfo = deviceInfo,
            IsRevoked = false
        };

        // Store in database
        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var tokenEntity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (tokenEntity == null)
            return false;

        // Check if token is expired or revoked
        return !tokenEntity.IsRevoked && tokenEntity.ExpiresAt > DateTimeOffset.UtcNow;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;

        var tokenEntity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (tokenEntity != null)
        {
            tokenEntity.IsRevoked = true;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId)
    {
        var userTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
        }

        if (userTokens.Any())
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    private static void GetSigningCredentials(IConfiguration config, out IConfigurationSection jwtSettings, out SigningCredentials credentials)
    {
        jwtSettings = config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new KeyNotFoundException("JwtSettings:SecretKey not found in appsettings.json");
        }

        var data = Encoding.UTF8.GetBytes(secretKey);
        var key = new SymmetricSecurityKey(data);
        credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }
}
