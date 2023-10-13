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

    [SetUp]
    public void Init()
    {
        _scope = _factory.Services.CreateScope();

        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IClock, FakeClock>(_ => _clock);
            });
        }).CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _factory.ResetDatabase();
        _scope.Dispose();
    }

    [TestCase("not_a_guid")]
    [TestCase("L8msfy9wd0qWbDJMZwwgQg")]
    public async Task TestRecovery_BadRecoveryId(string id)
    {
        HttpResponseMessage res = await _client.GetAsync($"/account/recover/${id}");
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    public async Task TestRecovery_Expired()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(1) + Duration.FromHours(2));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0).Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        await context.AccountRecoveries.AddAsync(recovery);
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public async Task TestRecovery_Old()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        User user = new()
        {
            Email = "test@email.com",
            Password = "password",
            Username = "username",
            Role = UserRole.Confirmed
        };
        await context.Users.AddAsync(user);

        AccountRecovery recovery1 = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0).Plus(Duration.FromHours(1)),
            User = user
        };

        AccountRecovery recovery2 = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(5),
            ExpiresAt = Instant.FromUnixTimeSeconds(5).Plus(Duration.FromHours(1)),
            User = user
        };

        await context.AccountRecoveries.AddRangeAsync(recovery1, recovery2);
        await context.SaveChangesAsync();
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery1.Id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    public async Task TestRecovery_Used()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0).Plus(Duration.FromHours(1)),
            UsedAt = Instant.FromUnixTimeSeconds(5),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        await context.AccountRecoveries.AddAsync(recovery);
        await context.SaveChangesAsync();
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    public async Task TestRecovery_BannedUser()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(1));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0).Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Banned
            }
        };

        await context.AccountRecoveries.AddAsync(recovery);
        await context.SaveChangesAsync();
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    public async Task TestRecovery_Success()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(1));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0).Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        await context.AccountRecoveries.AddAsync(recovery);
        await context.SaveChangesAsync();
        HttpResponseMessage res = await _client.GetAsync(Routes.RecoverAccount(recovery.Id));
        res.Should().HaveStatusCode(HttpStatusCode.OK);
    }
}
