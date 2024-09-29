using FluentValidation;
using FluentValidation.TestHelper;
using LeaderboardBackend.Models.Validation;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

public class UserPasswordTests
{
    private readonly TestValidator _sut = new();

    [TestCase("MWqVR7vHLZvizTLQX7")]
    [TestCase("5#SP:|2^U?2PGt.fv1", Description = "contains special characters")]
    [TestCase("cA8RlO8i", Description = "8 characters (minimum")]
    [TestCase("9cAf9eQIt3zSNCJFfGzAX5Fu2EjCxf8r11biqEl4gKAyDcsqjyJcwkgu8hEivJoc9FQqUtGpWaNdpzVc",
        Description = "80 characters (maximum)")]
    public void ValidPassword(string password)
    {
        TestValidationResult<ExampleObject> result = _sut.TestValidate(new ExampleObject(password));

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [TestCase("OuxNzURtWdXWd", Description = "No number")]
    [TestCase("DZWVZVV5ED8QE", Description = "No lowercase letter")]
    [TestCase("y267pmi50skcc", Description = "No uppercase letter")]
    [TestCase("zmgoyGS", Description = "7 characters")]
    [TestCase("qutOboNSzYplEKCDlCEbGPIEtMEnJImHwnluHvksTZbhuHSwFLpvUZQQxIdHctldJkdEVMRyiWcyuIeBe",
        Description = "81 characters")]
    public void InvalidPasswordFormat(string password)
    {
        TestValidationResult<ExampleObject> result = _sut.TestValidate(new ExampleObject(password));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode(UserPasswordRule.PASSWORD_FORMAT);
    }

    [Test]
    public void NullPassword()
    {
        TestValidationResult<ExampleObject> result = _sut.TestValidate(new ExampleObject());

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorCode("NotEmptyValidator");
    }

    private record ExampleObject(string? Password = null);

    private class TestValidator : AbstractValidator<ExampleObject>
    {
        public TestValidator() => RuleFor(x => x.Password).UserPassword();
    }
}
