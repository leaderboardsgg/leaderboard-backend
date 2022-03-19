using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
	Task<Leaderboard?> GetLeaderboard(ulong id);
	Task<List<Leaderboard>> GetLeaderboards(ulong[]? ids = null);
	Task CreateLeaderboard(Leaderboard leaderboard);
}
