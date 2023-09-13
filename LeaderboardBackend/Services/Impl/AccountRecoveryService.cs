using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using Microsoft.Extensions.Options;
using NodaTime;

namespace LeaderboardBackend.Services;

public class AccountRecoveryService : IAccountRecoveryService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly AppConfig _appConfig;

    public AccountRecoveryService(
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

    public async Task<CreateRecoveryResult> CreateRecoveryAndSendEmail(User user)
    {
        if (user.Role is not UserRole.Confirmed && user.Role is not UserRole.Administrator)
        {
            return new BadRole();
        }

        Instant now = _clock.GetCurrentInstant();

        AccountRecovery recovery = new()
        {
            CreatedAt = now,
            ExpiresAt = now + Duration.FromHours(1),
            User = user
        };

        await _applicationContext.AccountRecoveries.AddAsync(recovery);
        await _applicationContext.SaveChangesAsync();

        try
        {
            await _emailSender.EnqueueEmailAsync(
                user.Email,
                "Recover Your Account",
                GenerateAccountRecoveryEamilBody(user, recovery)
            );
        }
        catch
        {
            return new EmailFailed();
        }

        return recovery;
    }

    private string GenerateAccountRecoveryEamilBody(User user, AccountRecovery recovery) {
        UriBuilder builder = new(_appConfig.WebsiteUrl)
        {
            Path = "reset-password",
            Query = $"code={recovery.Id.ToUrlSafeBase64String()}"
        };

        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri}"">here</a> to reset your password.";
    }
}
