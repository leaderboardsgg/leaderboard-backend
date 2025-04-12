using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IRunService
{
    Task<Run?> GetRun(Guid id);
    Task<GetRunsForCategoryResult> GetRunsForCategory(long id, Page page, bool includeDeleted = false);
    Task<CreateRunResult> CreateRun(User user, Category category, CreateRunRequest request);
    Task<UpdateRunResult> UpdateRun(User user, Guid id, UpdateRunRequest request);
}

[GenerateOneOf]
public partial class CreateRunResult : OneOfBase<Run, BadRole, BadRunType>;

[GenerateOneOf]
public partial class UpdateRunResult : OneOfBase<BadRole, NotFound, BadRunType, Success>;

[GenerateOneOf]
public partial class GetRunsForCategoryResult : OneOfBase<ListResult<Run>, NotFound>;
