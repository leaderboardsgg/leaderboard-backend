using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IUserConfirmationService
{
    Task<UserConfirmation?> GetConfirmationById(Guid id);
    Task<UserConfirmation> CreateConfirmation(User user, CancellationToken token = default);
}
