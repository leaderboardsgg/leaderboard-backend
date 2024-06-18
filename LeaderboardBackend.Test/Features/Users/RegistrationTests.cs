using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using NodaTime.Testing;
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
        Mock<IEmailSender> emailSenderMock = new();
        using HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
                services.AddSingleton<IClock, FakeClock>(_ => new(Instant.FromUnixTimeSeconds(1)));
            });
        })
        .CreateClient();
        RegisterRequest request = _registerReqFaker.Generate();

        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Created);
        UserViewModel? content = await res.Content.ReadFromJsonAsync<UserViewModel>();
        content.Should().NotBeNull().And.BeEquivalentTo(new UserViewModel
        {
            Id = content!.Id,
            Username = request.Username,
            Role = UserRole.Registered
        });
        emailSenderMock.Verify(x =>
            x.EnqueueEmailAsync(
                It.IsAny<string>(),
                "Confirm Your Account",
                It.IsAny<string>()
            ),
            Times.Once()
        );

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
        AccountConfirmation confirmation = dbContext.AccountConfirmations.First(c => c.UserId == createdUser.Id);
        confirmation.Should().NotBeNull();
        confirmation.CreatedAt.ToUnixTimeSeconds().Should().Be(1);
        confirmation.UsedAt.Should().BeNull();
        Instant.Subtract(confirmation.ExpiresAt, confirmation.CreatedAt).Should().Be(Duration.FromHours(1));
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
    public async Task Register_EmailFailedToSend_ReturnsErrorCode()
    {
        Mock<IEmailSender> emailSenderMock = new();
        emailSenderMock.Setup(x =>
            x.EnqueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
        ).Throws(new Exception());

        HttpClient client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped(_ => emailSenderMock.Object)
            )
        ).CreateClient();
        RegisterRequest request = _registerReqFaker.Generate();

        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.InternalServerError);
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
