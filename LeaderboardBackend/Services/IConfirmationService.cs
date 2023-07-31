using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IConfirmationService
{
    Task<Confirmation?> GetConfirmationById(Guid id);
    Task<Confirmation> CreateConfirmation(User user);
}
