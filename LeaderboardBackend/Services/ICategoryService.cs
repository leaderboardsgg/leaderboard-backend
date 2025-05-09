using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface ICategoryService
{
    Task<Category?> GetCategory(long id);
    Task<Category?> GetCategoryBySlug(long leaderboardId, string slug);
    Task<GetCategoriesForLeaderboardResult> GetCategoriesForLeaderboard(long leaderboardId, StatusFilter statusFilter, Page page);
    Task<CreateCategoryResult> CreateCategory(long leaderboardId, CreateCategoryRequest request);
    Task<Category?> GetCategoryForRun(Run run);
    Task<UpdateResult<Category>> UpdateCategory(long id, UpdateCategoryRequest request);
    Task<DeleteResult> DeleteCategory(long id);
    Task<RestoreResult<Category>> RestoreCategory(long id);
}

[GenerateOneOf]
public partial class CreateCategoryResult : OneOfBase<Category, Conflict<Category>, NotFound>;

[GenerateOneOf]
public partial class GetCategoriesForLeaderboardResult : OneOfBase<ListResult<Category>, NotFound>;
