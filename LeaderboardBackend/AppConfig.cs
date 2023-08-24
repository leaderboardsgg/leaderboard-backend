using FluentValidation;

namespace LeaderboardBackend;

public class AppConfig
{
    public string? WebsiteUrl { get; set; }
    public string? EnvPath { get; set; } = ".env";
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] ParseAllowedOrigins() => AllowedOrigins?.Split(';') ?? Array.Empty<string>();
}

public class AppConfigValidator : AbstractValidator<AppConfig>
{
    public AppConfigValidator(IWebHostEnvironment env)
    {
        RuleFor(x => x.AllowedOrigins).NotEmpty().When(x => env.IsProduction());
        RuleFor(x => x.WebsiteUrl).NotNull();
    }
}
