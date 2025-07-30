using Microsoft.EntityFrameworkCore;
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
            ILogger<Program> logger) =>
        {
            logger.LogInformation("Refresh token request received");

            // Get refresh token from httpOnly cookie
            if (!httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || 
                string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("No refresh token found in cookies");
                return Results.Unauthorized();
            }

            // Find the refresh token in database
            var storedToken = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                logger.LogWarning("Invalid or expired refresh token");
                return Results.Unauthorized();
            }

            logger.LogInformation("Valid refresh token found for user: {UserId}", storedToken.UserId);

            // Generate new access token
            var newAccessToken = await tokenService.GenerateToken(user: storedToken.User);

            // Generate new refresh token
            var newRefreshToken = await tokenService.GenerateRefreshTokenAsync(
                storedToken.UserId, 
                httpContext.Request.Headers.UserAgent.ToString());

            // Revoke old refresh token
            await tokenService.RevokeRefreshTokenAsync(refreshToken);

            // Set new refresh token as httpOnly cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7) // 7 day expiry
            };
            httpContext.Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

            logger.LogInformation("Successfully refreshed tokens for user: {UserId}", storedToken.UserId);

            return Results.Ok(new { Token = newAccessToken });
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