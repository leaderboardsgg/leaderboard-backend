using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
    Task<LeaderboardWithStats?> GetLeaderboard(long id);
    Task<LeaderboardWithStats?> GetLeaderboardBySlug(string slug);
    Task<ListResult<LeaderboardWithStats>> ListLeaderboards(StatusFilter statusFilter, Page page, SortLeaderboardsBy sortBy);
    Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request);
    Task<RestoreResult<LeaderboardWithStats>> RestoreLeaderboard(long id);
    Task<DeleteResult> DeleteLeaderboard(long id);
    Task<UpdateResult<Leaderboard>> UpdateLeaderboard(long id, UpdateLeaderboardRequest request);
    Task<ListResult<LeaderboardWithStats>> SearchLeaderboards(string query, StatusFilter statusFilter, Page page);
}

[GenerateOneOf]
public partial class CreateLeaderboardResult : OneOfBase<Leaderboard, Conflict<Leaderboard>>;
