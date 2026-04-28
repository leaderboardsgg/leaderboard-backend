using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Test;

[TestFixture]
public class Users
{
    private static HttpClient _apiClient = null!;
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

        _apiClient = _factory.CreateClient();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await TestApiFactory.ResetDatabase(context);
        HttpResponseMessage responseMessage = await _apiClient.LoginAdminUser();
        LoginResponse? loginResponse = await responseMessage.Content.ReadFromJsonAsync<LoginResponse?>(TestInitCommonFields.JsonSerializerOptions);
        _jwt = loginResponse!.Token;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _apiClient.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        _apiClient.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GetUsers_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await context.Users.Where(u => u.Id != TestInitCommonFields.Admin.Id).ExecuteDeleteAsync();

        // Breaking standard pattern here because doing otherwise would be
        // unnecessarily tedious - zysim
        context.Users.AddRange([
            new()
            {
                Email = "getusers.ok@example.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = "GetUsersOk",
            },
            new()
            {
                Email = "getusers.ok1@example.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = "GetUsersOk1",
            },
            new()
            {
                Email = "getusers.ok2@example.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = "GetUsersOk2",
                Role = UserRole.Banned,
            },
            new()
            {
                Email = "getusers.ok3@example.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = "GetUsersOk3",
            },
            new()
            {
                Email = "getusers.ok4@example.com",
                Password = BCryptNet.EnhancedHashPassword("P4ssword"),
                Username = "GetUsersOk4",
            },
        ]);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        IEnumerable<UserViewModel> users = context.Users.Select(UserViewModel.MapFrom).OrderBy(u => u.Username);

        IEnumerable<UserViewModel> expected = users.Where(u => u.Role != UserRole.Banned);

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _apiClient.GetAsync("/users?role=Administrator,Registered");

        response.Should().Be200Ok().And.Satisfy<ListView<UserViewModel>>(listView =>
        {
            listView.Total.Should().Be(5);
            listView.LimitDefault.Should().Be(64);
            listView.Data.Should().BeEquivalentTo(expected, config => config.WithStrictOrdering());
        });

        IEnumerable<UserViewModel> expected1 = users.Where(u => u.Role == UserRole.Banned);
        HttpResponseMessage response2 = await _apiClient.GetAsync("/users?role=banned");

        response2.Should().Be200Ok().And.Satisfy<ListView<UserViewModel>>(listView =>
        {
            listView.Total.Should().Be(1);
            listView.Data.Should().BeEquivalentTo(expected1);
        });

        HttpResponseMessage response3 = await _apiClient.GetAsync("/users?role=banned,registered,confirmed,administrator");

        response3.Should().Be200Ok().And.Satisfy((ListView<UserViewModel> listView) =>
        {
            listView.Total.Should().Be(6);
            listView.Data.Should().BeEquivalentTo(users, config => config.WithStrictOrdering());
        });

        IEnumerable<UserViewModel> expected3 = users.Where(u => u.Role != UserRole.Banned).TakeLast(2);
        HttpResponseMessage response4 = await _apiClient.GetAsync("/users?limit=2&offset=3&role=Registered,Administrator");

        response4.Should().Be200Ok().And.Satisfy((ListView<UserViewModel> listView) =>
        {
            listView.Total.Should().Be(5);
            listView.Data.Should().BeEquivalentTo(expected3, config => config.WithStrictOrdering());
        });
    }

    [Test]
    public async Task GetUsers_Unauthorized()
    {
        HttpResponseMessage response = await _apiClient.GetAsync("/users");
        response.Should().Be401Unauthorized();
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task GetUsers_Forbidden(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        RegisterRequest request = new()
        {
            Email = $"getusers.forbidden.{role}@example.com",
            Password = "P4ssword",
            Username = $"GetUsersForbidden{role}",
        };

        CreateUserResult createUserResult = await userService.CreateUser(request);
        User user = createUserResult.AsT0;

        LoginResult loginResult = await userService.LoginByEmailAndPassword(request.Email, request.Password);

        user.Role = role;
        await context.SaveChangesAsync();

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", loginResult.AsT0);
        HttpResponseMessage response = await _apiClient.GetAsync("/users");
        response.Should().Be403Forbidden();
    }

    [TestCase("role=invalid&limit=10")]
    [TestCase("offset=-1")]
    [TestCase("limit=-1")]
    public async Task GetUsers_UnprocessableEntity(string query)
    {
        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _apiClient.GetAsync($"/users?{query}");
        response.Should().Be422UnprocessableEntity();
    }

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

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned,
            },
            TestInitCommonFields.JsonSerializerOptions
        );

        User? res = await context.Users.FindAsync(user.Id);
        res.Should().NotBeNull();
        res.Role.Should().Be(UserRole.Banned);
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
            Password = BCryptNet.EnhancedHashPassword("Passw0rd"),
            Username = "BanUserTestUnauthN"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned
            },
            TestInitCommonFields.JsonSerializerOptions
        );
        response.Should().Be401Unauthorized();
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

        HttpResponseMessage loginResponse = await _apiClient.LoginUser(
            registerRequestToBan.Email,
            registerRequestToBan.Password);

        LoginResponse? res = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", res!.Token);

        userToBan.Role = role;
        await context.SaveChangesAsync();

        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{userToBeBanned.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned
            },
            TestInitCommonFields.JsonSerializerOptions
        );
        response.Should().Be403Forbidden();
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

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned
            },
            TestInitCommonFields.JsonSerializerOptions
        );

        response.Should().Be403Forbidden().And.Satisfy((ProblemDetails problemDetails) =>
            problemDetails.Title.Should().Be("Banning Admins Forbidden"));
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

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = role
            },
            TestInitCommonFields.JsonSerializerOptions
        );

        response.Should().Be403Forbidden().And.Satisfy((ProblemDetails problemDetails) =>
            problemDetails.Title.Should().Be("Role Change Forbidden"));
    }

    [Test]
    public async Task BanUser_NotFound()
    {
        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{Guid.NewGuid().ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned
            },
            TestInitCommonFields.JsonSerializerOptions
        );
        response.Should().Be404NotFound();
    }

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

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Banned,
            },
            TestInitCommonFields.JsonSerializerOptions);

        response.Should().BeSuccessful();
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

        _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _apiClient.PatchAsJsonAsync<UpdateUserRequest>(
            $"/users/{user.Id.ToUrlSafeBase64String()}",
            new()
            {
                Role = UserRole.Confirmed,
            },
            TestInitCommonFields.JsonSerializerOptions
        );

        response.Should().BeSuccessful();

        User? res = await context.Users.FindAsync(user.Id);
        res.Should().NotBeNull();
        res.Role.Should().Be(UserRole.Confirmed);
    }
}
