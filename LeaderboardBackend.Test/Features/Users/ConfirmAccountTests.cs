using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class ConfirmAccountTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;
    private readonly FakeClock _clock = new(Instant.FromUnixTimeSeconds(1));
    private HttpClient _client = null!;

    [SetUp]
    public void Init()
    {
        _scope = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IClock, FakeClock>(_ => _clock);
            });
        }).Services.CreateScope();

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

    [Test]
    public async Task ConfirmAccount_BadConfirmationId()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        AccountConfirmation confirmation = new()
        {
            ExpiresAt = now.Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
            }
        };

        context.AccountConfirmations.Add(confirmation);
        await context.SaveChangesAsync();
        confirmation.CreatedAt.Should().Be(now);
        HttpResponseMessage res = await _client.PutAsync(Routes.ConfirmAccount(Guid.NewGuid()), null);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        User? user = await context.Users.FindAsync(confirmation.UserId);
        user!.Role.Should().Be(UserRole.Registered);
    }

    [Test]
    public async Task ConfirmAccount_MalformedConfirmationId()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(1));
        HttpResponseMessage res = await _client.PutAsync("/account/confirm/not_a_guid", null);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ConfirmAccount_BadRole()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        AccountConfirmation confirmation = new()
        {
            ExpiresAt = now.Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
                Role = UserRole.Confirmed
            }
        };

        context.AccountConfirmations.Add(confirmation);
        await context.SaveChangesAsync();
        confirmation.CreatedAt.Should().Be(now);
        HttpResponseMessage res = await _client.PutAsync(Routes.ConfirmAccount(confirmation.Id), null);
        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
        context.ChangeTracker.Clear();
        AccountConfirmation? conf = await context.AccountConfirmations.FindAsync(confirmation.Id);
        conf!.UsedAt.Should().BeNull();
    }

    [Test]
    public async Task ConfirmAccount_Expired()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountConfirmation confirmation = new()
        {
            ExpiresAt = now.Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
            }
        };

        context.AccountConfirmations.Add(confirmation);
        await context.SaveChangesAsync();
        _clock.Reset(now + Duration.FromHours(2));
        HttpResponseMessage res = await _client.PutAsync(Routes.ConfirmAccount(confirmation.Id), null);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        AccountConfirmation? conf = await context.AccountConfirmations.Include(c => c.User).SingleOrDefaultAsync(c => c.Id == confirmation.Id);
        conf!.UsedAt.Should().BeNull();
        conf!.User.Role.Should().Be(UserRole.Registered);
    }

    [Test]
    public async Task ConfirmAccount_AlreadyUsed()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        AccountConfirmation confirmation = new()
        {
            ExpiresAt = now.Plus(Duration.FromHours(1)),
            UsedAt = now.Plus(Duration.FromSeconds(5)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
            }
        };

        context.AccountConfirmations.Add(confirmation);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);
        HttpResponseMessage res = await _client.PutAsync(Routes.ConfirmAccount(confirmation.Id), null);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        User? user = await context.Users.FindAsync(confirmation.UserId);
        user!.Role.Should().Be(UserRole.Registered);
    }

    [Test]
    public async Task ConfirmAccount_Success()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);

        AccountConfirmation confirmation = new()
        {
            ExpiresAt = now.Plus(Duration.FromHours(1)),
            User = new()
            {
                Email = "test@email.com",
                Password = "password",
                Username = "username",
            }
        };

        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        context.AccountConfirmations.Add(confirmation);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(5);
        HttpResponseMessage res = await _client.PutAsync(Routes.ConfirmAccount(confirmation.Id), null);
        res.Should().HaveStatusCode(HttpStatusCode.OK);
        context.ChangeTracker.Clear();
        AccountConfirmation? conf = await context.AccountConfirmations.Include(c => c.User).SingleOrDefaultAsync(c => c.Id == confirmation.Id);
        conf!.UsedAt.Should().Be(now.Plus(Duration.FromMinutes(5)));
        conf!.User.Role.Should().Be(UserRole.Confirmed);
    }
}
