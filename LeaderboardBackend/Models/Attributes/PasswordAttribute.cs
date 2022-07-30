using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LeaderboardBackend.Models.Attributes;

public class PasswordAttribute : ValidationAttribute
{
	private const int MIN = 8;
	private const int MAX = 80;

	public string GetErrorMessage(List<string> errors)
	{
		return $"Your password has the following errors: {string.Join("; ", errors)}";
	}

	// FIXME: Use compiled Regexes instead of creating new instances every time! - Ero
	protected override ValidationResult? IsValid(object? value, ValidationContext _)
	{
		List<string> errors = new();

		if (value is null)
		{
			errors.Add("password must be supplied");
			return new(GetErrorMessage(errors));
		}

		string password = (string)value;

		if (password.Length < MIN)
		{
			errors.Add($"password shorter than {MIN}");
		}

		if (password.Length > MAX)
		{
			errors.Add($"password longer than {MAX}");
		}

		if (!new Regex(@"[a-z]").IsMatch(password))
		{
			errors.Add("no lowercase letters");
		}

		if (!new Regex(@"[A-Z]").IsMatch(password))
		{
			errors.Add("no uppercase letters");
		}

		if (!new Regex(@"[0-9]").IsMatch(password))
		{
			errors.Add("no numbers");
		}

		if (errors.Count > 0)
		{
			return new(GetErrorMessage(errors));
		}

		return ValidationResult.Success;
	}
}
