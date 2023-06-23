using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

public class RegistrationTests : IntegrationTestsBase
{
    private const string REGISTER_URI = "/api/users/register";

    private static readonly Faker<RegisterRequest> s_registerReqFaker = new AutoFaker<RegisterRequest>()
        .RuleFor(x => x.Username, b => "TestUser" + b.Random.Number(99999))
        .RuleFor(x => x.Password, b => "c00l_pAssword")
        .RuleFor(x => x.Email, b => "TestUser" + b.Internet.Email());

    [Test]
    public async Task Register_ValidRequest_CreatesAndReturnsUser()
    {
        RegisterRequest request = s_registerReqFaker.Generate();

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.Created);
        UserViewModel? content = await res.Content.ReadFromJsonAsync<UserViewModel>();
        content.Should().NotBeNull().And.BeEquivalentTo(new UserViewModel
        {
            Id = content!.Id,
            Username = request.Username
        });

        using IServiceScope scope = s_factory.Services.CreateScope();
        using ApplicationContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        User? createdUser = dbContext.Users.FirstOrDefault(u => u.Id == content.Id);
        createdUser.Should().NotBeNull().And.BeEquivalentTo(new User
        {
            Id = content!.Id,
            Password = createdUser!.Password,
            Username = request.Username,
            Email = request.Email,
            Admin = false
        });
    }

    [Test]
    public async Task Register_InvalidEmailFormat_ReturnsErrorCode()
    {
        RegisterRequest request = s_registerReqFaker.Generate() with { Email = "not_an_email" };

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Email), new[] { "EmailValidator" } }
        });
    }

    [TestCase("a", Description = "Username too short")]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaa", Description = "Username too long")]
    [TestCase("user富士山", Description = "Invalid username format")]
    public async Task Register_InvalidUsername_ReturnsUsernameFormatErrorCode(string username)
    {
        RegisterRequest request = s_registerReqFaker.Generate() with { Username = username };

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Username), new[] { "UsernameFormat" } }
        });
    }

    [TestCase("hyF4x8Y", Description = "Password too short")]
    [TestCase("Vat1bsMncIiC5xuQtDKzhrR7cS0c4mT5nNVjBBHGShA6joc8E3JuTHnNIO3NhoPH36Au102CENGADo0sO",
        Description = "Password too long")]
    [TestCase("EsoXcnUOMrek", Description = "No number")]
    [TestCase("QJGEW1LVUM2H", Description = "No lowercase letter")]
    [TestCase("hvcae76zgnad", Description = "No uppercase letter")]
    public async Task Register_InvalidPassword_ReturnsPasswordFormatErrorCode(string password)
    {
        RegisterRequest request = s_registerReqFaker.Generate() with { Password = password };

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Password), new[] { "PasswordFormat" } }
        });
    }

    [Test]
    public async Task Register_UsernameAlreadyTaken_ReturnsConflictAndErrorCode()
    {
        RegisterRequest createExistingUserReq = s_registerReqFaker.Generate();
        await Client.PostAsJsonAsync(REGISTER_URI, createExistingUserReq);
        RegisterRequest request = s_registerReqFaker.Generate() with { Username = createExistingUserReq.Username.ToLower() };

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Username), new[] { "UsernameTaken" } }
        });
    }

    [Test]
    public async Task Register_EmailAlreadyUsed_ReturnsConflictAndErrorCode()
    {
        RegisterRequest createExistingUserReq = s_registerReqFaker.Generate();
        await Client.PostAsJsonAsync(REGISTER_URI, createExistingUserReq);
        RegisterRequest request = s_registerReqFaker.Generate() with { Email = createExistingUserReq.Email.ToLower() };

        HttpResponseMessage res = await Client.PostAsJsonAsync(REGISTER_URI, request);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Email), new[] { "EmailAlreadyUsed" } }
        });
    }
}
