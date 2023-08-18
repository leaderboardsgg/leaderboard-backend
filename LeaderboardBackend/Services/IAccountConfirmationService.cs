using LeaderboardBackend.Models.Entities;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user, CancellationToken token = default);
}

[GenerateOneOf]
public partial class CreateConfirmationResult : OneOfBase<AccountConfirmation, BadRole, EmailFailed> { };

public readonly record struct BadRole();
public readonly record struct EmailFailed();
