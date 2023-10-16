using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Fixtures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test.Features.Users;

[TestFixture]
public class ResetPassworTests : IntegrationTestsBase
{
    private AsyncServiceScope _scope;
    private readonly FakeClock _clock = new(Instant.FromUnixTimeSeconds(1));
    private HttpClient _client = null!;
    private int _userNumber;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _userNumber = 0;

        _client = _factory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(
                services => services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        ).CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _client.Dispose();

    [SetUp]
    public void Init()
    {
        _scope = _factory.Services.CreateAsyncScope();
    }

    [TearDown]
    public async Task TearDown() => await _scope.DisposeAsync();

    [TestCase("not_an_id")]
    [TestCase("4BZgqaqRPEC7CKykWc0b2g")]
    public async Task ResetPassword_BadId(string id)
    {
        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ResetPassword_Expired()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(0) + Duration.FromHours(2));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("password", recovery.User.Password).Should().BeTrue();
        recovery.User.Password.Should().Be(BCryptNet.EnhancedHashPassword("password"));
    }

    [Test]
    public async Task ResetPassword_NotMostRecent()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        User user = new()
        {
            Email = $"pwdresettestuser{userNumber}@email.com",
            Password = BCryptNet.EnhancedHashPassword("password"),
            Username = $"pwdresettestuser{userNumber}",
            Role = UserRole.Confirmed
        };

        AccountRecovery recovery1 = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = user
        };

        AccountRecovery recovery2 = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(5),
            ExpiresAt = Instant.FromUnixTimeSeconds(5) + Duration.FromHours(1),
            User = user
        };

        context.AccountRecoveries.AddRange(recovery1, recovery2);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery1.Id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery1 = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery1.Id);
        recovery1.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("password", recovery1.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_AlreadyUsed()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            UsedAt = Instant.FromUnixTimeSeconds(5),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().Be(Instant.FromUnixTimeSeconds(5));
        BCryptNet.EnhancedVerify("password", recovery.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_Banned()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.NotFound);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("password", recovery.User.Password).Should().BeTrue();
    }

    [TestCase("OuxNzURtWdXWd", Description = "No number")]
    [TestCase("DZWVZVV5ED8QE", Description = "No lowercase letter")]
    [TestCase("y267pmi50skcc", Description = "No uppercase letter")]
    [TestCase("zmgoyGS", Description = "7 characters")]
    [TestCase("qutOboNSzYplEKCDlCEbGPIEtMEnJImHwnluHvksTZbhuHSwFLpvUZQQxIdHctldJkdEVMRyiWcyuIeBe",
        Description = "81 characters")]
    public async Task ResetPassword_BadPassword(string pwd)
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = pwd
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.UnprocessableEntity);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().BeNull();
        BCryptNet.EnhancedVerify("password", recovery.User.Password).Should().BeTrue();
    }

    [Test]
    public async Task ResetPassword_SamePassword()
    {
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = UserRole.Confirmed
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "password"
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
        _clock.Reset(Instant.FromUnixTimeSeconds(10));
        ApplicationContext context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        int userNumber = _userNumber++;

        AccountRecovery recovery = new()
        {
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            ExpiresAt = Instant.FromUnixTimeSeconds(0) + Duration.FromHours(1),
            User = new()
            {
                Email = $"pwdresettestuser{userNumber}@email.com",
                Password = BCryptNet.EnhancedHashPassword("password"),
                Username = $"pwdresettestuser{userNumber}",
                Role = role
            }
        };

        context.AccountRecoveries.Add(recovery);
        await context.SaveChangesAsync();

        HttpResponseMessage res = await Client.PostAsJsonAsync(Routes.RecoverAccount(recovery.Id), new ChangePasswordRequest
        {
            Password = "AValidPassword"
        });

        res.Should().HaveStatusCode(System.Net.HttpStatusCode.OK);
        context.ChangeTracker.Clear();
        recovery = await context.AccountRecoveries.Include(ar => ar.User).SingleAsync(ar => ar.Id == recovery.Id);
        recovery.UsedAt.Should().Be(Instant.FromUnixTimeSeconds(10));
        BCryptNet.EnhancedVerify("AValidPassword", recovery.User.Password).Should().BeTrue();
    }
}
