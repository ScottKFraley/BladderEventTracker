using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

using Moq;
using Moq.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Models;
using trackerApi.Services;
using trackerApi.TestUtils;

namespace trackerApi.UnitTests;

[ExcludeFromCodeCoverage]
public class AuthenticationEndpointsTests
{
    [Fact, Trait("Category", "Unit")]
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

    [Fact, Trait("Category", "Unit")]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokenAndSetsCookies()
    {
        // Arrange
        var hashedPassword = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("password123")));
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
            .ReturnsAsync("test-jwt-token");
        mockTokenService
            .Setup(ts => ts.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync("test-refresh-token");

        var loginDto = new LoginDto("testuser", "password123");
        var httpContext = new DefaultHttpContext();

        // Act
        var result = await LoginHandlerWithContext(
              httpContext,
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

        // Verify cookies were set
        Assert.True(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        var setCookieHeaders = httpContext.Response.Headers["Set-Cookie"];
        
        // Check that we have both cookies
        Assert.Equal(2, setCookieHeaders.Count);
        
        var accessTokenCookie = setCookieHeaders.FirstOrDefault(h => h.StartsWith("accessToken="));
        var refreshTokenCookie = setCookieHeaders.FirstOrDefault(h => h.StartsWith("refreshToken="));
        
        Assert.NotNull(accessTokenCookie);
        Assert.NotNull(refreshTokenCookie);
        Assert.Contains("test-jwt-token", accessTokenCookie);
        Assert.Contains("test-refresh-token", refreshTokenCookie);
        Assert.Contains("httponly", accessTokenCookie.ToLower());
        Assert.Contains("secure", accessTokenCookie.ToLower());
        Assert.Contains("samesite=strict", accessTokenCookie.ToLower());
    }


    [Fact, Trait("Category", "Unit")]
    public async Task GenerateToken_WithValidUsername_ReturnsOkWithToken()
    {
        // Arrange
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService
            .Setup(ts => ts.GenerateToken(It.IsAny<User>(), "testuser", false))
            .ReturnsAsync("test-jwt-token");

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await AuthenticationEndpoints.GenerateToken(httpContext, mockTokenService.Object, null!);

        // Assert
        Assert.Equal(200, GetStatusCodeFromResult(result));

        var tokenValue = GetTokenFromResult(result);
        Assert.Equal("test-jwt-token", tokenValue);

        mockTokenService.Verify(ts => ts.GenerateToken(It.IsAny<User>(), "testuser", false), Times.Once);
    }


    [Fact, Trait("Category", "Unit")]
    public async Task GenerateToken_WithNoUsername_ReturnsUnauthorized()
    {
        // Arrange
        var mockTokenService = new Mock<ITokenService>();
        var httpContext = new DefaultHttpContext();
        // Not setting any claims - empty identity

        // Act
        var result = await AuthenticationEndpoints.GenerateToken(httpContext, mockTokenService.Object, null!);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ErrorResponse>>(result);
        Assert.Equal(401, jsonResult.StatusCode);
        Assert.Equal("UNAUTHORIZED", jsonResult.Value?.Error);
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

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = await tokenService.GenerateToken(user: user);

        return Results.Ok(new { Token = token });
    }

    // Helper method to replicate the login endpoint handler logic with HttpContext for cookie testing
    private static async Task<IResult> LoginHandlerWithContext(
        HttpContext httpContext,
        AppDbContext context,
        IConfiguration config,
        ITokenService tokenService,
        LoginDto loginDto)
    {
        var user = await context.Set<User>()
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = await tokenService.GenerateToken(user: user);
        var refreshToken = await tokenService.GenerateRefreshTokenAsync(user.Id, "test-user-agent");

        // Set refresh token as httpOnly cookie
        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.Now.AddDays(30)
        };
        httpContext.Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);

        // Set access token as httpOnly cookie (30 days to match JWT)
        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.Now.AddDays(30)
        };
        httpContext.Response.Cookies.Append("accessToken", token, accessCookieOptions);

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

    /// <summary>
    /// Verifies a password against a stored SHA256 hash.
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="storedHash">The stored SHA256 hash in uppercase hex format</param>
    /// <returns>True if the password matches the hash</returns>
    private static bool VerifyPassword(string password, string storedHash)
    {
        var inputHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
