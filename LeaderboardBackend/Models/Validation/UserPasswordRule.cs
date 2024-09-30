using FluentValidation;

namespace LeaderboardBackend.Models.Validation;

public static class UserPasswordRule
{
    public const string PASSWORD_FORMAT = "PasswordFormat";

    /// <summary>
    /// Validation will fail if the property does not respect the password format.
    /// </summary>
    public static IRuleBuilderOptions<T, string> UserPassword<T>(this IRuleBuilderInitial<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(8, 80)
                .WithErrorCode(PASSWORD_FORMAT)
            .Must(IsValidPassword)
                .WithErrorCode(PASSWORD_FORMAT)
                .WithMessage("Invalid password format.");
    }

    private static bool IsValidPassword(string password)
    {
        bool hasLowerLetter = false;
        bool hasUpperLetter = false;
        bool hasDigit = false;

        foreach (char c in password)
        {
            if (char.IsAsciiLetterLower(c))
            {
                hasLowerLetter = true;
            }
            else if (char.IsAsciiLetterUpper(c))
            {
                hasUpperLetter = true;
            }
            else if (char.IsAsciiDigit(c))
            {
                hasDigit = true;
            }
        }

        return hasLowerLetter && hasUpperLetter && hasDigit;
    }
}
