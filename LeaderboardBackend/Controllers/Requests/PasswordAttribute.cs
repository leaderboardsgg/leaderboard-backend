using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace LeaderboardBackend.Controllers.Requests
{
	public class PasswordAttribute : ValidationAttribute
	{
		private static readonly int MIN = 8;
		private static readonly int MAX = 80;

		public string GetErrorMessageLength(bool tooShort)
		{
			if (tooShort) {
				return $"Your password needs to be at least {MIN} characters long.";
			}
			return $"Your password needs to be at most {MAX} characters long.";
		}

		public string GetErrorMessageMissing(List<string> missing)
		{
			if (missing.Count == 1) {
				return $"Your password still needs {missing[0]}.";
			}
			if (missing.Count == 2) {
				return $"Your password still needs {missing[0]} and {missing[1]}.";
			}
			return $"Your password still needs {String.Join(", ", missing.GetRange(0, missing.Count - 1).ToArray())}, and {missing[missing.Count - 1]}";
		}

		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			var user = (RegisterRequest)validationContext.ObjectInstance;
			var password = user.Password!;

			// Validate length
			if (password.Length < MIN) {
				return new ValidationResult(GetErrorMessageLength(true));
			} else if (password.Length > MAX) {
				return new ValidationResult(GetErrorMessageLength(false));
			}

			// Validate content
			var missing = new List<string>();

			if (!new Regex(@"[a-z]").IsMatch(password)) {
				missing.Add("a lowercase letter");
			}

			if (!new Regex(@"[A-Z]").IsMatch(password)) {
				missing.Add("an uppercase letter");
			}

			if (!new Regex(@"[0-9]").IsMatch(password)) {
				missing.Add("a number");
			}

			if (missing.Count > 0)
			{
				return new ValidationResult(GetErrorMessageMissing(missing));
			}

			return ValidationResult.Success;
		}
	}
}
