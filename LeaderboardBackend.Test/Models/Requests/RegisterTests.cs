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
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.True(valid);
		Assert.AreEqual(0, results.Count);
	}

	[Test]
	public static void Invalid_PasswordTooShort()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "sH0rt";
		model.PasswordConfirm = "sH0rt";
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.False(valid);
		Assert.AreEqual(1, results.Count);
	}

	[Test]
	public static void Invalid_PasswordTooLong()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "L000000000000000000000000000000000000000000000000000000000000000000000000000000ng";
		model.PasswordConfirm = "L000000000000000000000000000000000000000000000000000000000000000000000000000000ng";
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.False(valid);
		Assert.AreEqual(1, results.Count);
	}

	[Test]
	public static void Invalid_PasswordMismatch()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "P4ssword";
		model.PasswordConfirm = "M1smatch";
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.False(valid);
		Assert.AreEqual(1, results.Count);
	}

	[Test]
	public static void Invalid_PasswordMissingContent()
	{
		RegisterRequest model = GetValidModel();
		model.Password = "password";
		model.PasswordConfirm = "password";
		List<ValidationResult> results = new();
		bool valid = Validator.TryValidateObject(model, new ValidationContext(model), results, true);
		Assert.False(valid);
		Assert.AreEqual(1, results.Count);
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
}
