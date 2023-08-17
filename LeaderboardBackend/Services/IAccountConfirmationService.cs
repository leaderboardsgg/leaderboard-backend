using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<BadRole?> CreateConfirmationAndSendEmail(User user, CancellationToken token = default);
}

public readonly record struct BadRole();
