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
using LeaderboardBackend.Test.Lib;
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
    public async Task Register_ValidRequest()
    {
        Mock<IEmailSender> emailSenderMock = new();
        Instant now = Instant.FromUnixTimeSeconds(1);

        using HttpClient client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
                services.AddSingleton<IClock, FakeClock>(_ => new(now));
            })
        )
        .CreateClient();

        RegisterRequest request = _registerReqFaker.Generate();

        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Accepted);

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
        User? createdUser = dbContext.Users.FirstOrDefault(u => u.Username == request.Username);

        createdUser.Should().NotBeNull().And.BeEquivalentTo(new User
        {
            Password = createdUser!.Password,
            Username = request.Username,
            Email = request.Email,
            Role = UserRole.Registered,
            CreatedAt = now
        }, opts => opts.Excluding(u => u.Id));

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
            { nameof(RegisterRequest.Email), [ "EmailValidator" ] }
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
            { nameof(RegisterRequest.Username), [ "UsernameFormat" ] }
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
            { nameof(RegisterRequest.Password), [ "PasswordFormat" ] }
        });
    }

    [Test]
    public async Task Register_UsernameAlreadyTaken_ReturnsConflictAndErrorCode()
    {
        RegisterRequest createExistingUserReq = new()
        {
            Email = "toddparker@example.com",
            Password = "MyPassword1",
            Username = "Todd"
        };

        await Client.PostAsJsonAsync(Routes.REGISTER, createExistingUserReq);
        RegisterRequest request = new()
        {
            Email = "toddjones@example.com",
            Password = "MyPassword2",
            Username = "todd"
        };

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Conflict);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();
        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Username), [ "UsernameTaken" ] }
        });
    }

    [Test]
    public async Task Register_EmailAlreadyUsed_ResendsConfirmationEmailIfRegistered()
    {
        Mock<IEmailSender> emailSender = new();

        using HttpClient client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped(_ => emailSender.Object))
        ).CreateClient();

        RegisterRequest createExistingUserReq = _registerReqFaker.Generate();
        await client.PostAsJsonAsync(Routes.REGISTER, createExistingUserReq);

        RegisterRequest request = _registerReqFaker.Generate() with { Email = createExistingUserReq.Email.ToLower() };
        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Accepted);

        emailSender.Verify(s =>
            s.EnqueueEmailAsync(
                createExistingUserReq.Email,
                "Confirm Your Account",
                It.IsAny<string>()
            ),
            Times.Exactly(2)
        );
    }

    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Administrator)]
    [TestCase(UserRole.Banned)]
    public async Task Register_EmailAlreadyUsed_OtherRoles(UserRole role)
    {
        Mock<IEmailSender> emailSender = new();

        using HttpClient client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped(_ => emailSender.Object)
        )).CreateClient();

        IServiceScope scope = _factory.Services.CreateScope();
        IUserService service = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        CreateUserResult result = await service.CreateUser(new()
        {
            Email = $"testregister.emailused.{role}@example.com",
            Password = "P4ssword",
            Username = $"RegisterTestEmailUsed{role}",
        });

        result.IsT0.Should().BeTrue();
        User user = result.AsT0;
        context.Update(user);
        user!.Role = role;

        await context.SaveChangesAsync();

        RegisterRequest request = _registerReqFaker.Generate() with { Email = $"testregister.emailused.{role}@example.com" };
        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.Accepted);

        emailSender.Verify(s =>
            s.EnqueueEmailAsync(
                $"testregister.emailused.{role}@example.com",
                "A Registration Attempt was Made with Your Email",
                It.IsAny<string>()
            ),
            role is UserRole.Banned ? Times.Never() : Times.Once()
        );
    }

    [Test]
    public async Task Register_EmailAlreadyUsed_EmailServiceFailed()
    {
        Mock<IEmailSender> emailSender = new();
        emailSender.Setup(x =>
            x.EnqueueEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
        ).Throws(new Exception());

        using HttpClient client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddScoped(_ => emailSender.Object)
        )).CreateClient();

        IServiceScope scope = _factory.Services.CreateScope();
        IUserService service = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        CreateUserResult result = await service.CreateUser(new()
        {
            Email = "testregister.emailused.servicefailed@example.com",
            Password = "P4ssword",
            Username = $"RegisterTestEmailUsedServiceFailed",
        });

        result.IsT0.Should().BeTrue();
        User user = result.AsT0;
        context.Update(user);
        user!.Role = UserRole.Confirmed;

        await context.SaveChangesAsync();

        RegisterRequest request = _registerReqFaker.Generate() with { Email = "testregister.emailused.servicefailed@example.com" };
        HttpResponseMessage res = await client.PostAsJsonAsync(Routes.REGISTER, request);

        res.Should().HaveStatusCode(HttpStatusCode.InternalServerError);

        emailSender.Verify(s =>
            s.EnqueueEmailAsync(
                "testregister.emailused.servicefailed@example.com",
                "A Registration Attempt was Made with Your Email",
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }
}
