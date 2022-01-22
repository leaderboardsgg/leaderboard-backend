using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface ILeaderboardService
	{
		Task<Leaderboard?> GetLeaderboard(long id);

		Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null);
		Task CreateLeaderboard(Leaderboard leaderboard);
	}
}
