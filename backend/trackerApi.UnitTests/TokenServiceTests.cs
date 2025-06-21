using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Moq;
using trackerApi.Services;
using trackerApi.Models;
using System.Diagnostics.CodeAnalysis;

namespace trackerApi.UnitTests;

[ExcludeFromCodeCoverage]
public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly Mock<IUserService> _mockUserService;
    private readonly TokenService _tokenService;
    private readonly User _testUser;

    public TokenServiceTests()
    {
        // Setup configuration
        var initialData = new[]
        {
            new KeyValuePair<string, string?>("JwtSettings:Issuer", "test-issuer"),
            new KeyValuePair<string, string?>("JwtSettings:Audience", "test-audience"),
            new KeyValuePair<string, string?>("JwtSettings:SecretKey", "your-test-secret-key-minimum-256-bits-long"),
            new KeyValuePair<string, string?>("JwtSettings:ExpirationInMinutes", "60")
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();

        // Setup mock user service
        _mockUserService = new Mock<IUserService>();

        // Initialize TokenService with both dependencies
        _tokenService = new TokenService(_configuration, _mockUserService.Object);

        // Initialize test user with all required properties
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = "hashedpassword123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Setup mock user service to return test user when queried
        _mockUserService
            .Setup(s => s.GetUserByUsername("testuser"))
            .ReturnsAsync(_testUser);
    }

    [Fact]
    public async Task GenerateToken_WithUser_ReturnsValidLoginToken()
    {
        // Act
        var token = await _tokenService.GenerateToken(user: _testUser);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify claims
        var claims = jwtToken.Claims.ToList();
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier &&
                                    c.Value == _testUser.Id.ToString());
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name &&
                                    c.Value == _testUser.Username);

        // Verify token properties
        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Contains("test-audience", jwtToken.Audiences);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_WithUsername_ReturnsValidRefreshToken()
    {
        // Act
        var token = await _tokenService.GenerateToken(username: "testuser");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify claims
        var claims = jwtToken.Claims.ToList();
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier &&
                                    c.Value == _testUser.Id.ToString());
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name &&
                                    c.Value == "testuser");
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Iat);

        // Verify token properties
        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Contains("test-audience", jwtToken.Audiences);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);

        // Verify the user service was called
        _mockUserService.Verify(s => s.GetUserByUsername("testuser"), Times.Once);
    }

    [Fact]
    public async Task GenerateToken_WithNoUserOrUsername_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tokenService.GenerateToken());

        Assert.Equal("Either user or username must be provided", exception.Message);
    }

    [Fact]
    public async Task GenerateToken_ValidatesExpiration()
    {
        // Arrange
        var customConfigData = new[]
        {
            new KeyValuePair<string, string?>("JwtSettings:Issuer", "test-issuer"),
            new KeyValuePair<string, string?>("JwtSettings:Audience", "test-audience"),
            new KeyValuePair<string, string?>("JwtSettings:SecretKey", "your-test-secret-key-minimum-256-bits-long"),
            new KeyValuePair<string, string?>("JwtSettings:ExpirationInMinutes", "30")
        };

        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(customConfigData)
            .Build();

        var tokenService = new TokenService(customConfig, _mockUserService.Object);

        // Act
        var token = await tokenService.GenerateToken(user: _testUser);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expectedExpiration = DateTime.UtcNow.AddMinutes(30);
        Assert.True(Math.Abs((expectedExpiration - jwtToken.ValidTo).TotalMinutes) < 1);
    }

    [Fact]
    public async Task GenerateToken_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var tokenService = new TokenService(emptyConfig, _mockUserService.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            tokenService.GenerateToken(user: _testUser));

        Assert.Equal("JwtSettings:SecretKey not found in appsettings.json", exception.Message);
    }

    [Fact]
    public async Task GenerateToken_WithNonexistentUsername_ThrowsArgumentException()
    {
        // Arrange
        _mockUserService
            .Setup(s => s.GetUserByUsername("nonexistent"))
            .ReturnsAsync((User?)null);  // Explicitly cast null to User?

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _tokenService.GenerateToken(username: "nonexistent"));

        Assert.Equal("User not found", exception.Message);
    }

}
