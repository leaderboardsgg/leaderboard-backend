using FluentValidation;
using FluentValidation.TestHelper;
using LeaderboardBackend.Models.Validation;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

public class UsernameRuleTests
{
    private readonly TestValidator _sut = new();

    [TestCase("testuser")]
    [TestCase("testuser123")]
    [TestCase("test_user_123")]
    [TestCase("test-user-123")]
    [TestCase("test_user-123")]
    [TestCase("o'test_user123")]
    [TestCase("123")]
    [TestCase("123test")]
    public void ValidUsername(string username)
    {
        TestValidationResult<ExampleObject> result = _sut.TestValidate(new ExampleObject(username));

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [TestCase("testuser_")]
    [TestCase("_testuser")]
    [TestCase("'testuser")]
    [TestCase("testuser'")]
    [TestCase("-testuser")]
    [TestCase("testuser-")]
    [TestCase("_'-testuser")]
    [TestCase("testuser-'_")]
    [TestCase("test_-user")]
    [TestCase("user富士山")]
    [TestCase("a", Description = "1 character")]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaa", Description = "26 characters")]
    public void InvalidUsernameFormat(string username)
    {
        TestValidationResult<ExampleObject> result = _sut.TestValidate(new ExampleObject(username));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorCode(UsernameRule.USERNAME_FORMAT);
    }
    private record ExampleObject(string Username);

    private class TestValidator : AbstractValidator<ExampleObject>
    {
        public TestValidator() => RuleFor(x => x.Username).Username();
    }
}
