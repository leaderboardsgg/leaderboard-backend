using LeaderboardBackend.Models.Entities;
using Microsoft.Extensions.Options;
using NodaTime;

namespace LeaderboardBackend.Services;

public class AccountConfirmationService : IAccountConfirmationService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly AppConfig _appConfig;

    public AccountConfirmationService(
        ApplicationContext applicationContext,
        IEmailSender emailSender,
        IClock clock,
        IOptions<AppConfig> appConfig
    )
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
        _clock = clock;
        _appConfig = appConfig.Value;
    }

    public async Task<AccountConfirmation?> GetConfirmationById(Guid id)
    {
        return await _applicationContext.AccountConfirmations.FindAsync(id);
    }

    public async Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user)
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

        await _applicationContext.SaveChangesAsync();

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

    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation)
    {
        // Copy of https://datatracker.ietf.org/doc/html/rfc7515#page-55
        string encodedConfirmationId = Convert.ToBase64String(confirmation.Id.ToByteArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        UriBuilder builder = new(_appConfig.WebsiteUrl);
        builder.Path = "confirm-account";
        builder.Query = $"code={encodedConfirmationId}";
        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri.ToString()}""here</a> to confirm your account.";
    }
}
