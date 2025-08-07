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

                // Find the refresh token in database
                var dbLookupStopwatch = Stopwatch.StartNew();
                var storedToken = await context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);
                dbLookupStopwatch.Stop();

                if (storedToken == null || storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    stopwatch.Stop();
                    refreshEvent.Properties["Success"] = "false";
                    refreshEvent.Properties["FailureReason"] = storedToken == null ? "TokenNotFound" : "TokenExpired";
                    refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                    refreshEvent.Metrics["DatabaseLookupDuration"] = dbLookupStopwatch.ElapsedMilliseconds;
                    telemetryClient.TrackEvent(refreshEvent);
                    
                    logger.LogWarning("Invalid or expired refresh token");
                    return Results.Unauthorized();
                }

                refreshEvent.Properties["UserId"] = storedToken.UserId.ToString();
                logger.LogInformation("Valid refresh token found for user: {UserId}", storedToken.UserId);

                // Generate new access token
                var accessTokenStopwatch = Stopwatch.StartNew();
                var newAccessToken = await tokenService.GenerateToken(user: storedToken.User);
                accessTokenStopwatch.Stop();

                // Generate new refresh token
                var newRefreshTokenStopwatch = Stopwatch.StartNew();
                var newRefreshToken = await tokenService.GenerateRefreshTokenAsync(
                    storedToken.UserId, 
                    httpContext.Request.Headers.UserAgent.ToString());
                newRefreshTokenStopwatch.Stop();

                // Revoke old refresh token
                var revokeStopwatch = Stopwatch.StartNew();
                await tokenService.RevokeRefreshTokenAsync(refreshToken);
                revokeStopwatch.Stop();

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
                refreshEvent.Metrics["Duration"] = stopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["DatabaseLookupDuration"] = dbLookupStopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["AccessTokenGenerationDuration"] = accessTokenStopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["RefreshTokenGenerationDuration"] = newRefreshTokenStopwatch.ElapsedMilliseconds;
                refreshEvent.Metrics["TokenRevokeDuration"] = revokeStopwatch.ElapsedMilliseconds;
                telemetryClient.TrackEvent(refreshEvent);

                telemetryClient.TrackMetric("Authentication.TokenRefresh.Success", 1,
                    new Dictionary<string, string> { ["UserId"] = storedToken.UserId.ToString() });

                logger.LogInformation("Successfully refreshed tokens for user: {UserId}", storedToken.UserId);

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