using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions.Specialized;
using LeaderboardBackend.Models;
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
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Categories
{
    private static TestApiClient _apiClient = null!;
    private static WebApplicationFactory<Program> _factory = null!;
    private static readonly FakeClock _clock = new(new Instant());
    private static string? _jwt;
    private static LeaderboardViewModel _createdLeaderboard = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        );
        _apiClient = new TestApiClient(_factory.CreateClient());

        PostgresDatabaseFixture.ResetDatabaseToTemplate();
        _jwt = (await _apiClient.LoginAdminUser()).Token;

        _createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new()
            {
                Body = new CreateLeaderboardRequest()
                {
                    Name = "Super Mario Bros.",
                    Slug = "super_mario_bros",
                },
                Jwt = _jwt
            }
        );
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();

    [Test]
    public async Task GetCategory_NotFound() =>
        await _apiClient.Awaiting(
            a => a.Get<CategoryViewModel>(
                $"/api/cateogries/69",
                new() { Jwt = _jwt }
            )
        ).Should()
        .ThrowAsync<RequestFailureException>()
        .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task CreateCategory_GetCategory_OK()
    {
        CreateCategoryRequest request = new()
        {
            Name = "1 Player",
            Slug = "1_player",
            Info = "only one guy allowed",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        CategoryViewModel createdCategory = await _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt
            }
        );

        createdCategory.CreatedAt.Should().Be(_clock.GetCurrentInstant());

        CategoryViewModel retrievedCategory = await _apiClient.Get<CategoryViewModel>(
            $"/api/category/{createdCategory?.Id}", new() { }
        );

        retrievedCategory.Should().BeEquivalentTo(request);
    }

    [Test]
    public async Task CreateCategory_Unauthenticated()
    {
        CreateCategoryRequest request = new()
        {
            Name = "Unauthenticated",
            Slug = "unauthn",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task CreateCategory_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        string email = $"testuser.updatecat.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"CreateCatTest{role}"
        };

        await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

        CreateCategoryRequest request = new()
        {
            Name = "Bad Role",
            Slug = $"bad-role-{role}",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = res.Token,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateCategory_LeaderboardNotFound()
    {
        CreateCategoryRequest request = new()
        {
            Name = "404",
            Slug = "404",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            "/leaderboard/1000/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Leaderboard Not Found");
    }

    [Test]
    public async Task CreateCategory_NoConflictBecauseOldCatIsDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "First",
            Slug = "should-not-conflict",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);

        CreateCategoryRequest request = new()
        {
            Name = "Shouldn't conflict",
            Slug = "should-not-conflict",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt
            }
        )).Should().NotThrowAsync();
    }

    [Test]
    public async Task CreateCategory_Conflict()
    {
        CreateCategoryRequest request = new()
        {
            Name = "First",
            Slug = "repeated-slug",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        CategoryViewModel created = await _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt
            }
        );

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);

        ConflictDetails<CategoryViewModel>? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ConflictDetails<CategoryViewModel>>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Conflict");
        problemDetails!.Conflicting.Should().BeEquivalentTo(created);
    }

    [TestCase(null, "bad-data", SortDirection.Ascending, RunType.Score, HttpStatusCode.UnprocessableContent)]
    [TestCase("Bad Data", null, SortDirection.Ascending, RunType.Score, HttpStatusCode.UnprocessableContent)]
    [TestCase("Bad Request Invalid SortDirection", "invalid-sort-direction", "Invalid SortDirection", RunType.Score, HttpStatusCode.BadRequest)]
    [TestCase("Bad Request Invalid Type", "invalid-type", SortDirection.Ascending, "Invalid Type", HttpStatusCode.BadRequest)]
    public async Task CreateCategory_BadData(string? name, string? slug, object sortDirection, object runType, HttpStatusCode expectedCode)
    {
        var request = new
        {
            Name = name,
            SortDirection = sortDirection,
            Type = runType,
            Slug = slug,
        };

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Post<CategoryViewModel>(
            $"/leaderboard/{_createdLeaderboard.Id}/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == expectedCode);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("One or more validation errors occurred.");
    }

    [Test]
    public async Task DeleteCategory_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Delete Cat OK",
            Slug = "deletecat-ok",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _apiClient.Delete(
            $"/category/{cat.Id}",
            new()
            {
                Jwt = _jwt
            }
        );
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Category? deleted = await context.FindAsync<Category>(cat.Id);

        deleted.Should().NotBeNull();
        deleted!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task DeleteCategory_Unauthenticated() =>
        await FluentActions.Awaiting(() => _apiClient.Delete(
            "/category/1",
            new() { }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task DeleteCategory_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        string email = $"testuser.deletecat.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"DeleteCatTest{role}"
        };

        await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

        Category cat = new()
        {
            Name = "Bad Role",
            Slug = $"deletecat-bad-role-{role}",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);

        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/category/{cat.Id}",
            new()
            {
                Jwt = res.Token
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteCategory_NotFound()
    {
        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/category/{int.MaxValue}",
            new()
            {
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Not Found");
    }

    [Test]
    public async Task DeleteCategory_NotFound_AlreadyDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Deleted",
            Slug = "deletedcat-already-deleted",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/category/{cat.Id}",
            new()
            {
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Already Deleted");
    }
}
