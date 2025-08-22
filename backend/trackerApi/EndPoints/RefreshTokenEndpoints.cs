using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using trackerApi.DbContext;
using trackerApi.Services;

namespace trackerApi.EndPoints;

public static class RefreshTokenEndpoints
{
    public static void MapRefreshTokenEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/auth/refresh", async (
            HttpContext httpContext,
            AppDbContext context,
            ITokenService tokenService,
            ILogger<Program> logger,
            TelemetryClient telemetryClient) =>
        {
            var stopwatch = Stopwatch.StartNew();
            var refreshEvent = new EventTelemetry("TokenRefresh");
            refreshEvent.Properties["UserAgent"] = httpContext.Request.Headers.UserAgent.ToString();
            refreshEvent.Properties["IPAddress"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            try
            {
                logger.LogInformation("Refresh token request received");

                // Get refresh token from httpOnly cookie
                if (!httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || 
                    string.IsNullOrEmpty(refreshToken))
                {
                    stopwatch.Stop();
                    refreshEvent.Properties["Success"] = "false";
                    refreshEvent.Properties["FailureReason"] = "NoRefreshTokenCookie";
                    refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                    telemetryClient.TrackEvent(refreshEvent);
                    
                    logger.LogWarning("No refresh token found in cookies");
                    return Results.Unauthorized();
                }

                // Find the refresh token and user data in single optimized query
                var dbLookupStopwatch = Stopwatch.StartNew();
                var tokenData = await context.RefreshTokens
                    .Where(rt => rt.Token == refreshToken && !rt.IsRevoked)
                    .Select(rt => new {
                        rt.Id,
                        rt.UserId,
                        rt.ExpiresAt,
                        rt.Token,
                        UserUsername = rt.User!.Username
                    })
                    .FirstOrDefaultAsync();
                dbLookupStopwatch.Stop();

                if (tokenData == null || tokenData.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    stopwatch.Stop();
                    refreshEvent.Properties["Success"] = "false";
                    refreshEvent.Properties["FailureReason"] = tokenData == null ? "TokenNotFound" : "TokenExpired";
                    refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                    refreshEvent.Metrics["DatabaseLookupDuration"] = dbLookupStopwatch.ElapsedMilliseconds;
                    telemetryClient.TrackEvent(refreshEvent);
                    
                    logger.LogWarning("Invalid or expired refresh token");
                    return Results.Unauthorized();
                }

                refreshEvent.Properties["Username"] = tokenData.UserUsername;
                logger.LogInformation("Valid refresh token found for user: {Username}", tokenData.UserUsername);

                // Generate new tokens and perform refresh token rotation in single transaction
                var tokenOperationStopwatch = Stopwatch.StartNew();
                var newAccessToken = await tokenService.GenerateTokenFromUserData(
                    tokenData.UserId, 
                    tokenData.UserUsername);
                
                var newRefreshToken = await tokenService.RotateRefreshTokenAsync(
                    tokenData.Id,
                    tokenData.UserId, 
                    httpContext.Request.Headers.UserAgent.ToString());
                tokenOperationStopwatch.Stop();

                // Set new refresh token as httpOnly cookie
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7) // 7 day expiry
                };
                httpContext.Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

                stopwatch.Stop();
                refreshEvent.Properties["Success"] = "true";
                refreshEvent.Properties["Username"] = tokenData.UserUsername;
                refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["DatabaseLookupDuration"] = dbLookupStopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["TokenOperationDuration"] = tokenOperationStopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["DatabaseOperations"] = 2; // 1 SELECT + 1 Transaction (INSERT + UPDATE)
                telemetryClient.TrackEvent(refreshEvent);

                telemetryClient.TrackMetric("Authentication.TokenRefresh.Success", 1,
                    new Dictionary<string, string> { ["Username"] = tokenData.UserUsername });

                logger.LogInformation("Successfully refreshed tokens for user: {Username} (Optimized: 2 DB operations)", tokenData.UserUsername);

                return Results.Ok(new { Token = newAccessToken });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                refreshEvent.Properties["Success"] = "false";
                refreshEvent.Properties["FailureReason"] = "Exception";
                refreshEvent.Properties["ExceptionMessage"] = ex.Message;
                refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                telemetryClient.TrackEvent(refreshEvent);
                telemetryClient.TrackException(ex);
                throw;
            }
        })
        .WithName("RefreshToken")
        .WithOpenApi();

        app.MapPost("/api/v1/auth/revoke", async (
            HttpContext httpContext,
            ITokenService tokenService,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Token revocation request received");

            // Get refresh token from httpOnly cookie
            if (!httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || 
                string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("No refresh token found in cookies for revocation");
                return Results.BadRequest("No refresh token found");
            }

            // Revoke the refresh token
            await tokenService.RevokeRefreshTokenAsync(refreshToken);

            // Clear the refresh token cookie
            httpContext.Response.Cookies.Delete("refreshToken");

            logger.LogInformation("Successfully revoked refresh token");

            return Results.Ok(new { Message = "Token revoked successfully" });
        })
        .WithName("RevokeToken")
        .WithOpenApi();

        app.MapPost("/api/v1/auth/revoke-all", async (
            HttpContext httpContext,
            ITokenService tokenService,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Revoke all tokens request received");

            // Get current user ID from JWT claims
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("No valid user ID found in token claims");
                return Results.Unauthorized();
            }

            // Revoke all refresh tokens for the user
            await tokenService.RevokeAllUserRefreshTokensAsync(userId);

            // Clear the refresh token cookie
            httpContext.Response.Cookies.Delete("refreshToken");

            logger.LogInformation("Successfully revoked all tokens for user: {UserId}", userId);

            return Results.Ok(new { Message = "All tokens revoked successfully" });
        })
        .RequireAuthorization()
        .WithName("RevokeAllTokens")
        .WithOpenApi();
    }
}