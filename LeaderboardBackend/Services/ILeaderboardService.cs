using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
	Task<Leaderboard?> GetLeaderboard(long id);
	Task<Leaderboard?> GetLeaderboardBySlug(string slug);
	Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null);
	Task CreateLeaderboard(Leaderboard leaderboard);
}
