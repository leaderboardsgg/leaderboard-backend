using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LeaderboardBackend.Controllers.Requests
{
	public class PasswordAttribute : ValidationAttribute
	{
		private static readonly int MIN = 8;
		private static readonly int MAX = 80;

		public string GetErrorMessage(List<string> errors) =>
			$"Your password has the following errors: {string.Join("; ", errors)}";

		protected override ValidationResult? IsValid(object? value, ValidationContext _)
		{
			string password = (string)value!;

			List<string> errors = new();

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
				return new ValidationResult(GetErrorMessage(errors));
			}

			return ValidationResult.Success;
		}
	}
}
