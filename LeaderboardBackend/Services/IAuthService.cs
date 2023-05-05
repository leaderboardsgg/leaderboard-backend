using System.Security.Claims;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IAuthService
{
    string GenerateJSONWebToken(User user);
    string? GetEmailFromClaims(ClaimsPrincipal claims);
    Guid? GetUserIdFromClaims(ClaimsPrincipal claims);
}
