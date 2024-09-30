using FluentValidation;

namespace LeaderboardBackend.Models.Validation;

public static class UsernameRule
{
    public const string USERNAME_FORMAT = "UsernameFormat";

    // Double the single quote for postgres -Ted
    public const string REGEX = @"^[a-zA-Z0-9]([-_'']?[a-zA-Z0-9])+$";

    /// <summary>
    /// Validation will fail if the property does not respect the username format.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(2, 25)
                .WithErrorCode(USERNAME_FORMAT)
            .Matches(REGEX)
                .WithErrorCode(USERNAME_FORMAT)
                .WithMessage("Invalid username format.");
    }
}
