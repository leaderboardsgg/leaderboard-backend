using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
    Task<Leaderboard?> GetLeaderboard(long id);
    Task<Leaderboard?> GetLeaderboardBySlug(string slug);
    Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null);
    Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request);
}

[GenerateOneOf]
public partial class CreateLeaderboardResult : OneOfBase<Leaderboard, CreateLeaderboardConflict>;
