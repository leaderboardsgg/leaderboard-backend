using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IConfirmationService
{
    Task<UserConfirmation?> GetConfirmationById(Guid id);
    Task<UserConfirmation> CreateConfirmation(User user);
}
