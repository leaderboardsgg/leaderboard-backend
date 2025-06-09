using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface ILeaderboardService
{
    Task<Leaderboard?> GetLeaderboard(long id);
    Task<Leaderboard?> GetLeaderboardBySlug(string slug);
    Task<ListResult<Leaderboard>> ListLeaderboards(StatusFilter statusFilter, Page page, SortLeaderboardsBy sortBy);
    Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request);
    Task<RestoreResult<Leaderboard>> RestoreLeaderboard(long id);
    Task<DeleteResult> DeleteLeaderboard(long id);
    Task<UpdateResult<Leaderboard>> UpdateLeaderboard(long id, UpdateLeaderboardRequest request);
    Task<ListResult<Leaderboard>> SearchLeaderboards(string query, StatusFilter statusFilter, Page page);
}

[GenerateOneOf]
public partial class CreateLeaderboardResult : OneOfBase<Leaderboard, Conflict<Leaderboard>>;
