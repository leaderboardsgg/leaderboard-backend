using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class ConfirmationService : IConfirmationService
{
    private readonly ApplicationContext _applicationContext;

    public ConfirmationService(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<UserConfirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.UserConfirmations.FindAsync(id);
    }

    public async Task<UserConfirmation> CreateConfirmation(User user)
    {
        UserConfirmation newConfirmation =
            new()
            {
                UserId = user.Id,
            };

        _applicationContext.UserConfirmations.Add(newConfirmation);

        await _applicationContext.SaveChangesAsync();

        return newConfirmation;
    }
}
