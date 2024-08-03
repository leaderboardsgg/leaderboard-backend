using FluentValidation;

namespace LeaderboardBackend.Services;

public class BrevoOptions
{
    public const string KEY = "Brevo";
    public string ApiKey { get; set; } = string.Empty;
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }
}

public class BrevoOptionsValidator : AbstractValidator<BrevoOptions>
{
    public BrevoOptionsValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty();
        RuleFor(x => x.SenderEmail).EmailAddress();
    }
}
