using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LeaderboardBackend.Services;

[Obsolete("Replaced by BrevoService")]
public class EmailSender : IEmailSender
{
    private readonly EmailSenderConfig _config;
    private readonly ILogger<EmailSender> _logger;
    private readonly ISmtpClient _smtpClient;
    private readonly MailboxAddress _sender;
    private readonly SemaphoreSlim _semaphore;

    public EmailSender(IOptions<EmailSenderConfig> config, ILogger<EmailSender> logger, ISmtpClient smtpClient)
    {
        _config = config.Value;
        _logger = logger;
        _smtpClient = smtpClient;
        _sender = new MailboxAddress(name: _config.SenderName, address: _config.SenderAddress);
        _semaphore = new SemaphoreSlim(1);
    }

    public async Task EnqueueEmailAsync(string recipientAddress, string subject, string htmlMessage)
    {
        if (_config.Smtp is null)
        {
            _logger.LogError("Can't send email, SMTP configuration is missing");
            return;
        }

        MimeMessage message = new();
        message.From.Add(_sender);
        message.To.Add(new MailboxAddress(name: null, address: recipientAddress));
        message.Subject = subject;

        BodyBuilder bodyBuilder = new() { HtmlBody = htmlMessage };
        message.Body = bodyBuilder.ToMessageBody();

        await _semaphore.WaitAsync();
        try
        {
            await SendEmail(message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SendEmail(MimeMessage message)
    {
        // check the connection because the SMTP server will drop the connection after some idle time
        // connection errors set IsConnected to false
        try
        {
            await _smtpClient.NoOpAsync();
        }
        catch
        {
            // ignored
        }

        if (!_smtpClient.IsConnected)
        {
            await _smtpClient.ConnectAsync(_config.Smtp!.Host, _config.Smtp.Port, _config.Smtp.UseSsl);
        }

        if ((_config.Smtp!.Username is not null || _config.Smtp.Password is not null)
            && !_smtpClient.IsAuthenticated)
        {
            await _smtpClient.AuthenticateAsync(_config.Smtp.Username, _config.Smtp.Password);
        }

        await _smtpClient.SendAsync(message);
    }
}
