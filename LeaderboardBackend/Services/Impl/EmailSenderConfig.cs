using FluentValidation;

namespace LeaderboardBackend.Services;

public class EmailSenderConfig
{
    public const string KEY = "EmailSender";

    public string SenderAddress { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public SmtpServerConfig? Smtp { get; set; }
}

public class SmtpServerConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = false;
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class EmailSenderConfigValidator : AbstractValidator<EmailSenderConfig>
{
    public EmailSenderConfigValidator()
    {
        RuleFor(x => x.SenderAddress).NotEmpty();
        RuleFor(x => x.SenderName).NotEmpty();
        RuleFor(x => x.Smtp)
            .SetValidator(new SmtpServerConfigValidator()!)
            .When(x => x.Smtp is not null);
    }
}

public class SmtpServerConfigValidator : AbstractValidator<SmtpServerConfig>
{
    public SmtpServerConfigValidator()
    {
        RuleFor(x => x.Host).NotEmpty();
        RuleFor(x => x.Port).GreaterThan(0);
    }
}
