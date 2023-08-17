using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Services;

public class AccountConfirmationService : IAccountConfirmationService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;

    public AccountConfirmationService(ApplicationContext applicationContext, IEmailSender emailSender)
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
    }

    public async Task<AccountConfirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.AccountConfirmations.FindAsync(id);
    }

    public async Task<AccountConfirmation> CreateConfirmation(User user, CancellationToken token = default)
    {
        AccountConfirmation newConfirmation =
            new()
            {
                CreatedAt = SystemClock.Instance.GetCurrentInstant(),
                ExpiresAt = SystemClock.Instance.GetCurrentInstant() + Duration.FromHours(1),
                UserId = user.Id,
            };

        _applicationContext.AccountConfirmations.Add(newConfirmation);

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
    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation) =>
        $@"Hi {user.Username},<br/><br/>Click <a href=""/confirm-account?code={Convert.ToBase64String(confirmation.Id.ToByteArray())}"">here</a> to confirm your account.";
}
