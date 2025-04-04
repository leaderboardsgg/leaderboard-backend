using FluentValidation;

namespace LeaderboardBackend;

public class LimitConfig
{
    public int Default { get; set; }
    public int Max { get; set; }
}

public class AppConfig
{
    public Uri WebsiteUrl { get; set; } = null!;
    public string? EnvPath { get; set; } = ".env";
    public string AllowedOrigins { get; set; } = string.Empty;

    public string[] ParseAllowedOrigins() => AllowedOrigins?.Split(';') ?? Array.Empty<string>();

    public Dictionary<string, LimitConfig> Limits { get; set; } = [];
}

public class LimitValidator : AbstractValidator<LimitConfig>
{
    public LimitValidator()
    {
        RuleFor(x => x.Default).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Max).GreaterThanOrEqualTo(0);
    }
}

public class AppConfigValidator : AbstractValidator<AppConfig>
{
    public AppConfigValidator(IWebHostEnvironment env)
    {
        RuleFor(x => x.AllowedOrigins).NotEmpty().When(x => env.IsProduction());
        RuleFor(x => x.WebsiteUrl).NotNull().Must(u => u.IsAbsoluteUri);
        RuleFor(x => x.Limits.Keys).Must(x => x.Contains("default", StringComparer.InvariantCultureIgnoreCase));

        RuleForEach(x => x.Limits.Values)
            .Must(limit => limit.Max >= limit.Default)
            .SetValidator(new LimitValidator());
    }
}
