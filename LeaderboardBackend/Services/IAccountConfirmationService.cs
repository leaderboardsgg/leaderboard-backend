using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IAccountConfirmationService
{
    Task<AccountConfirmation?> GetConfirmationById(Guid id);
    Task<AccountConfirmation> CreateConfirmation(User user, CancellationToken token = default);
}
