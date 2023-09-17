using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user);
}

[GenerateOneOf]
public partial class CreateConfirmationResult : OneOfBase<AccountConfirmation, BadRole, EmailFailed> { };
