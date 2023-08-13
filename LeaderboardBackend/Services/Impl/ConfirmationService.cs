using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<CreateUserConfirmationResult> CreateConfirmation(User user, CancellationToken token = default)
    {
        UserConfirmation newConfirmation =
            new()
            {
                UserId = user.Id,
            };

        _applicationContext.UserConfirmations.Add(newConfirmation);

        try
        {
            await _applicationContext.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            return new DbCreateFailed();
        }
        catch (OperationCanceledException)
        {
            return new DbCreateTimedOut();
        }

#pragma warning disable CS4014 // Suppress no 'await' call
        _emailSender.EnqueueEmailAsync(
            user.Email,
            // TODO: Finalise the title
            "Confirmation",
            // TODO: Generate confirmation link
            GenerateAccountConfirmationEmailBody(user, newConfirmation)
        );
#pragma warning restore CS4014

        return newConfirmation;
    }

    // TODO: Finalise message contents
    private string GenerateAccountConfirmationEmailBody(User user, UserConfirmation confirmation) =>
        $@"Hi {user.Username},<br/><br/>Click <a href=""/confirm-account?code={Convert.ToBase64String(confirmation.Id.ToByteArray())}"">here</a> to confirm your account.";
}
