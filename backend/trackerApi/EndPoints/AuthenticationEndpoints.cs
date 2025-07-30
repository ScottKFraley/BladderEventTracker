using Microsoft.EntityFrameworkCore;

using Serilog;

using System.Security.Cryptography;

using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

namespace trackerApi.EndPoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/login", async (
            HttpContext httpContext,
            AppDbContext context,
            IConfiguration config,
            ITokenService tokenService,
            LoginDto loginDto,
            ILogger<Program> logger) =>
            {
                logger.LogInformation("Login attempt for user: {Username}", loginDto.Username);

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    logger.LogInformation("Password verification result: Failed for user {Username}", loginDto.Username);
                    return Results.Unauthorized();
                }

                logger.LogInformation("Password verification successful for user {Username}", loginDto.Username);
                var token = await tokenService.GenerateToken(user: user);

                logger.LogInformation("Generated token: {TokenPreview}", token?[..Math.Min(10, token.Length)]);

                // Generate refresh token
                var refreshToken = await tokenService.GenerateRefreshTokenAsync(
                    user.Id, 
                    httpContext.Request.Headers.UserAgent.ToString());

                // Set refresh token as httpOnly cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30) // 30 day expiry
                };
                httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

                logger.LogInformation("Successfully logged in user {Username} and set refresh token", loginDto.Username);

                return Results.Ok(new { Token = token });
            })
        .WithName("Login")
        .WithOpenApi();

        // see GenerateToken() method below
        app.MapPost("/api/v1/auth/token", GenerateToken)
            .WithName("GenerateToken")
            .WithOpenApi();

        //app.MapPost("/api/v1/auth/register", async (
        //            AppDbContext context,
        //            RegisterDto registerDto) =>
        //{
        //    if (awaitcontext.Users.AnyAsync(u => u.Username == registerDto.Username))
        //    {
        //        returnResults.BadRequest("Username already exists");
        //    }
        //    varuser = newUser
        //            {
        //        Username = registerDto.Username,
        //        PasswordHash = HashPassword(registerDto.Password)
        //    };
        //    context.Users.Add(user);
        //    awaitcontext.SaveChangesAsync();
        //    returnResults.Created($"/api/users/{user.Id}", user);
        //})
        //        .WithName("Register")
        //        .WithOpenApi();

    } // end public static void MapAuthEndpoints()

    /// <summary>
    /// Generates a new JWT.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tokenService"></param>
    /// <remarks>
    /// This is a Minimal API 'handler' method.
    /// </remarks>
    /// <returns>
    /// An IResult instance containg a StatusCodes value.
    /// </returns>
    public static async Task<IResult> GenerateToken(
        HttpContext context,
        ITokenService tokenService)
    {
        var username = context.User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            return Results.Unauthorized();
        }

        var token = await tokenService.GenerateToken(username: username);

        return Results.Ok(new { token });
    }

    /// <summary>
    /// Verifies a password against a stored SHA256 hash.
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="storedHash">The stored SHA256 hash in uppercase hex format</param>
    /// <returns>True if the password matches the hash</returns>
    private static bool VerifyPassword(string password, string storedHash)
    {
        var inputHash = HashPassword(password);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hashes a password using SHA256 and returns the uppercase hex string.
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>The SHA256 hash as an uppercase hex string</returns>
    private static string HashPassword(string password)
    {
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hashBytes = SHA256.HashData(passwordBytes);
        return Convert.ToHexString(hashBytes);
    }
}
