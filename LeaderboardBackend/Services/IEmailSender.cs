namespace LeaderboardBackend.Services;

public interface IEmailSender
{
    /// <summary>
    /// Enqueues an email for sending.
    /// </summary>
    /// <param name="recipientAddress">The email address of the recipient</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlMessage">Email message in HTML format</param>
    /// <remarks>Throws an exception on queuing failure.</remarks>
    Task EnqueueEmailAsync(string recipientAddress, string subject, string htmlMessage);
}
