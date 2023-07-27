using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
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
    private const string BANNED_EMAIL = "banned@user.com";
    private const string VALID_PASSWORD = "P4ssword";

    [OneTimeSetUp]
    public void Init()
    {
        s_factory.ResetDatabase();

        // Swap to creating users via the UserService instead of calling the DB, once
        // it has the ability to change a user's roles.
        using IServiceScope s = s_factory.Services.CreateScope();
        ApplicationContext dbContext = s.ServiceProvider.GetRequiredService<ApplicationContext>();
        dbContext.Users.AddRange(new[]
        {
            new User{
                Email = VALID_EMAIL,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(VALID_PASSWORD),
                Username = "Test User",
            },
            new User{
                Email = BANNED_EMAIL,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(VALID_PASSWORD),
                Role = UserRole.Banned,
                Username = "Banned User",
            },
        });
        dbContext.SaveChanges();
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

    [Test]
    public async Task Login_InvalidRequest_400Error()
    {
        HttpResponseMessage res = await Client.PostAsync(
            LOGIN_URI,
            new StringContent("\"", new MediaTypeHeaderValue("application/json"))
        );
        res.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase(VALID_EMAIL, "Inc0rrectPassword", HttpStatusCode.Unauthorized, Description = "Wrong password")]
    [TestCase(BANNED_EMAIL, "Inc0rrectPassword", HttpStatusCode.Forbidden, Description = "Banned user")]
    [TestCase("unknown@user.com", "Inc0rrectPassword", HttpStatusCode.NotFound, Description = "Wrong email")]
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
