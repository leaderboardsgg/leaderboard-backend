using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
    Task<Leaderboard?> GetLeaderboard(long id);
    Task<Leaderboard?> GetLeaderboardBySlug(string slug);
    Task<List<Leaderboard>> ListLeaderboards();
    Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request);
    Task<RestoreLeaderboardResult> RestoreLeaderboard(long id);
}

[GenerateOneOf]
public partial class CreateLeaderboardResult : OneOfBase<Leaderboard, CreateLeaderboardConflict>;

[GenerateOneOf]
public partial class RestoreLeaderboardResult : OneOfBase<Leaderboard, LeaderboardNotFound, LeaderboardNeverDeleted, RestoreLeaderboardConflict>;
