using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
    Task<Leaderboard?> GetLeaderboard(long id);
    Task<Leaderboard?> GetLeaderboardBySlug(string slug);
    Task<List<Leaderboard>> ListLeaderboards(bool includeDeleted);
    Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request);
    Task<RestoreLeaderboardResult> RestoreLeaderboard(long id);
    Task<DeleteResult> DeleteLeaderboard(long id);
    Task<UpdateResult<Leaderboard>> UpdateLeaderboard(long id, UpdateLeaderboardRequest request);
}

[GenerateOneOf]
public partial class CreateLeaderboardResult : OneOfBase<Leaderboard, Conflict<Leaderboard>>;

[GenerateOneOf]
public partial class RestoreLeaderboardResult : OneOfBase<Leaderboard, NotFound, LeaderboardNeverDeleted, Conflict<Leaderboard>>;
