namespace LeaderboardBackend.Services;

public interface IEmailSender
{
    Task EnqueueEmailAsync(string recipientAddress, string subject, string htmlMessage);
}
