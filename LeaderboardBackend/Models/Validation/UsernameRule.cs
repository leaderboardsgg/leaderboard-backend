using FluentValidation;

namespace LeaderboardBackend.Models.Validation;

public static class UsernameRule
{
    public const string NAME = "USERNAME_FORMAT";

    /// <summary>
    /// Validation will fail if the property does not respect the password format.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Username<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Length(2, 25)
                .WithErrorCode(NAME)
            .Matches("^(?:[a-zA-Z0-9]+[-_']?[a-zA-Z0-9]+)+$")
                .WithErrorCode(NAME)
                .WithMessage("Invalid username format.");
    }
}
