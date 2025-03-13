using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IRunService
{
    Task<Run?> GetRun(Guid id);
    Task<CreateRunResult> CreateRun(User user, Category category, CreateRunRequest request);
}

[GenerateOneOf]
public partial class CreateRunResult : OneOfBase<Run, BadRole, BadRunType>;
