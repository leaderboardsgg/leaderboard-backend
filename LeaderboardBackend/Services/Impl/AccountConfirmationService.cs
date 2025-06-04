using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class AccountConfirmationService(
    ApplicationContext applicationContext,
    IEmailSender emailSender,
    IClock clock,
    IOptions<AppConfig> appConfig
) : IAccountConfirmationService
{

    public async Task<AccountConfirmation?> GetConfirmationById(Guid id) =>
        await applicationContext.AccountConfirmations.FindAsync(id);

    public async Task<CreateConfirmationResult> CreateConfirmationAndSendEmail(User user)
    {
        if (user.Role is not UserRole.Registered)
        {
            return new BadRole();
        }

        Instant now = clock.GetCurrentInstant();

        AccountConfirmation newConfirmation =
            new()
            {
                ExpiresAt = now + Duration.FromHours(1),
                UserId = user.Id,
            };

        applicationContext.AccountConfirmations.Add(newConfirmation);
        await applicationContext.SaveChangesAsync();

        try
        {
            await emailSender.EnqueueEmailAsync(
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

    public async Task<EmailExistingResult> EmailExistingUserOfRegistrationAttempt(User user)
    {
        // Resend the first email if UserRole.Registered
        if (user.Role is UserRole.Registered)
        {
            CreateConfirmationResult r = await CreateConfirmationAndSendEmail(user);
            return r.Match<EmailExistingResult>(
                confirmation => new Success(),
                badRole => new BadRole(),
                emailFailed => new EmailFailed()
            );
        }

        if (user.Role is UserRole.Banned)
        {
            return new BadRole();
        }

        try
        {
            await emailSender.EnqueueEmailAsync(
                user.Email,
                "A Registration Attempt was Made with Your Email",
                GenerateRegistrationAttemptEmailBody(user)
            );

            return new Success();
        }
        catch
        {
            return new EmailFailed();
        }
    }

    public async Task<ConfirmAccountResult> ConfirmAccount(Guid id)
    {
        AccountConfirmation? confirmation = await applicationContext.AccountConfirmations.Include(c => c.User).SingleOrDefaultAsync(c => c.Id == id);

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

        Instant now = clock.GetCurrentInstant();

        if (confirmation.ExpiresAt <= now)
        {
            return new Expired();
        }

        confirmation.User.Role = UserRole.Confirmed;
        confirmation.UsedAt = now;
        await applicationContext.SaveChangesAsync();
        return new AccountConfirmed();
    }

    private string GenerateAccountConfirmationEmailBody(User user, AccountConfirmation confirmation)
    {
        // Copy of https://datatracker.ietf.org/doc/html/rfc7515#page-55
        UriBuilder builder = new(appConfig.Value.WebsiteUrl)
        {
            Path = "confirm-account",
            Query = $"code={confirmation.Id.ToUrlSafeBase64String()}"
        };
        return $@"Hi {user.Username},<br/><br/>Click <a href=""{builder.Uri}"">here</a> to confirm your account.";
    }

    // TODO: Fill contents
    private static string GenerateRegistrationAttemptEmailBody(User user) =>
        $@"Hi {user.Username},<br/><br/>Someone tried to register an account with your email address. " +
        "If it wasn't you, you can safely ignore this email.";
}
