namespace LeaderboardBackend.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string recipientAddress, string subject, string htmlMessage);
}
