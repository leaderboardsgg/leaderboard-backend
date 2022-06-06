using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IBanService
{
	Task<Ban?> GetBanById(long id);

	Task<List<Ban>> GetBans();

	Task<List<Ban>> GetBansByLeaderboard(long leaderboardId);

	Task<List<Ban>> GetBansByUser(Guid userId);

	Task CreateBan(Ban user);

	Task DeleteBan(long id);
}
