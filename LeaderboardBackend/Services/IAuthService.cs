using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services;

public interface IAuthService
{
	string GenerateJSONWebToken(User user);
}