using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;
using OneOf.Types;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Services;

public class AccountRecoveryService : IAccountRecoveryService
{
    private readonly ApplicationContext _applicationContext;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly AppConfig _appConfig;
    private readonly ILogger<AccountRecoveryService> _logger;

    public AccountRecoveryService(
        ApplicationContext applicationContext,
        IEmailSender emailSender,
        IClock clock,
        IOptions<AppConfig> appConfig,
        ILogger<AccountRecoveryService> logger
    )
    {
        _applicationContext = applicationContext;
        _emailSender = emailSender;
        _clock = clock;
        _appConfig = appConfig.Value;
        _logger = logger;
    }

    public async Task<CreateRecoveryResult> CreateRecoveryAndSendEmail(User user)
    {
        if (user.Role is not UserRole.Confirmed && user.Role is not UserRole.Administrator)
        {
            _logger.LogWarning("Can't send account recovery email; user {id} not confirmed/admin", user.Id);
            return new BadRole();
        }

        Instant now = _clock.GetCurrentInstant();

        AccountRecovery recovery = new()
        {
            ExpiresAt = now + Duration.FromHours(1),
            User = user
        };

        _applicationContext.AccountRecoveries.Add(recovery);
        await _applicationContext.SaveChangesAsync();

        try
        {
            await _emailSender.EnqueueEmailAsync(
                user.Email,
                "Recover Your Account",
                GenerateAccountRecoveryEmailBody(user, recovery)
            );
        }
        catch (Exception e)
        {
            _logger.LogError(
                "{type}: Recovery email failed to send for user {id}, {username}",
                e.GetType().ToString(),
                user.Id,
                user.Username
            );
            return new EmailFailed();
        }

        return recovery;
    }

    private string GenerateAccountRecoveryEmailBody(User user, AccountRecovery recovery)
    {
        UriBuilder builder = new(_appConfig.WebsiteUrl)
        {
            Path = "reset-password",
            Query = $"code={recovery.Id.ToUrlSafeBase64String()}"
        };

        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri}"">here</a> to reset your password.";
    }

    public async Task<TestRecoveryResult> TestRecovery(Guid id)
    {
        AccountRecovery? recovery = await _applicationContext.AccountRecoveries.Include(ar => ar.User).SingleOrDefaultAsync(ar => ar.Id == id);

        if (recovery is null)
        {
            return new NotFound();
        }

        if (recovery.User.Role is UserRole.Banned)
        {
            return new BadRole();
        }

        if (recovery.UsedAt is not null)
        {
            return new AlreadyUsed();
        }

        Instant now = _clock.GetCurrentInstant();

        if (recovery.ExpiresAt <= now)
        {
            return new Expired();
        }

        IQueryable<Guid> latest =
            from rec in _applicationContext.AccountRecoveries
            where rec.UserId == recovery.UserId
            orderby rec.CreatedAt descending
            select rec.Id;

        Guid latestId = await latest.FirstAsync();

        if (latestId != id)
        {
            return new Expired();
        }

        return new Success();
    }

    public async Task<ResetPasswordResult> ResetPassword(Guid id, string password)
    {
        AccountRecovery? recovery = await _applicationContext.AccountRecoveries.Include(ar => ar.User).SingleOrDefaultAsync(ar => ar.Id == id);

        if (recovery is null)
        {
            return new NotFound();
        }

        if (recovery.User.Role is UserRole.Banned)
        {
            return new BadRole();
        }

        if (recovery.UsedAt is not null)
        {
            return new AlreadyUsed();
        }

        Instant now = _clock.GetCurrentInstant();

        if (recovery.ExpiresAt <= now)
        {
            return new Expired();
        }

        IQueryable<Guid> latest =
            from rec in _applicationContext.AccountRecoveries
            where rec.UserId == recovery.UserId
            orderby rec.CreatedAt descending
            select rec.Id;

        Guid latestId = await latest.FirstAsync();

        if (latestId != id)
        {
            return new Expired();
        }

        if (BCryptNet.EnhancedVerify(password, recovery.User.Password))
        {
            return new SamePassword();
        }

        recovery.User.Password = BCryptNet.EnhancedHashPassword(password);
        recovery.UsedAt = now;
        await _applicationContext.SaveChangesAsync();

        return new Success();
    }
}
