using trackerApi.Models;

namespace trackerApi.Services;

public interface ITokenService
{
    string GenerateToken(User? user = null, string? username = null, bool isRefreshToken = false);
}
