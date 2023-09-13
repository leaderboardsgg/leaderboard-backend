using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using OneOf;

namespace LeaderboardBackend.Services;

public interface IAccountRecoveryService
{
    Task<CreateRecoveryResult> CreateRecoveryAndSendEmail(User user);
}

[GenerateOneOf]
public partial class CreateRecoveryResult : OneOfBase<AccountRecovery, BadRole, EmailFailed> {};
