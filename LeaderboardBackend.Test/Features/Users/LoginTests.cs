using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Fixtures;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

public class LoginTests : IntegrationTestsBase
{
    [OneTimeSetUp]
    public async Task Init()
    {
        await _factory.ResetDatabase();

        // TODO: Swap to creating users via the UserService instead of calling the DB, once
        // it has the ability to change a user's roles.
        using IServiceScope s = _factory.Services.CreateScope();
        ApplicationContext dbContext = s.ServiceProvider.GetRequiredService<ApplicationContext>();
        dbContext.Users.AddRange(new[]
        {
            new User{
                Email = "valid@user.com",
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                Username = "Test_User",
            },
            new User{
                Email = "banned@user.com",
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword("P4ssword"),
                Role = UserRole.Banned,
                Username = "Banned_User",
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

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.LOGIN, request);

        res.Should().HaveStatusCode(HttpStatusCode.OK);
        LoginResponse? content = await res.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();

        using IServiceScope s = _factory.Services.CreateScope();
        JwtConfig jwtConfig = s.ServiceProvider.GetRequiredService<IOptions<JwtConfig>>().Value;
        TokenValidationParameters parameters = Jwt.ValidationParameters.GetInstance(jwtConfig);

        Jwt.SecurityTokenHandler.ValidateToken(content!.Token, parameters, out _).Should().BeOfType<ClaimsPrincipal>();
    }

    [TestCase(null, null, "NotNullValidator", "NotEmptyValidator", Description = "Null email + password")]
    [TestCase("ee", "ff", "EmailValidator", null, Description = "Invalid email + password")]
    [TestCase("ee", "P4ssword", "EmailValidator", null, Description = "Null email + valid password")]
    public async Task Login_InvalidRequest_Returns422(
        string? email,
        string? password,
        string? emailErrorCode,
        string? passwordErrorCode)
    {
        LoginRequest request = new()
        {
            Email = email!,
            Password = password!,
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.LOGIN, request);

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
    public async Task Login_InvalidRequest_Returns400()
    {
        HttpResponseMessage res = await Client.PostAsync(
            Routes.LOGIN,
            new StringContent("\"", new MediaTypeHeaderValue("application/json"))
        );
        res.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }

    [TestCase("valid@user.com", "Inc0rrectPassword", HttpStatusCode.Unauthorized, Description = "Wrong password")]
    [TestCase("banned@user.com", "Inc0rrectPassword", HttpStatusCode.Forbidden, Description = "Banned user")]
    [TestCase("unknown@user.com", "Inc0rrectPassword", HttpStatusCode.NotFound, Description = "Wrong email")]
    public async Task Login_InvalidRequest_OtherErrors(string email, string password, HttpStatusCode statusCode)
    {
        LoginRequest request = new()
        {
            Email = email,
            Password = password,
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.LOGIN, request);

        res.Should().HaveStatusCode(statusCode);
    }
}
