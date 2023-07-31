using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class ConfirmationService : IConfirmationService
{
    private readonly ApplicationContext _applicationContext;

    public ConfirmationService(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<Confirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.Confirmations.FindAsync(id);
    }

    public async Task<Confirmation> CreateConfirmation(User user)
    {
        Confirmation newConfirmation =
            new()
            {
                UserId = user.Id,
            };

        _applicationContext.Confirmations.Add(newConfirmation);

        await _applicationContext.SaveChangesAsync();

        return newConfirmation;
    }
}
