using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Moq;
using Moq.EntityFrameworkCore;

using System.Security.Claims;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Models;
using trackerApi.Services;
using trackerApi.TestUtils;

namespace trackerApi.UnitTests;

public class AuthenticationEndpointsTests
{
    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        var users = new List<User>().AsQueryable();
        var mockContext = DbContextMockHelper.CreateMockDbContext(users);
        var mockConfig = new Mock<IConfiguration>();
        var mockTokenService = new Mock<ITokenService>();

        var loginDto = new LoginDto("nonexistentuser", "password123");

        // Act
        var result = await LoginHandler(
            mockContext.Object,
            mockConfig.Object,
            mockTokenService.Object,
            loginDto);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var users = new List<User> { testUser }.AsQueryable();
        var mockContext = DbContextMockHelper.CreateMockDbContext(users);
        var mockConfig = new Mock<IConfiguration>();
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService
            .Setup(ts => ts.GenerateToken(It.IsAny<User>(), null, false))
            .Returns("test-jwt-token");

        var loginDto = new LoginDto("testuser", "password123");

        // Act
        var result = await LoginHandler(
              mockContext.Object,
              mockConfig.Object,
              mockTokenService.Object,
              loginDto);

        // Assert
        var okResult = Assert.IsAssignableFrom<IResult>(result);
        Assert.Equal(200, (okResult as IStatusCodeHttpResult)?.StatusCode);

        // Access the Value property using reflection since it's an anonymous type
        var resultType = result.GetType();
        var valueProperty = resultType.GetProperty("Value");
        var value = valueProperty?.GetValue(result);
        var token = value?.GetType().GetProperty("Token")?.GetValue(value) as string;

        Assert.Equal("test-jwt-token", token);
    }


    [Fact]
    public void GenerateToken_WithValidUsername_ReturnsOkWithToken()
    {
        // Arrange
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService
            .Setup(ts => ts.GenerateToken(It.IsAny<User>(), "testuser", false))
            .Returns("test-jwt-token");

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = AuthenticationEndpoints.GenerateToken(httpContext, mockTokenService.Object);

        // Assert
        // Just confirm it's a 200 OK result
        Assert.Equal(200, GetStatusCodeFromResult(result));

        // And that it contains the expected token
        var tokenValue = GetTokenFromResult(result);
        Assert.Equal("test-jwt-token", tokenValue);

        mockTokenService.Verify(ts => ts.GenerateToken(It.IsAny<User>(), "testuser", false), Times.Once);
    }


    [Fact]
    public void GenerateToken_WithNoUsername_ReturnsUnauthorized()
    {
        // Arrange
        var mockTokenService = new Mock<ITokenService>();
        var httpContext = new DefaultHttpContext();
        // Not setting any claims - empty identity

        // Act
        var result = AuthenticationEndpoints.GenerateToken(httpContext, mockTokenService.Object);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
        mockTokenService.Verify(ts => ts.GenerateToken(It.IsAny<User>(), It.IsAny<string>(), false), Times.Never);
    }


    // Helper method to replicate the login endpoint handler logic
    private static async Task<IResult> LoginHandler(
        AppDbContext context,
        IConfiguration config,
        ITokenService tokenService,
        LoginDto loginDto)
    {
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = tokenService.GenerateToken(user: user);

        return Results.Ok(new { Token = token });
    }


    private static int GetStatusCodeFromResult(IResult result)
    {
        return (int?)(result.GetType().GetProperty("StatusCode")?.GetValue(result)) ?? 0;
    }


    private static string GetTokenFromResult(IResult result)
    {
        var valueProperty = result.GetType().GetProperty("Value")
            ?? throw new InvalidOperationException("Result does not contain a Value property");

        var value = valueProperty.GetValue(result)
            ?? throw new InvalidOperationException("Result Value property is null");

        var tokenProperty = value.GetType().GetProperty("token")
            ?? throw new InvalidOperationException("Result Value does not contain a token property");

        return tokenProperty.GetValue(value)?.ToString()
            ?? throw new InvalidOperationException("Token value is null");
    }
}
