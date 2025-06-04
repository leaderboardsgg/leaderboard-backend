using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user);
    Task<EmailExistingResult> EmailExistingUserOfRegistrationAttempt(User user);
    Task<ConfirmAccountResult> ConfirmAccount(Guid id);
}

[GenerateOneOf]
public partial class CreateConfirmationResult : OneOfBase<AccountConfirmation, BadRole, EmailFailed> { };

// Remember: make sure this is a subset of CreateConfirmationResult, or at least match it
[GenerateOneOf]
public partial class EmailExistingResult : OneOfBase<Success, BadRole, EmailFailed> { };
[GenerateOneOf]
public partial class ConfirmAccountResult : OneOfBase<AccountConfirmed, AlreadyUsed, BadRole, ConfirmationNotFound, Expired> { };
