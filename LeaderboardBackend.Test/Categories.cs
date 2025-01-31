using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions.Specialized;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
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
    public async Task GetCategory_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "get ok",
            Slug = "getcategory-ok",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);

        await _apiClient.Awaiting(
            a => a.Get<CategoryViewModel>(
                $"api/category/{created.Id}",
                new() { }
            )
        ).Should()
        .NotThrowAsync()
        .WithResult(new()
        {
            Id = created.Id,
            Name = "get ok",
            Slug = "getcategory-ok",
            Info = "",
            Type = RunType.Score,
            SortDirection = SortDirection.Ascending,
            LeaderboardId = _createdLeaderboard.Id,
            CreatedAt = _clock.GetCurrentInstant(),
            UpdatedAt = null,
            DeletedAt = null,
        });
    }

    [TestCase("NotANumber")]
    [TestCase("69")]
    public async Task GetCategory_NotFound(object id) =>
        await _apiClient.Awaiting(
            a => a.Get<CategoryViewModel>(
                $"/api/category/{id}",
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
    public async Task UpdateCategory_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "update ok",
            Slug = "update-ok",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _apiClient.Patch(
            $"category/{created.Id}",
            new()
            {
                Body = new UpdateCategoryRequest()
                {
                    Name = "new update",
                    Slug = "new-update",
                    Info = "new info",
                    SortDirection = SortDirection.Descending,
                },
                Jwt = _jwt,
            }
        );
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Category? retrieved = await context.FindAsync<Category>(created.Id);
        retrieved!.Name.Should().Be("new update");
        retrieved!.Slug.Should().Be("new-update");
        retrieved!.Info.Should().Be("new info");
        retrieved!.SortDirection.Should().Be(SortDirection.Descending);
        retrieved!.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task UpdateCategory_Unauthenticated()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Update Cat UnauthN",
            Slug = "updatecat-unauth",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time,
        };

        context.Add(cat);
        context.SaveChanges();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{cat.Id}",
            new()
            {
                Body = new UpdateCategoryRequest
                {
                    Name = "should not work"
                }
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.Name.Should().Be("Update Cat UnauthN");
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task UpdateCategory_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Update Cat UnauthZ",
            Slug = $"updatecat-unauthz-{role}",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        string email = $"testuser.updatecat.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"UpdateCatTest{role}"
        };

        await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{cat.Id}",
            new()
            {
                Body = new UpdateCategoryRequest
                {
                    Name = "should not work",
                },
                Jwt = res.Token,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.Name.Should().Be("Update Cat UnauthZ");
    }

    [Test]
    public async Task UpdateCategory_CategoryNotFound() =>
        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{int.MaxValue}",
            new()
            {
                Body = new UpdateCategoryRequest
                {
                    Name = "should not work",
                },
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task UpdateCategory_Conflict()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category first = new()
        {
            Name = "Update First",
            Slug = "updatecat-first",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        Category toConflict = new()
        {
            Name = "To conflict",
            Slug = "updatecat-to-conflict",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.AddRange(first, toConflict);
        await context.SaveChangesAsync();
        first.Id.Should().NotBe(default);
        toConflict.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{toConflict.Id}",
            new()
            {
                Body = new UpdateCategoryRequest()
                {
                    Slug = "updatecat-first",
                },
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);

        ConflictDetails<CategoryViewModel>? conflictDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ConflictDetails<CategoryViewModel>>(TestInitCommonFields.JsonSerializerOptions);
        conflictDetails!.Title.Should().Be("Conflict");
        conflictDetails!.Conflicting!.Id.Should().Be(first.Id);

        Category? toConflictRetrieved = await context.FindAsync<Category>(toConflict.Id);
        toConflictRetrieved!.Slug.Should().Be("updatecat-to-conflict");
    }

    [Test]
    public async Task UpdateCategory_NoConflictBecauseOldCatIsDeleted()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category deleted = new()
        {
            Name = "Update Deleted",
            Slug = "updatecat-deleted",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        Category toNotConflict = new()
        {
            Name = "Update Should Not Conflict Deleted",
            Slug = "updatecat-no-conflict-deleted",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.AddRange(deleted, toNotConflict);
        await context.SaveChangesAsync();
        deleted.Id.Should().NotBe(default);
        toNotConflict.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{toNotConflict.Id}",
            new()
            {
                Body = new UpdateCategoryRequest()
                {
                    Slug = "updatecat-deleted"
                },
                Jwt = _jwt
            }
        )).Should().NotThrowAsync();

        Category? toNotConflictRetrieved = await context.FindAsync<Category>(toNotConflict.Id);
        toNotConflictRetrieved!.Slug.Should().Be("updatecat-deleted");
    }

    [Test]
    public async Task UpdateCategory_NoConflictBecauseDifferentLeaderboard()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category first = new()
        {
            Name = "Update No Conflict",
            Slug = "updatecat-no-conflict-different-board",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        Leaderboard board = new()
        {
            Name = "Update Cat Different Board",
            Slug = "updatecat-no-conflict-different-board",
        };

        context.AddRange(first, board);
        await context.SaveChangesAsync();
        first.Id.Should().NotBe(default);
        board.Id.Should().NotBe(default);

        Category toNotConflict = new()
        {
            Name = "Should Not Conflict",
            Slug = "updatecat-should-not-conflict-different-board",
            LeaderboardId = board.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };
        context.Add(toNotConflict);
        await context.SaveChangesAsync();
        toNotConflict.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{toNotConflict.Id}",
            new()
            {
                Body = new UpdateCategoryRequest()
                {
                    Slug = first.Slug,
                },
                Jwt = _jwt,
            }
        )).Should().NotThrowAsync();

        Category? toNotConflictRetrieved = await context.FindAsync<Category>(toNotConflict.Id);
        toNotConflictRetrieved!.Slug.Should().Be(first.Slug);
    }

    [TestCase(1, "b.b")]
    [TestCase(2, "b")]
    [TestCase(3, null)]
    public async Task UpdateCategory_BadData(int index, string? slug)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Update Bad Data",
            Slug = $"updatecat-bad-data-{index}",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        UpdateCategoryRequest updateRequest = new() { };

        if (slug is not null)
        {
            updateRequest.Slug = slug;
        }

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{cat.Id}",
            new()
            {
                Body = updateRequest,
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableEntity);

        ValidationProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();

        if (slug is not null)
        {
            problemDetails!.Errors["Slug"].Should().Equal([SlugRule.SLUG_FORMAT]);
        }
        else
        {
            problemDetails!.Errors[""].Should().Equal(["PredicateValidator"]);
        }

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.Slug.Should().Be(cat.Slug);
    }

    [Test]
    public async Task UpdateCategory_FieldNotAllowed()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Update Field Not Allowed",
            Slug = $"updatecat-field-not-allowed",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"category/{cat.Id}",
            new()
            {
                Body = new
                {
                    Type = RunType.Time,
                },
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableEntity);
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
        deleted!.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
        deleted!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task DeleteCategory_Unauthenticated()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Delete Cat UnauthN",
            Slug = "deletecat-unauthn",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"category/{cat.Id}",
            new() { }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.DeletedAt.Should().BeNull();
    }

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
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/category/{cat.Id}",
            new()
            {
                Jwt = res.Token
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.DeletedAt.Should().BeNull();
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
        context.ChangeTracker.Clear();

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/category/{cat.Id}",
            new()
            {
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Already Deleted");

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.UpdatedAt.Should().BeNull();
    }

    [Test]
    public async Task RestoreCategory_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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
        context.ChangeTracker.Clear();

        CategoryViewModel restored = await _apiClient.Put<CategoryViewModel>(
            $"category/{cat.Id}/restore",
            new()
            {
                Jwt = _jwt
            }
        );

        restored.DeletedAt.Should().BeNull();

        Category? verify = await context.FindAsync<Category>(cat.Id);
        verify!.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task RestoreCategory_Unauthenticated()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Restore Cat UnauthN",
            Slug = "restorecat-unauthn",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Put<CategoryViewModel>(
            $"category/{cat.Id}/restore",
            new() { }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        Category? verify = await context.FindAsync<Category>(cat.Id);
        verify!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task RestoreCategory_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Restore Cat UnauthZ",
            Slug = $"restorecat-unauthz-{role}",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        string email = $"testuser.restorecat.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"RestoreCatTest{role}"
        };

        await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

        await FluentActions.Awaiting(() => _apiClient.Put<CategoryViewModel>(
            $"category/{cat.Id}/restore",
            new()
            {
                Jwt = res.Token
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task RestoreCategory_NotFound()
    {
        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Put<CategoryViewModel>(
            $"category/{int.MaxValue}/restore",
            new()
            {
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Not Found");
    }

    [Test]
    public async Task RestoreCategory_NotFound_WasNeverDeleted()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Restore Cat Not Found Never Deleted",
            Slug = "restorecat-notfound-never-deleted",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Put<CategoryViewModel>(
            $"category/{cat.Id}/restore",
            new()
            {
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Not Deleted");
    }

    [Test]
    public async Task RestoreCategory_Conflict()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category deleted = new()
        {
            Name = "Restore Cat To Conflict",
            Slug = "restorecat-to-conflict",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        Category conflicting = new()
        {
            Name = "Restore Cat Conflicting",
            Slug = "restorecat-to-conflict",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Categories.AddRange(deleted, conflicting);
        await context.SaveChangesAsync();
        deleted.Id.Should().NotBe(default);
        conflicting.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Put<CategoryViewModel>(
            $"category/{deleted.Id}/restore",
            new()
            {
                Jwt = _jwt,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);

        ConflictDetails<CategoryViewModel>? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ConflictDetails<CategoryViewModel>>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Conflict");
        problemDetails!.Conflicting.Should().BeEquivalentTo(CategoryViewModel.MapFrom(conflicting));

        Category? verify = await context.FindAsync<Category>(deleted.Id);
        verify!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
        verify!.UpdatedAt.Should().BeNull();
    }

    [Test]
    public async Task RestoreCategory_NoConflict_DifferentBoard()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category deleted = new()
        {
            Name = "Restore Cat Should Not Conflict",
            Slug = "restorecat-should-not-conflict",
            LeaderboardId = _createdLeaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        Leaderboard board = new()
        {
            Name = "Restore Cat Board",
            Slug = "restorecat-board",
        };

        context.AddRange(deleted, board);
        await context.SaveChangesAsync();
        deleted.Id.Should().NotBe(default).And.NotBe(_createdLeaderboard.Id);
        board.Id.Should().NotBe(default);

        Category notConflicting = new()
        {
            Name = "Restore Cat Conflicting",
            Slug = deleted.Slug,
            LeaderboardId = board.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Add(notConflicting);
        await context.SaveChangesAsync();
        notConflicting.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        CategoryViewModel restored = await _apiClient.Put<CategoryViewModel>(
            $"category/{notConflicting.Id}/restore",
            new()
            {
                Jwt = _jwt
            }
        );

        restored.DeletedAt.Should().BeNull();

        Category? verify = await context.FindAsync<Category>(notConflicting.Id);
        verify!.DeletedAt.Should().BeNull();
    }
}
