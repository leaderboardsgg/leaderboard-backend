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
    private static readonly Faker<RegisterRequest> _registerReqFaker = new AutoFaker<RegisterRequest>()
        .RuleFor(x => x.Username, b => "TestUser" + b.Random.Number(99999))
        .RuleFor(x => x.Password, b => "c00l_pAssword")
        .RuleFor(x => x.Email, b => "TestUser" + b.Internet.Email());

    [Test]
    public async Task Register_ValidRequest_CreatesAndReturnsUser()
    {
        RegisterRequest request = _registerReqFaker.Generate();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Created);
        UserViewModel? content = await res.Content.ReadFromJsonAsync<UserViewModel>();
        content.Should().NotBeNull().And.BeEquivalentTo(new UserViewModel
        {
            Id = content!.Id,
            Username = request.Username
        });

        using IServiceScope scope = _factory.Services.CreateScope();
        using ApplicationContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        User? createdUser = dbContext.Users.FirstOrDefault(u => u.Id == content.Id);
        createdUser.Should().NotBeNull().And.BeEquivalentTo(new User
        {
            Id = content!.Id,
            Password = createdUser!.Password,
            Username = request.Username,
            Email = request.Email,
            Role = UserRole.Registered
        });
    }

    [Test]
    public async Task Register_InvalidEmailFormat_ReturnsErrorCode()
    {
        RegisterRequest request = _registerReqFaker.Generate() with { Email = "not_an_email" };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Email), new[] { "EmailValidator" } }
        });
    }

    [Test]
    public async Task Register_InvalidUsername_ReturnsUsernameFormatErrorCode()
    {
        RegisterRequest request = _registerReqFaker.Generate() with { Username = "山" };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Username), new[] { "UsernameFormat" } }
        });
    }

    [Test]
    public async Task Register_InvalidPassword_ReturnsPasswordFormatErrorCode()
    {
        RegisterRequest request = _registerReqFaker.Generate() with { Password = "a" };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

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
        RegisterRequest createExistingUserReq = _registerReqFaker.Generate();
        await Client.PostAsJsonAsync(Routes.REGISTER, createExistingUserReq);
        RegisterRequest request = _registerReqFaker.Generate() with { Username = createExistingUserReq.Username.ToLower() };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

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
        RegisterRequest createExistingUserReq = _registerReqFaker.Generate();
        await Client.PostAsJsonAsync(Routes.REGISTER, createExistingUserReq);
        RegisterRequest request = _registerReqFaker.Generate() with { Email = createExistingUserReq.Email.ToLower() };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Email), new[] { "EmailAlreadyUsed" } }
        });
    }
}
