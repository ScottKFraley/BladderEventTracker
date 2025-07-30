using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

namespace trackerApi.UnitTests;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly Mock<IUserService> _mockUserService;
    private readonly TokenService _tokenService;
    private readonly User _testUser;
    private bool _disposed = false;

    public RefreshTokenServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock logger for AppDbContext
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<AppDbContext>>();
        _dbContext = new AppDbContext(options, mockLogger.Object);

        // Setup configuration
        var initialData = new[]
        {
            new KeyValuePair<string, string?>("JwtSettings:Issuer", "test-issuer"),
            new KeyValuePair<string, string?>("JwtSettings:Audience", "test-audience"),
            new KeyValuePair<string, string?>("JwtSettings:SecretKey", "your-test-secret-key-minimum-256-bits-long-for-testing-purposes"),
            new KeyValuePair<string, string?>("JwtSettings:ExpirationInMinutes", "60")
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        // Setup mock user service
        _mockUserService = new Mock<IUserService>();

        // Initialize TokenService
        _tokenService = new TokenService(_configuration, _mockUserService.Object, _dbContext);

        // Initialize test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = "hashedpassword123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsAdmin = false
        };

        // Add test user to database
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();

        // Setup mock user service
        _mockUserService
            .Setup(s => s.GetUserByUsername(_testUser.Username))
            .ReturnsAsync(_testUser);
    }

    #region GenerateRefreshTokenAsync Tests

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithValidUser_ReturnsToken()
    {
        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);

        // Verify token was stored in database
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        Assert.NotNull(storedToken);
        Assert.Equal(_testUser.Id, storedToken.UserId);
        Assert.False(storedToken.IsRevoked);
        Assert.True(storedToken.ExpiresAt > DateTimeOffset.UtcNow.AddDays(29));
        Assert.True(storedToken.ExpiresAt <= DateTimeOffset.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithDeviceInfo_StoresDeviceInfo()
    {
        // Arrange
        var deviceInfo = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id, deviceInfo);

        // Assert
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        Assert.NotNull(storedToken);
        Assert.Equal(deviceInfo, storedToken.DeviceInfo);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_MultipleTokens_GeneratesUniqueTokens()
    {
        // Act
        var token1 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var token2 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var token3 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token1, token3);
        Assert.NotEqual(token2, token3);

        // Verify all tokens are stored
        var storedTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == _testUser.Id)
            .ToListAsync();

        Assert.Equal(3, storedTokens.Count);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithInvalidUserId_StillCreatesToken()
    {
        // Arrange
        var invalidUserId = Guid.NewGuid();

        // Act
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(invalidUserId);

        // Assert - Token should still be created even if user doesn't exist
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == invalidUserId);

        Assert.NotNull(storedToken);
        Assert.Equal(invalidUserId, storedToken.UserId);
    }

    #endregion

    #region ValidateRefreshTokenAsync Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var expiredToken = new RefreshToken
        {
            UserId = _testUser.Id,
            Token = "expired-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Expired yesterday
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync(expiredToken.Token, _testUser.Id);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithRevokedToken_ReturnsFalse()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            UserId = _testUser.Id,
            Token = "revoked-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = true // Revoked
        };

        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync(revokedToken.Token, _testUser.Id);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithWrongUser_ReturnsFalse()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync(refreshToken, otherUserId);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNonExistentToken_ReturnsFalse()
    {
        // Act
        var isValid = await _tokenService.ValidateRefreshTokenAsync("non-existent-token", _testUser.Id);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNullOrEmptyToken_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(null!, _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync("", _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync("   ", _testUser.Id));
    }

    #endregion

    #region RevokeRefreshTokenAsync Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_RevokesToken()
    {
        // Arrange
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Verify token is initially valid
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));

        // Act
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));

        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        Assert.NotNull(storedToken);
        Assert.True(storedToken.IsRevoked);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistentToken_DoesNotThrow()
    {
        // Act & Assert - Should not throw exception
        await _tokenService.RevokeRefreshTokenAsync("non-existent-token");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNullOrEmptyToken_DoesNotThrow()
    {
        // Act & Assert - Should not throw exceptions
        await _tokenService.RevokeRefreshTokenAsync(null!);
        await _tokenService.RevokeRefreshTokenAsync("");
        await _tokenService.RevokeRefreshTokenAsync("   ");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithAlreadyRevokedToken_RemainsRevoked()
    {
        // Arrange
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        // Act - Revoke again
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        // Assert
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        Assert.NotNull(storedToken);
        Assert.True(storedToken.IsRevoked);
    }

    #endregion

    #region RevokeAllUserRefreshTokensAsync Tests

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_WithMultipleTokens_RevokesAllTokens()
    {
        // Arrange
        var token1 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var token2 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var token3 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Verify tokens are initially valid
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(token1, _testUser.Id));
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(token2, _testUser.Id));
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(token3, _testUser.Id));

        // Act
        await _tokenService.RevokeAllUserRefreshTokensAsync(_testUser.Id);

        // Assert
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(token1, _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(token2, _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(token3, _testUser.Id));

        var userTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == _testUser.Id)
            .ToListAsync();

        Assert.All(userTokens, token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_WithNoTokens_DoesNotThrow()
    {
        // Arrange
        var userWithNoTokens = Guid.NewGuid();

        // Act & Assert - Should not throw exception
        await _tokenService.RevokeAllUserRefreshTokensAsync(userWithNoTokens);
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_WithMixedTokens_OnlyRevokesUserTokens()
    {
        // Arrange
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "otheruser",
            PasswordHash = "hashedpassword456",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsAdmin = false
        };

        _dbContext.Users.Add(otherUser);
        await _dbContext.SaveChangesAsync();

        var userToken1 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var userToken2 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var otherUserToken = await _tokenService.GenerateRefreshTokenAsync(otherUser.Id);

        // Act
        await _tokenService.RevokeAllUserRefreshTokensAsync(_testUser.Id);

        // Assert
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(userToken1, _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(userToken2, _testUser.Id));
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(otherUserToken, otherUser.Id));
    }

    [Fact]
    public async Task RevokeAllUserRefreshTokensAsync_WithAlreadyRevokedTokens_DoesNotChangeState()
    {
        // Arrange
        var token1 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);
        var token2 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Revoke one token manually
        await _tokenService.RevokeRefreshTokenAsync(token1);

        // Act
        await _tokenService.RevokeAllUserRefreshTokensAsync(_testUser.Id);

        // Assert
        var userTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == _testUser.Id)
            .ToListAsync();

        Assert.All(userTokens, token => Assert.True(token.IsRevoked));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RefreshTokenWorkflow_CompleteScenario_WorksCorrectly()
    {
        // Generate refresh token
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id, "Test Browser");

        // Validate the token
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));

        // Generate another token for the same user
        var refreshToken2 = await _tokenService.GenerateRefreshTokenAsync(_testUser.Id);

        // Both should be valid
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(refreshToken2, _testUser.Id));

        // Revoke the first token
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        // First should be invalid, second should still be valid
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));
        Assert.True(await _tokenService.ValidateRefreshTokenAsync(refreshToken2, _testUser.Id));

        // Revoke all tokens for the user
        await _tokenService.RevokeAllUserRefreshTokensAsync(_testUser.Id);

        // Both should now be invalid
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(refreshToken, _testUser.Id));
        Assert.False(await _tokenService.ValidateRefreshTokenAsync(refreshToken2, _testUser.Id));
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _dbContext?.Dispose();
            }
            _disposed = true;
        }
    }
}