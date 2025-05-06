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
    /// <returns>
    ///     One of four cases (none of which return any inner data):
    ///     <list type="bullet">
    ///         <item>
    ///             <term>BadRole</term>
    ///             <description>
    ///                 The user is either not a
    ///                 <see cref="UserRole.Administrator"/>, or they are a
    ///                 <see cref="UserRole.Confirmed" /> but are trying to
    ///                 edit a run that isn't theirs.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>NotFound</term>
    ///             <description>The run doesn't exist in the DB.</description>
    ///         </item>
    ///         <item>
    ///             <term>BadRunType</term>
    ///             <description>
    ///                 <c>request</c>'s <c>runType</c> does not match the run
    ///                 type of the category the run should belong to.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>Success</term>
    ///             <description>
    ///                 The update succeeded.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    Task<UpdateRunResult> UpdateRun(User user, Guid id, UpdateRunRequest request);
    Task<DeleteResult> DeleteRun(Guid id);
}

[GenerateOneOf]
public partial class CreateRunResult : OneOfBase<Run, BadRole, BadRunType>;

[GenerateOneOf]
public partial class UpdateRunResult : OneOfBase<BadRole, UserDoesNotOwnRun, NotFound, AlreadyDeleted, BadRunType, Success>;

[GenerateOneOf]
public partial class GetRunsForCategoryResult : OneOfBase<ListResult<Run>, NotFound>;
