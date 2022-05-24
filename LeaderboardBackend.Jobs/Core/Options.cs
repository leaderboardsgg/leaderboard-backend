using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Jobs.Core;

internal static class Options
{
	public static string StringLine(string optionName, Func<string?, bool>? validator = null, string invalidMessage = "Invalid input.")
	{
		if (validator is null)
		{
			validator = s => !string.IsNullOrWhiteSpace(s);
		}

		string? input = null;
		do
		{
			Console.Write($"{optionName}: ");
			input = Console.ReadLine();
			if (!validator(input))
			{
				Console.WriteLine(invalidMessage);
			}
		} while (!validator(input));

		return input ?? "";
	}

	public static bool YesOrNo(string question)
	{
		Console.Write($"{question} (y/[N]): ");
		string? input = Console.ReadLine();
		return input is not null && input.Trim() == "y";
	}

	public static User User()
	{
		Console.WriteLine("Please provide the new user's information.");

		RegisterRequest request = new();
		bool valid = false;
		do
		{
			string username = StringLine("Username");
			string email = StringLine("Email");
			string password = StringLine("Password");
			string passwordConfirm = StringLine(
				"Confirm Password",
				validator: s => !string.IsNullOrWhiteSpace(s) && s == password,
				invalidMessage: "Passwords must match"
			);
			request = new()
			{
				Username = username,
				Email = email,
				Password = password,
				PasswordConfirm = passwordConfirm,
			};
			List<ValidationResult> errors = new();
			ValidationContext ctx = new(request);
			valid = Validator.TryValidateObject(request, ctx, errors, validateAllProperties: true);
			if (errors.Count > 0)
			{
				Console.WriteLine($"Entity was invalid: {string.Join('/', errors.Select(r => r.ErrorMessage))}");
			}
		} while (!valid);

		return new User
		{
			Username = request.Username,
			Email = request.Email,
			Password = BCryptNet.EnhancedHashPassword(request.Password)
		};
	}
}
