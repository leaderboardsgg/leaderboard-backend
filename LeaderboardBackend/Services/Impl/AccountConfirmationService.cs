using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Services;

public class AccountConfirmationService : IAccountConfirmationService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;

    public AccountConfirmationService(ApplicationContext applicationContext, IEmailSender emailSender, IClock clock)
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
        _clock = clock;
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

        Instant now = _clock.GetCurrentInstant();

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

    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation) =>
        $@"Hi {user.Username},<br/><br/>Click <a href=""https://leaderboards.gg/confirm-account?code={EncodeIdForEmail(confirmation)}""here</a> to confirm your account.";

    // Copy of https://datatracker.ietf.org/doc/html/rfc7515#page-55
    private string EncodeIdForEmail(AccountConfirmation confirmation)
        => Convert.ToBase64String(confirmation.Id.ToByteArray())
            .Split('=')[0]
            .Replace('+', '-')
            .Replace('/', '_');
}
