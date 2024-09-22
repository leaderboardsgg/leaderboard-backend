using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

public class TestRecoveryTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;
    private HttpClient _client = null!;
    private readonly FakeClock _clock = new(Instant.FromUnixTimeSeconds(1));

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        ).CreateClient();
    }

    [SetUp]
    public void Init()
    {
        _factory.ResetDatabase();
        _scope = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        ).Services.CreateScope();
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [TestCase("not_a_guid")]
    [TestCase("L8msfy9wd0qWbDJMZwwgQg")]
    public async Task TestRecovery_BadRecoveryId(string id)
    {
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TestRecovery_Expired()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant().Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();
        _clock.AdvanceHours(2);
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TestRecovery_Old()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        User user = new()
        {
            Email = "test@email.com",
            Password = "password",
            Username = "username",
            Role = UserRole.Confirmed
        };
        context.Users.Add(user);

        AccountRecovery recovery1 = new()
        {
            ExpiresAt = _clock.GetCurrentInstant().Plus(Duration.FromHours(1)),
            User = user
        };

        context.AccountRecoveries.Add(recovery1);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);

        AccountRecovery recovery2 = new()
        {
            ExpiresAt = _clock.GetCurrentInstant().Plus(Duration.FromHours(1)),
            User = user
        };

        context.AccountRecoveries.Add(recovery2);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery1.Id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task TestRecovery_Used()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant().Plus(Duration.FromHours(1)),
            UsedAt = _clock.GetCurrentInstant().Plus(Duration.FromMinutes(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(2);
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [TestCase(UserRole.Administrator, HttpStatusCode.OK)]
    [TestCase(UserRole.Banned, HttpStatusCode.NotFound)]
    [TestCase(UserRole.Confirmed, HttpStatusCode.OK)]
    [TestCase(UserRole.Registered, HttpStatusCode.OK)]
    public async Task TestRecovery_Roles(UserRole role, HttpStatusCode expected)
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant().Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = role
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();
        recovery.CreatedAt.Should().Be(_clock.GetCurrentInstant());
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.Should().HaveStatusCode(expected);
    }
}
