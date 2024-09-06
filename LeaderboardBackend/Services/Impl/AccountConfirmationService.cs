using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
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
            return new EmailFailed();
        }

        return newConfirmation;
    }

    public async Task<ConfirmAccountResult> ConfirmAccount(Guid id)
    {
        AccountConfirmation? confirmation = await _applicationContext.AccountConfirmations.Include(c => c.User).SingleOrDefaultAsync(c => c.Id == id);

        if (confirmation is null)
        {
            return new ConfirmationNotFound();
        }

        if (confirmation.User.Role is not UserRole.Registered)
        {
            return new BadRole();
        }

        if (confirmation.UsedAt is not null)
        {
            return new AlreadyUsed();
        }

        Instant now = _clock.GetCurrentInstant();

        if (confirmation.ExpiresAt <= now)
        {
            return new Expired();
        }

        confirmation.User.Role = UserRole.Confirmed;
        confirmation.UsedAt = now;
        await _applicationContext.SaveChangesAsync();
        return new AccountConfirmed();
    }

    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation)
    {
        // Copy of https://datatracker.ietf.org/doc/html/rfc7515#page-55
        UriBuilder builder = new(_appConfig.WebsiteUrl)
        {
            Path = "confirm-account",
            Query = $"code={confirmation.Id.ToUrlSafeBase64String()}"
        };
        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri}"">here</a> to confirm your account.";
    }
}
