using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;

using Serilog;

using System.Diagnostics;
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
            ILogger<Program> logger,
            TelemetryClient telemetryClient) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var loginEvent = new EventTelemetry("UserLogin");
                loginEvent.Properties["Username"] = loginDto.Username;
                loginEvent.Properties["UserAgent"] = httpContext.Request.Headers.UserAgent.ToString();
                loginEvent.Properties["IPAddress"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                try
                {
                    logger.LogInformation("Login attempt for user: {Username}", loginDto.Username);

                    var user = await context.Users
                        .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                    if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                    {
                        stopwatch.Stop();
                        loginEvent.Properties["Success"] = "false";
                        loginEvent.Properties["FailureReason"] = user == null ? "UserNotFound" : "InvalidPassword";
                        loginEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                        telemetryClient.TrackEvent(loginEvent);
                        
                        logger.LogInformation("Password verification result: Failed for user {Username}", loginDto.Username);
                        return Results.Unauthorized();
                    }

                    logger.LogInformation("Password verification successful for user {Username}", loginDto.Username);
                    
                    // Track token generation time
                    var tokenStopwatch = Stopwatch.StartNew();
                    var token = await tokenService.GenerateToken(user: user);
                    tokenStopwatch.Stop();

                    logger.LogInformation("Generated token: {TokenPreview}", token?[..Math.Min(10, token.Length)]);

                    // Generate refresh token
                    var refreshTokenStopwatch = Stopwatch.StartNew();
                    var refreshToken = await tokenService.GenerateRefreshTokenAsync(
                        user.Id, 
                        httpContext.Request.Headers.UserAgent.ToString());
                    refreshTokenStopwatch.Stop();

                    // Set refresh token as httpOnly cookie
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(30) // 30 day expiry
                    };
                    httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

                    stopwatch.Stop();
                    loginEvent.Properties["Success"] = "true";
                    loginEvent.Properties["UserId"] = user.Id.ToString();
                    loginEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                    loginEvent.Metrics["TokenGenerationDuration"] = tokenStopwatch.ElapsedMilliseconds;
                    loginEvent.Metrics["RefreshTokenGenerationDuration"] = refreshTokenStopwatch.ElapsedMilliseconds;
                    telemetryClient.TrackEvent(loginEvent);

                    // Track successful login metric
                    telemetryClient.TrackMetric("Authentication.Login.Success", 1, 
                        new Dictionary<string, string> { ["Username"] = loginDto.Username });

                    logger.LogInformation("Successfully logged in user {Username} and set refresh token", loginDto.Username);

                    return Results.Ok(new { Token = token });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    loginEvent.Properties["Success"] = "false";
                    loginEvent.Properties["FailureReason"] = "Exception";
                    loginEvent.Properties["ExceptionMessage"] = ex.Message;
                    loginEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                    telemetryClient.TrackEvent(loginEvent);
                    telemetryClient.TrackException(ex);
                    throw;
                }
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
        ITokenService tokenService,
        TelemetryClient telemetryClient)
    {
        var stopwatch = Stopwatch.StartNew();
        var tokenEvent = new EventTelemetry("TokenGeneration");
        
        try
        {
            var username = context.User.Identity?.Name;
            tokenEvent.Properties["Username"] = username ?? "Anonymous";
            tokenEvent.Properties["UserAgent"] = context.Request.Headers.UserAgent.ToString();

            if (string.IsNullOrEmpty(username))
            {
                stopwatch.Stop();
                tokenEvent.Properties["Success"] = "false";
                tokenEvent.Properties["FailureReason"] = "NoUsername";
                tokenEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                telemetryClient?.TrackEvent(tokenEvent);
                return Results.Unauthorized();
            }

            var token = await tokenService.GenerateToken(username: username);
            
            stopwatch.Stop();
            tokenEvent.Properties["Success"] = "true";
            tokenEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
            telemetryClient?.TrackEvent(tokenEvent);
            
            telemetryClient?.TrackMetric("Authentication.TokenGeneration.Success", 1,
                new Dictionary<string, string> { ["Username"] = username });

            return Results.Ok(new { token });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            tokenEvent.Properties["Success"] = "false";
            tokenEvent.Properties["FailureReason"] = "Exception";
            tokenEvent.Properties["ExceptionMessage"] = ex.Message;
            tokenEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
            telemetryClient?.TrackEvent(tokenEvent);
            telemetryClient?.TrackException(ex);
            throw;
        }
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
