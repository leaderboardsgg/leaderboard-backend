using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Fixtures;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

public class LoginTests : IntegrationTestsBase
{
    private const string LOGIN_URI = "/login";
    private const string VALID_EMAIL = "valid@user.com";
    private const string VALID_PASSWORD = "P4ssword";
    private IUserService _userService = null!;

    [OneTimeSetUp]
    public async Task Init()
    {
        s_factory.ResetDatabase();
        using IServiceScope scope = s_factory.Services.CreateScope();
        _userService = scope.ServiceProvider.GetService<IUserService>()!;
        await _userService.CreateUser(new()
        {
            Email = VALID_EMAIL,
            Password = VALID_PASSWORD,
            Username = "Test User",
        });
    }

    [Test]
    public async Task Login_ValidRequest_ReturnsLoginResponse()
    {
        LoginRequest request = new()
        {
            Email = TestInitCommonFields.Admin.Email,
            Password = TestInitCommonFields.Admin.Password,
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(LOGIN_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.OK);
        LoginResponse? content = await res.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().MatchRegex(@"^[A-Za-z0-9-_=]{36}\.[A-Za-z0-9-_=]{190}\.[A-Za-z0-9-_=]{43}$");
    }

    [TestCase(null, null, "NotNullValidator", "NotNullValidator", Description = "Null email + password")]
    [TestCase("ee", "ff", "EmailValidator", UserPasswordRule.PASSWORD_FORMAT, Description = "Invalid email + password")]
    [TestCase("ee", VALID_PASSWORD, "EmailValidator", null, Description = "Null email + valid password")]
    [TestCase(VALID_EMAIL, "ff", null, UserPasswordRule.PASSWORD_FORMAT, Description = "Valid email + null password")]
    public async Task Login_InvalidRequest_Returns422UnprocessableEntity(
        string? email,
        string? password,
        string? emailErrorCode,
        string? passwordErrorCode)
    {
        // We should figure out how to have an UnvalidatedLoginRequest w/ null fields and a
        // LoginRequest with non-null fields, so we don't have to force null coalescence - zysim
        LoginRequest request = new()
        {
            Email = email!,
            Password = password!,
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(LOGIN_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        if (emailErrorCode is not null)
        {
            content!.Errors[nameof(LoginRequest.Email)].Should().Equal(emailErrorCode);
        }

        if (passwordErrorCode is not null)
        {
            content!.Errors[nameof(LoginRequest.Password)].Should().Equal(passwordErrorCode);
        }
    }

    [TestCase(VALID_EMAIL, "Inc0rrectPassword", HttpStatusCode.Unauthorized)]
    [TestCase("unknown@user.com", "Inc0rrectPassword", HttpStatusCode.NotFound)]
    public async Task Login_InvalidRequest_OtherErrors(string email, string password, HttpStatusCode statusCode)
    {
        LoginRequest request = new()
        {
            Email = email,
            Password = password,
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(LOGIN_URI, request);

        res.Should().HaveStatusCode(statusCode);
    }
}
