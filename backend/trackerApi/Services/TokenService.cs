using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using trackerApi.Models;

namespace trackerApi.Services;

/// <summary>
/// // For initial login:
/// var loginToken = tokenService.GenerateToken(user: user);
/// 
/// For refresh token:
/// var refreshToken = tokenService.GenerateToken(username: existingUsername);
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User? user = null, string? username = null, bool isRefreshToken = false)
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
            // Claims for refresh token
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
