using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;
using System;
using System.Linq;
using System.Collections.Generic;
using trackerApi.Services;
using trackerApi.Models;

namespace trackerApi.UnitTests;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;
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

        _tokenService = new TokenService(_configuration);

        // Initialize test user with all required properties
        _testUser = new User
        {
            Id = Guid.NewGuid(), // This would be set automatically, but we're being explicit
            Username = "testuser",
            PasswordHash = "hashedpassword123", // In real scenario this would be an actual hash
            CreatedAt = DateTime.UtcNow, // This would be set automatically
            UpdatedAt = DateTime.UtcNow  // This would be set automatically
        };
    }

    [Fact]
    public void GenerateToken_WithUser_ReturnsValidLoginToken()
    {
        // Act
        var token = _tokenService.GenerateToken(user: _testUser);

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
    public void GenerateToken_WithUsername_ReturnsValidRefreshToken()
    {
        // Act
        var token = _tokenService.GenerateToken(username: "testuser");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify claims
        var claims = jwtToken.Claims.ToList();
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name &&
                                    c.Value == "testuser");
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Iat);

        // Verify token properties
        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Contains("test-audience", jwtToken.Audiences);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_WithNoUserOrUsername_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _tokenService.GenerateToken());

        Assert.Equal("Either user or username must be provided", exception.Message);
    }

    [Fact]
    public void GenerateToken_ValidatesExpiration()
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

        var tokenService = new TokenService(customConfig);

        // Act
        var token = tokenService.GenerateToken(user: _testUser);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var expectedExpiration = DateTime.UtcNow.AddMinutes(30);
        Assert.True(Math.Abs((expectedExpiration - jwtToken.ValidTo).TotalMinutes) < 1);
    }

    [Fact]
    public void GenerateToken_WithInvalidConfiguration_ThrowsException()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var tokenService = new TokenService(emptyConfig);

        // Act & Assert
        var exception = Assert.Throws<KeyNotFoundException>(() =>
            tokenService.GenerateToken(user: _testUser));

        Assert.Equal("JwtSettings:SecretKey not found in appsettings.json", exception.Message);
    }

}
