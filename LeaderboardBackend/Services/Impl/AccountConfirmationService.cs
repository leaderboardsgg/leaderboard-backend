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

    public async Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user, CancellationToken token = default)
    {
        if (user.Role is not UserRole.Registered)
        {
            return new BadRole();
        }

        Instant now = SystemClock.Instance.GetCurrentInstant();

        AccountConfirmation newConfirmation =
            new()
            {
                CreatedAt = now,
                ExpiresAt = now + Duration.FromHours(1),
                UserId = user.Id,
            };

        _applicationContext.AccountConfirmations.Add(newConfirmation);

        await _applicationContext.SaveChangesAsync(token);

        try
        {
            await _emailSender.EnqueueEmailAsync(
                user.Email,
                "Confirm Your Account",
                GenerateAccountConfirmationEmailBody(user, newConfirmation)
            );
        }
        catch
        {
            // TODO: Log/otherwise handle the fact that the email failed to be queued - zysim
            return new EmailFailed();
        }

        return newConfirmation;
    }

    // TODO: Finalise message contents
    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation) =>
        $@"Hi {user.Username},<br/><br/>Click <a href=""/confirm-account?code={Convert.ToBase64String(confirmation.Id.ToByteArray())}"">here</a> to confirm your account.";
}
