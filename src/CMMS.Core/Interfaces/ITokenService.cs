using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshToken GenerateRefreshToken(int userId, string? ipAddress = null);
    int? ValidateAccessToken(string token);
}
