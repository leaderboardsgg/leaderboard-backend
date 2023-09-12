using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user);
    Task<ConfirmAccountResult> ConfirmAccount(Guid id);
}

[GenerateOneOf]
public partial class CreateConfirmationResult : OneOfBase<AccountConfirmation, BadRole, EmailFailed> { };

[GenerateOneOf]
public partial class ConfirmAccountResult : OneOfBase<AccountConfirmed, AlreadyUsed, BadRole, ConfirmationNotFound, Expired> { };

