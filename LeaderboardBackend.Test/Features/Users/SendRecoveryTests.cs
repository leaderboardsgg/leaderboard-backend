using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class SendRecoveryTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;

    [SetUp]
    public void Init()
    {
        _scope = _factory.Services.CreateScope();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.ResetDatabase();
        _scope.Dispose();
    }

    public async Task SendRecoveryEmail_MalformedMissingUsername()
    {
        Mock<IEmailSender> emailSenderMock = new();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
            });
        }).CreateClient();

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        User user = new()
        {
            Email = "test@email.com",
            Username = "username",
            Password = "password",
            Role = UserRole.Confirmed
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(
            Routes.RECOVER_ACCOUNT,
            new
            {
                Email = user.Email
            }
        );

        res.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        context.ChangeTracker.Clear();

        AccountRecovery? recovery = await context.AccountRecoveries.FirstOrDefaultAsync(
            ar => ar.UserId == user.Id
        );

        recovery.Should().BeNull();
        emailSenderMock.Verify(
            m => m.EnqueueEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    public async Task SendRecoveryEmail_MalformedMissingEmail()
    {
        Mock<IEmailSender> emailSenderMock = new();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
            });
        }).CreateClient();

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        User user = new()
        {
            Email = "test@email.com",
            Username = "username",
            Password = "password",
            Role = UserRole.Confirmed
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(
            Routes.RECOVER_ACCOUNT,
            new
            {
                Username = "username"
            }
        );

        res.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        context.ChangeTracker.Clear();

        AccountRecovery? recovery = await context.AccountRecoveries.FirstOrDefaultAsync(
            ar => ar.UserId == user.Id
        );

        recovery.Should().BeNull();
        emailSenderMock.Verify(
            m => m.EnqueueEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Registered)]
    public async Task SendRecoveryEmail_BadRole(UserRole role)
    {
        Mock<IEmailSender> emailSenderMock = new();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
            });
        }).CreateClient();

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        User user = new()
        {
            Email = "test@email.com",
            Password = "password",
            Username = "username",
            Role = role
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(
            Routes.RECOVER_ACCOUNT,
            new RecoverAccountRequest
            {
                Email = "test@email.com",
                Username = "username"
            }
        );

        res.Should().HaveStatusCode(HttpStatusCode.OK);
        context.ChangeTracker.Clear();

        AccountRecovery? recovery = await context.AccountRecoveries.SingleOrDefaultAsync(
            ar => ar.UserId == user.Id
        );

        recovery.Should().BeNull();
        emailSenderMock.Verify(
            m => m.EnqueueEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Test]
    public async Task SendRecoveryEmail_UserNotPresent()
    {
        Mock<IEmailSender> emailSenderMock = new();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
            });
        })
        .CreateClient();

        HttpResponseMessage res = await client.PostAsJsonAsync(
            Routes.RECOVER_ACCOUNT,
            new RecoverAccountRequest
            {
                Email = "test@email.com",
                Username = "username"
            }
        );

        res.Should().HaveStatusCode(HttpStatusCode.OK);

        emailSenderMock.Verify(m => m.EnqueueEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Test]
    public async Task SendRecoveryEmail_Success()
    {
        Mock<IEmailSender> emailSenderMock = new();

        HttpClient client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => emailSenderMock.Object);
                services.AddSingleton<IClock, FakeClock>(_ => new(Instant.FromUnixTimeSeconds(0)));
            });
        }).CreateClient();

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        User user = new()
        {
            Email = "test@email.com",
            Username = "username",
            Password = "password",
            Role = UserRole.Confirmed
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await client.PostAsJsonAsync(
            Routes.RECOVER_ACCOUNT,
            new RecoverAccountRequest
            {
                Email = "test@email.com",
                Username = "username"
            }
        );

        res.Should().HaveStatusCode(HttpStatusCode.OK);
        context.ChangeTracker.Clear();

        AccountRecovery? recovery = await context.AccountRecoveries.FirstOrDefaultAsync(
            ar => ar.UserId == user.Id
        );

        recovery.Should().NotBeNull();
        recovery!.CreatedAt.Should().Be(Instant.FromUnixTimeSeconds(0));
        recovery!.ExpiresAt.Should().Be(Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1));

        emailSenderMock.Verify(
            m => m.EnqueueEmailAsync(user.Email, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once()
        );
    }
}
