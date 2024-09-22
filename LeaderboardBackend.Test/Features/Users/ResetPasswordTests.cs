using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class ResetPasswordTests : IntegrationTestsBase
{
    private IServiceScope _scope = null!;
    private FakeClock _clock = null!;
    private HttpClient _client = null!;
    private int _userNumber;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _userNumber = 0;
        _clock = new(Instant.FromUnixTimeSeconds(10) + Duration.FromHours(1));

        _client = _factory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(
                services => services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        ).CreateClient();
    }

    [SetUp]
    public void Init()
    {
        _scope = _factory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(
                services => services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        ).Services.CreateScope();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
    }

    [TestCase("not_an_id")]
    [TestCase("4BZgqaqRPEC7CKykWc0b2g")]
    public async Task ResetPassword_BadId(string id)
    {
        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ResetPassword_Expired()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        _clock.AdvanceHours(2);

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("P4ssword", recovery.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_NotMostRecent()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        User user = new()
        {
            Email = $"pwdresettestuser{userNumber}@email.com",
            Password = BCryptNet.EnhancedHashPassword("P4ssword"),
            Username = $"pwdresettestuser{userNumber}",
            Role = UserRole.Confirmed
        };

        AccountRecovery recovery1 = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = user
        };

        context.AccountRecoveries.Add(recovery1);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);

        AccountRecovery recovery2 = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = user
        };

        context.AccountRecoveries.Add(recovery2);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery1.Id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery1 = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery1.Id);
        recovery1.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("P4ssword", recovery1.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_AlreadyUsed()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            UsedAt = _clock.GetCurrentInstant() + Duration.FromMinutes(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(2);

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().Be(_clock.GetCurrentInstant() - Duration.FromMinutes(1));
        BCryptNet.EnhancedVerify("P4ssword", recovery.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_Banned()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Banned
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.Forbidden);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("P4ssword", recovery.User.Password).Should().BeTrue();
    }

    [TestCase("OuxNzURtWdXWd", Description = "No number")]
    [TestCase("DZWVZVV5ED8QE", Description = "No lowercase letter")]
    [TestCase("y267pmi50skcc", Description = "No uppercase letter")]
    [TestCase("zmgoyGS", Description = "7 characters")]
    [TestCase("qutOboNSzYplEKCDlCEbGPIEtMEnJImHwnluHvksTZbhuHSwFLpvUZQQxIdHctldJkdEVMRyiWcyuIeBe",
        Description = "81 characters")]
    public async Task ResetPassword_BadPassword(string pwd)
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = pwd
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);
        ValidationProblemDetails? content = await res.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        content.Should().NotBeNull();

        content!.Errors.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            { nameof(RegisterRequest.Password), new[] { "PasswordFormat" } }
        });

        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("P4ssword", recovery.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_SamePassword()
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "P4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.Conflict);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
    }

    [TestCase(UserRole.Administrator)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task ResetPassword_Success(UserRole role)
    {
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            ExpiresAt = _clock.GetCurrentInstant() + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = $"pwdresettestuser{userNumber}",
                Role = role
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);

        HttpResponseMessage res = await _client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidP4ssword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.OK);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().Be(_clock.GetCurrentInstant());
        BCryptNet.EnhancedVerify("AValidP4ssword", recovery.User.Password).Should().BeTrue();
    }
}
