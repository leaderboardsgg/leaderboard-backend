using LeaderboardBackend.Models.Entities;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IConfirmationService
{
    Task<UserConfirmation?> GetConfirmationById(Guid id);
    Task<CreateUserConfirmationResult> CreateConfirmation(User user, CancellationToken token = default);
}

[GenerateOneOf]
public partial class CreateUserConfirmationResult : OneOfBase<UserConfirmation, DbCreateFailed, DbCreateTimedOut> { };

public readonly record struct DbCreateFailed();
public readonly record struct DbCreateTimedOut();
