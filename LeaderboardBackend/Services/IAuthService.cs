using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IAuthService
{
	string GenerateJSONWebToken(User user);
}
