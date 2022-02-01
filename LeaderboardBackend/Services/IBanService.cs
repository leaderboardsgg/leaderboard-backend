using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IBanService
	{
		Task<Ban?> GetBanById(ulong id);

		Task<List<Ban>> GetBans(object? filter = null);

		Task CreateBan(Ban user);
	}
}
