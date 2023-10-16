using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IAccountRecoveryService
{
    Task<CreateRecoveryResult> CreateRecoveryAndSendEmail(User user);
    Task<RecoverAccountResult> TestRecovery(Guid id);
}

[GenerateOneOf]
public partial class CreateRecoveryResult : OneOfBase<AccountRecovery, BadRole, EmailFailed> { };

[GenerateOneOf]
public partial class RecoverAccountResult : OneOfBase<AlreadyUsed, BadRole, Expired, NotFound, Success> { };
