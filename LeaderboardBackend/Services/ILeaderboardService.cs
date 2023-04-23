using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
	Task<Leaderboard?> GetLeaderboard(long id);
	Task<List<Leaderboard>> GetLeaderboards(GetLeaderboardsQuery query);
	Task CreateLeaderboard(Leaderboard leaderboard);
}

public record GetLeaderboardsQuery(long[]? Ids = null, string? Slug = null);
