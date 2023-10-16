using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public interface IAccountRecoveryService
{
    Task<CreateRecoveryResult> CreateRecoveryAndSendEmail(User user);
    Task<TestRecoveryResult> TestRecovery(Guid id);
    Task<ResetPasswordResult> ResetPassword(Guid id, string pwd);
}

public readonly record struct SamePassword { }

[GenerateOneOf]
public partial class CreateRecoveryResult : OneOfBase<AccountRecovery, BadRole, EmailFailed> { };

[GenerateOneOf]
public partial class TestRecoveryResult : OneOfBase<AlreadyUsed, BadRole, Expired, NotFound, Success> { };

[GenerateOneOf]
public partial class ResetPasswordResult : OneOfBase<AlreadyUsed, BadRole, Expired, NotFound, SamePassword, Success> { }
