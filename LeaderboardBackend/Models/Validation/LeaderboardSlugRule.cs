using FluentValidation;

namespace LeaderboardBackend.Models.Validation;

public static class LeaderboardSlugRule
{
    public const string LEADERBOARD_SLUG_FORMAT = "SlugFormat";
    public const string REGEX = @"^(?!([0-9]+)$)[a-zA-Z0-9\-_]*$";

    public static IRuleBuilderOptions<T, string> LeaderboardSlug<T>(this IRuleBuilder<T, string> ruleBuilder)
        => ruleBuilder
            .Length(2, 80)
                .WithErrorCode(LEADERBOARD_SLUG_FORMAT)
            .Matches(REGEX)
                .WithErrorCode(LEADERBOARD_SLUG_FORMAT)
                .WithMessage("Invalid slug format.");
}
