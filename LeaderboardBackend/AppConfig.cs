using FluentValidation;

namespace LeaderboardBackend;

public class AppConfig
{
    public static Uri BASE_PATH = new("https://leaderboards.gg");
    public string? EnvPath { get; set; } = ".env";
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] ParseAllowedOrigins() => AllowedOrigins?.Split(';') ?? Array.Empty<string>();
}

public class AppConfigValidator : AbstractValidator<AppConfig>
{
    public AppConfigValidator(IWebHostEnvironment env)
    {
        RuleFor(x => x.AllowedOrigins).NotEmpty().When(x => env.IsProduction());
    }
}
