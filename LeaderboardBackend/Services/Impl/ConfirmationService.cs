using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class ConfirmationService : IConfirmationService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;

    public ConfirmationService(ApplicationContext applicationContext, IEmailSender emailSender)
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
    }

    public async Task<UserConfirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.UserConfirmations.FindAsync(id);
    }

    public async Task<UserConfirmation> CreateConfirmation(User user, CancellationToken token = default)
    {
        UserConfirmation newConfirmation =
            new()
            {
                UserId = user.Id,
            };

        _applicationContext.UserConfirmations.Add(newConfirmation);

        await _applicationContext.SaveChangesAsync(token);

#pragma warning disable CS4014 // Suppress no 'await' call
        _emailSender.EnqueueEmailAsync(
            user.Email,
            // TODO: Finalise the title
            "Confirmation",
            GenerateAccountConfirmationEmailBody(user, newConfirmation)
        );
#pragma warning restore CS4014

        return newConfirmation;
    }

    // TODO: Finalise message contents
    private string GenerateAccountConfirmationEmailBody(User user, UserConfirmation confirmation) =>
        $@"Hi {user.Username},<br/><br/>Click <a href=""/confirm-account?code={Convert.ToBase64String(confirmation.Id.ToByteArray())}"">here</a> to confirm your account.";
}
