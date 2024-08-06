using brevo_csharp.Api;
using brevo_csharp.Model;
using Microsoft.Extensions.Options;

namespace LeaderboardBackend.Services;

public class BrevoService(IOptions<BrevoOptions> options, ILogger<BrevoService> logger) : IEmailSender
{
    private readonly TransactionalEmailsApi _transactionalEmailsApi = new();
    private readonly SendSmtpEmailSender _smtpEmailSender = new(options.Value.SenderName, options.Value.SenderEmail);

    public async System.Threading.Tasks.Task EnqueueEmailAsync(string recipientAddress, string subject, string htmlMessage)
    {
        SendSmtpEmail email = new(_smtpEmailSender, [new(recipientAddress)], null, null, htmlMessage, null, subject);
        CreateSmtpEmail result = await _transactionalEmailsApi.SendTransacEmailAsync(email);
        logger.LogInformation("Email sent with id {Id}", result.MessageId);
    }
}
