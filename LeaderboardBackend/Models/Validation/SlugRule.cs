using FluentValidation;

namespace LeaderboardBackend.Models.Validation;

public static class SlugRule
{
    public const string SLUG_FORMAT = "SlugFormat";

    public static IRuleBuilderOptions<T, string> Slug<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .Length(2, 80)
                .WithErrorCode(SLUG_FORMAT)
            .Matches(@"^([a-zA-Z0-9-_]|%[A-F0-9]{2})*$")
                .WithErrorCode(SLUG_FORMAT)
                .WithMessage("Invalid slug format.");
}
