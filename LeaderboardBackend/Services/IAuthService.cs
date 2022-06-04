using System.Security.Claims;
using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IAuthService
{
	string GenerateJSONWebToken(User user);
	public string? GetEmailFromClaims(ClaimsPrincipal claims);
	public Guid? GetUserIdFromClaims(ClaimsPrincipal claims);
}
