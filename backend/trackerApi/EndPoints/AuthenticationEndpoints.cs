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

                        var failureReason = user == null ? "UserNotFound" : "InvalidPassword";
                        loginEvent.Properties["Success"] = "false";
                        loginEvent.Properties["FailureReason"] = failureReason;
                        loginEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;

                        telemetryClient.TrackEvent(loginEvent);
                        
                        logger.LogInformation("Password verification result: Failed for user {Username}", loginDto.Username);
                        
                        // Enhanced error response
                        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                            ?? Guid.NewGuid().ToString();
                        
                        var errorResponse = new ErrorResponse
                        {
                            Error = "AUTHENTICATION_FAILED",
                            Message = "Invalid username or password",
                            StatusCode = 401,
                            CorrelationId = correlationId,
                            Timestamp = DateTime.Now,
                            SuggestedAction = "Please check your username and password and try again."
                        };
                        
                        return Results.Json(errorResponse, statusCode: StatusCodes.Status401Unauthorized);
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
                    var refreshCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.Now.AddDays(30) // 30 day expiry
                    };
                    httpContext.Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);

                    // Set access token as httpOnly cookie (30 days to match JWT)
                    var accessCookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.Now.AddDays(30) // Match JWT expiration
                    };
                    httpContext.Response.Cookies.Append("accessToken", token!, accessCookieOptions);

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
                    
                    logger.LogError(ex, "Login error for user {Username}", loginDto.Username);
                    
                    // Enhanced error response for exceptions
                    var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                        ?? Guid.NewGuid().ToString();
                    
                    var errorResponse = new ErrorResponse
                    {
                        Error = "INTERNAL_SERVER_ERROR",
                        Message = "An error occurred during login.",
                        Details = ex.Message,
                        StatusCode = 500,
                        CorrelationId = correlationId,
                        Timestamp = DateTime.Now,
                        SuggestedAction = "Please try again later or contact support if the issue persists."
                    };
                    
                    return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
        .WithName("Login")
        .WithOpenApi();

        // see GenerateToken() method below
        app.MapPost("/api/v1/auth/token", GenerateToken)
            .WithName("GenerateToken")
            .WithOpenApi();

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
                
                var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                    ?? Guid.NewGuid().ToString();
                    
                var errorResponse = new ErrorResponse
                {
                    Error = "UNAUTHORIZED",
                    Message = "User authentication required",
                    StatusCode = 401,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.Now,
                    SuggestedAction = "Please login to obtain an access token"
                };
                
                return Results.Json(errorResponse, statusCode: StatusCodes.Status401Unauthorized);
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
            
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString();
            
            var errorResponse = new ErrorResponse
            {
                Error = "TOKEN_GENERATION_ERROR",
                Message = "Failed to generate authentication token.",
                Details = ex.Message,
                StatusCode = 500,
                CorrelationId = correlationId,
                Timestamp = DateTime.Now,
                SuggestedAction = "Please try again or contact support if the issue persists."
            };
            
            return Results.Json(errorResponse, statusCode: StatusCodes.Status500InternalServerError);
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
