using trackerApi.Models;

namespace trackerApi.Services;

public interface ITokenService
{
    //string GenerateToken(User? user = null, string? username = null, bool isRefreshToken = false);
    Task<string> GenerateToken(User? user = null, string? username = null, bool isRefreshToken = false);

    /// <summary>
    /// Optimized JWT generation for refresh scenarios - no additional DB calls
    /// </summary>
    /// <param name="userId">The user ID for the JWT claims.</param>
    /// <param name="username">The username for the JWT claims.</param>
    /// <returns>The generated JWT token string.</returns>
    Task<string> GenerateTokenFromUserData(Guid userId, string username);

    /// <summary>
    /// Optimized refresh token rotation: INSERT new + UPDATE old in single transaction
    /// </summary>
    /// <param name="oldTokenId">The ID of the refresh token to revoke.</param>
    /// <param name="userId">The user ID to generate the new refresh token for.</param>
    /// <param name="deviceInfo">Optional device information for tracking purposes.</param>
    /// <returns>The generated refresh token string.</returns>
    Task<string> RotateRefreshTokenAsync(Guid oldTokenId, Guid userId, string? deviceInfo = null);

    /// <summary>
    /// Generates a new refresh token for the specified user and stores it in the database.
    /// </summary>
    /// <param name="userId">The user ID to generate the refresh token for.</param>
    /// <param name="deviceInfo">Optional device information for tracking purposes.</param>
    /// <returns>The generated refresh token string.</returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, string? deviceInfo = null);

    /// <summary>
    /// Validates that a refresh token exists and is valid for the specified user.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <param name="userId">The user ID to validate the token against.</param>
    /// <returns>True if the token is valid and not expired or revoked, otherwise false.</returns>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId);

    /// <summary>
    /// Revokes a specific refresh token by marking it as revoked.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    Task RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes all refresh tokens for a specific user.
    /// </summary>
    /// <param name="userId">The user ID whose refresh tokens should be revoked.</param>
    Task RevokeAllUserRefreshTokensAsync(Guid userId);
}
