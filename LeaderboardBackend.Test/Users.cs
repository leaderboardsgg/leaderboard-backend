using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions.Specialized;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
public class Users
{
    private static TestApiClient _apiClient = null!;
    private static WebApplicationFactory<Program> _factory = null!;
    private static readonly FakeClock _clock = new(new());
    private static string? _jwt;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        );

        _apiClient = new TestApiClient(_factory.CreateClient());
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await TestApiFactory.ResetDatabase(context);
        _jwt = (await _apiClient.LoginAdminUser()).Token;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();

    [Test]
    public async Task BanUser_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = "testuser.banuser.ok@example.com",
            Password = "Passw0rd",
            Username = "BanUserTestOk"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.ChangeTracker.Clear();

        await _apiClient.Patch($"/users/{user.Id.ToUrlSafeBase64String()}", new()
        {
            Body = new
            {
                Role = UserRole.Banned,
            },
            Jwt = _jwt
        });

        User? res = await context.Users.FindAsync(user.Id);
        res!.Role.Should().Be(UserRole.Banned);
    }

    [Test]
    public async Task BanUser_Unauthorized()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = "testuser.banuser.unauthn@example.com",
            Password = "Passw0rd",
            Username = "BanUserTestUnauthN"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.ChangeTracker.Clear();

        await _apiClient.Awaiting(a => a.Patch(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Body = new
                {
                    Role = UserRole.Banned,
                }
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    [TestCase(UserRole.Banned)]
    public async Task BanUser_Forbidden_NonAdmin(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequestToBan = new()
        {
            Email = $"testuser.banuser.toban.{role}@example.com",
            Password = "Passw0rd",
            Username = $"BanUserTestToBan{role}"
        };

        RegisterRequest registerRequestToBeBanned = new()
        {
            Email = $"testuser.banuser.tobebanned.{role}@example.com",
            Password = "Passw0rd",
            Username = $"BanUserTestToBeBanned{role}"
        };

        CreateUserResult createUserToBanResult = await userService.CreateUser(registerRequestToBan);
        CreateUserResult createUserToBeBannedResult = await userService.CreateUser(registerRequestToBeBanned);
        createUserToBanResult.IsT0.Should().BeTrue();
        createUserToBeBannedResult.IsT0.Should().BeTrue();
        User userToBan = createUserToBanResult.AsT0;
        User userToBeBanned = createUserToBeBannedResult.AsT0;

        LoginResponse res = await _apiClient.LoginUser(
            registerRequestToBan.Email,
            registerRequestToBan.Password
        );

        userToBan.Role = role;
        await context.SaveChangesAsync();

        ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
            $"/users/{userToBeBanned.Id.ToUrlSafeBase64String()}",
            new()
            {
                Body = new
                {
                    Role = UserRole.Banned,
                },
                Jwt = res.Token
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task BanUser_Forbidden_UserToBanIsAdmin()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = "testuser.banuser.unauthz.isadmin@example.com",
            Password = "Passw0rd",
            Username = "BanUserTestUnauthZIsAdmin"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;

        user.Role = UserRole.Administrator;
        await context.SaveChangesAsync();

        ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Body = new
                {
                    Role = UserRole.Banned,
                },
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Banning Admins Forbidden");
    }

    [TestCase(UserRole.Registered)]
    [TestCase(UserRole.Administrator)]
    public async Task BanUser_Forbidden_RoleChangeForbidden(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = $"testuser.banuser.rolechange.{role}@example.com",
            Password = "Passw0rd",
            Username = $"BanUserTestRoleChange{role}"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;

        ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Body = new
                {
                    Role = role,
                },
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Role Change Forbidden");
    }

    [Test]
    public async Task BanUser_NotFound() =>
        await _apiClient.Awaiting(a => a.Patch(
            $"/users/{Guid.NewGuid().ToUrlSafeBase64String()}",
            new()
            {
                Body = new UpdateUserRequest()
                {
                    Role = UserRole.Banned
                },
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task BanUser_UserAlreadyBanned_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = "testuser.banuser.alreadybanned@example.com",
            Password = "Passw0rd",
            Username = "BanUserTestAlreadyBanned"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;

        user.Role = UserRole.Banned;
        await context.SaveChangesAsync();

        await _apiClient.Awaiting(a => a.Patch(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Body = new
                {
                    Role = UserRole.Banned,
                },
                Jwt = _jwt
            }
        )).Should().NotThrowAsync();
    }

    [Test]
    public async Task UnbanUser_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest registerRequest = new()
        {
            Email = "testuser.unbanuser.ok@example.com",
            Password = "Passw0rd",
            Username = "UnbanUserTestOk"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;

        user.Role = UserRole.Banned;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await _apiClient.Patch($"/users/{user.Id.ToUrlSafeBase64String()}", new()
        {
            Body = new
            {
                Role = UserRole.Confirmed,
            },
            Jwt = _jwt
        });

        User? res = await context.Users.FindAsync(user.Id);
        res!.Role.Should().Be(UserRole.Confirmed);
    }
}
