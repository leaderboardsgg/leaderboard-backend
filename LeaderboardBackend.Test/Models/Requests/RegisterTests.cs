using LeaderboardBackend.Controllers.Requests;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Test.Models.Requests;

[TestFixture]
internal class RegisterTests
{
	[Test]
	public static void Valid()
	{
		RegisterRequest model = GetValidModel();
		ValidationTest(model);
	}

	[Test]
	public static void Invalid_PasswordTooShort()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "sH0rt";
		model.PasswordConfirm = "sH0rt";
		ValidationTest(model, expectedErrors: 1);
	}

	[Test]
	public static void Invalid_PasswordTooLong()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "L000000000000000000000000000000000000000000000000000000000000000000000000000000ng";
		model.PasswordConfirm = "L000000000000000000000000000000000000000000000000000000000000000000000000000000ng";
		ValidationTest(model, expectedErrors: 1);
	}

	[Test]
	public static void Invalid_PasswordMismatch()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "P4ssword";
		model.PasswordConfirm = "M1smatch";
		ValidationTest(model, expectedErrors: 1);
	}

	[Test]
	public static void Invalid_PasswordMissingContent()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "password";
		model.PasswordConfirm = "password";
		ValidationTest(model, expectedErrors: 1);
	}

	[Test]
	public static void Invalid_EveryPropertyWrong()
	{
		RegisterRequest terribleModel = new()
		{
			Username = "_)(*",
			Email = "notanemail",
			Password = "password",
			PasswordConfirm = "differentpassword",
		};
		ValidationTest(terribleModel, expectedErrors: 4);
	}

	private static RegisterRequest GetValidModel()
	{
		return new RegisterRequest()
		{
			Username = "ValidUsername",
			Email = "valid@email.com",
			Password = "valid_passW0rd",
			PasswordConfirm = "valid_passW0rd"
		};
	}

	private static void ValidationTest(RegisterRequest model, int expectedErrors = 0)
	{
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.AreEqual(valid, expectedErrors == 0);
		Assert.AreEqual(expectedErrors, results.Count);
	}
}
