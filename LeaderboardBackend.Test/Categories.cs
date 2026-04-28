using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
public class Categories
{
    private HttpClient _client = null!;
    private static WebApplicationFactory<Program> _factory = null!;
    private static readonly FakeClock _clock = new(new Instant());
    private static string? _jwt;
    private static LeaderboardViewModel _leaderboard = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IClock, FakeClock>(_ => _clock)
            )
        );

        _client = _factory.CreateClient();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await TestApiFactory.ResetDatabase(context);

        HttpResponseMessage response = await _client.LoginAdminUser();
        LoginResponse? loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
        _jwt = loginResponse!.Token;

        // TODO: Rely on seed data instead.
        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response2 = await _client.PostAsJsonAsync(
            "/leaderboards",
            new CreateLeaderboardRequest()
            {
                Name = "Super Mario Bros.",
                Slug = "super_mario_bros",
            },
            TestInitCommonFields.JsonSerializerOptions);

        _leaderboard = (await response2.Content.ReadFromJsonAsync<LeaderboardViewModel>(TestInitCommonFields.JsonSerializerOptions))!;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Test]
    public async Task GetCategoryByID_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "get ok",
            Slug = "getcategory-ok",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);

        HttpResponseMessage response = await _client.GetCategory(created.Id);

        response.Should().Be200Ok().And.BeAs(CategoryViewModel.MapFrom(created) with
        {
            CreatedAt = _clock.GetCurrentInstant(),
            UpdatedAt = null,
            DeletedAt = null,
            Status = Status.Published
        });
    }

    [TestCase("NotANumber")]
    [TestCase("69")]
    public async Task GetCategoryByID_NotFound(object id)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/categories/{id}");
        response.Should().Be404NotFound();
    }

    [Test]
    public async Task GetCategoryBySlug_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "get slug ok",
            Slug = "getcategory-slug-ok",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);
        HttpResponseMessage response = await _client.GetCategory(
            _leaderboard.Id, created.Slug);

        response.Should().Be200Ok().And.BeAs(
            CategoryViewModel.MapFrom(created) with
            {
                CreatedAt = _clock.GetCurrentInstant(),
                UpdatedAt = null,
                DeletedAt = null,
                Status = Status.Published
            });
    }

    [Test]
    public async Task GetCategoryBySlug_NotFound_WrongSlug()
    {
        HttpResponseMessage response = await _client.GetCategory(
            _leaderboard.Id, "wrong-slug"
        );

        response.Should().Be404NotFound();
    }

    [Test]
    public async Task GetCategoryBySlug_NotFound_WrongLeaderboardID()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "get slug not found",
            Slug = "getcategory-slug-wrong-board-id",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);

        HttpResponseMessage response = await _client.GetCategory(short.MaxValue, created.Slug);
        response.Should().Be404NotFound();
    }

    [Test]
    public async Task GetCategoryBySlug_NotFound_IsDeleted()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category created = new()
        {
            Name = "get slug not found",
            Slug = "getcategory-slug-deleted",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);

        HttpResponseMessage response = await _client.GetCategory(
            _leaderboard.Id, created.Slug);

        response.Should().Be404NotFound();
    }

    [Test]
    public async Task GetCategoriesForLeaderboard_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard board = new()
        {
            Name = "get cats ok",
            Slug = "getcategories-ok",
            Categories = [
                new()
                {
                    Name = "get cats ok",
                    Slug = "getcategories-ok",
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Score,
                },
                new()
                {
                    Name = "get cats ok RTA",
                    Slug = "getcategories-ok-rta",
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Time
                },
                new()
                {
                    Name = "get cats ok deleted",
                    Slug = "getcategories-ok-deleted",
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Score,
                    DeletedAt = _clock.GetCurrentInstant(),
                },
            ],
        };

        context.Add(board);
        await context.SaveChangesAsync();
        board.Id.Should().NotBe(default);
        HttpResponseMessage response = await _client.GetCategoriesForLeaderboard(board.Id, 99999999);

        response.Should().Be200Ok().And.Satisfy<ListView<CategoryViewModel>>(model =>
        {
            model.Data.Should().BeEquivalentTo(board.Categories.Take(2), opts => opts.ExcludingMissingMembers());
            model.Total.Should().Be(2);
            model.LimitDefault.Should().Be(64);
        });

        HttpResponseMessage response2 = await _client.GetCategoriesForLeaderboard(board.Id, null, null, StatusFilter.Any);

        response2.Should().Be200Ok().And.Satisfy<ListView<CategoryViewModel>>(model =>
        {
            model.Data.Should().BeEquivalentTo(board.Categories, options => options.ExcludingMissingMembers());
            model.Total.Should().Be(3);
        });

        board.Categories.ElementAt(0).DeletedAt = _clock.GetCurrentInstant();
        board.Categories.ElementAt(1).DeletedAt = _clock.GetCurrentInstant();
        await context.SaveChangesAsync();

        HttpResponseMessage response3 = await _client.GetCategoriesForLeaderboard(board.Id);
        response3.Should().Be200Ok().And.Satisfy<ListView<CategoryViewModel>>(model => model.Data.Should().BeEmpty());
    }

    [TestCase(-1, 0)]
    [TestCase(1024, -1)]
    public async Task GetCategoriesForLeaderboard_BadPageData(int limit, int offset)
    {
        HttpResponseMessage response = await _client.GetCategoriesForLeaderboard(54, limit, offset);
        response.Should().Be422UnprocessableEntity();
    }

    [Test]
    public async Task GetCategoriesForLeaderboard_NotFound()
    {
        HttpResponseMessage response = await _client.GetCategoriesForLeaderboard(
            short.MaxValue);

        response.Should().Be404NotFound();
    }

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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.CreateCategory(_leaderboard.Id, request);

        long id = default;
        response.Should().Be201Created().And.Satisfy<CategoryViewModel>(model =>
        {
            id = model.Id;
            model.CreatedAt.Should().Be(_clock.GetCurrentInstant());
        });

        HttpResponseMessage response2 = await _client.GetCategory(id);
        response2.Should().Be200Ok().And.Satisfy<CategoryViewModel>(model => model.Should().BeEquivalentTo(request));
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

        HttpResponseMessage response = await _client.CreateCategory(_leaderboard.Id, request);
        response.Should().Be401Unauthorized();
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task CreateCategory_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        string email = $"testuser.createcat.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"CreateCatTest{role}"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        createUserResult.IsT0.Should().BeTrue();

        HttpResponseMessage response = await _client.LoginUser(email, registerRequest.Password);
        LoginResponse res = (await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions))!;
        _client.DefaultRequestHeaders.Authorization = new("Bearer", res.Token);

        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        CreateCategoryRequest request = new()
        {
            Name = "Bad Role",
            Slug = $"bad-role-{role}",
            Info = "",
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        HttpResponseMessage response2 = await _client.CreateCategory(_leaderboard.Id, request);
        response2.Should().Be403Forbidden();
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.CreateCategory(1000, request);

        response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(
            details => details.Title.Should().Be("Leaderboard Not Found"));
    }

    [Test]
    public async Task CreateCategory_NoConflictBecauseOldCatIsDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "First",
            Slug = "should-not-conflict",
            LeaderboardId = _leaderboard.Id,
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.CreateCategory(_leaderboard.Id, request);
        response.Should().Be201Created();
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.CreateCategory(_leaderboard.Id, request);

        CategoryViewModel created = (await response.Content.ReadFromJsonAsync<CategoryViewModel>(
            TestInitCommonFields.JsonSerializerOptions))!;

        HttpResponseMessage response2 = await _client.CreateCategory(_leaderboard.Id, request);
        response2.Should().Be409Conflict().And.Satisfy<ConflictDetails<CategoryViewModel>>(details =>
        {
            details.Title.Should().Be("Conflict");
            details.Conflicting.Should().BeEquivalentTo(created);
        });
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/leaderboards/{_leaderboard.Id}/categories",
            request,
            TestInitCommonFields.JsonSerializerOptions);

        response.Should().HaveHttpStatusCode(expectedCode).And.Satisfy<ProblemDetails>(
            details => details.Title.Should().Be("One or more validation errors occurred."));
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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(created);
        await context.SaveChangesAsync();
        created.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(created.Id, new()
        {
            Name = "new update",
            Slug = "new-update",
            Info = "new info",
            SortDirection = SortDirection.Descending,
        });

        response.Should().Be204NoContent();

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time,
        };

        context.Add(cat);
        context.SaveChanges();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _client.UpdateCategory(cat.Id, new()
        {
            Name = "should not work"
        });

        response.Should().Be401Unauthorized();
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
            LeaderboardId = _leaderboard.Id,
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

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        HttpResponseMessage response = await _client.LoginUser(registerRequest.Email, registerRequest.Password);

        LoginResponse res = (await response.Content.ReadFromJsonAsync<LoginResponse>(
            TestInitCommonFields.JsonSerializerOptions))!;

        _client.DefaultRequestHeaders.Authorization = new("Bearer", res.Token);

        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        HttpResponseMessage response2 = await _client.UpdateCategory(cat.Id, new()
        {
            Name = "should not work",
        });

        response2.Should().Be403Forbidden();

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.Name.Should().Be("Update Cat UnauthZ");
    }

    [Test]
    public async Task UpdateCategory_CategoryNotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(int.MaxValue, new()
        {
            Name = "should not work"
        });

        response.Should().Be404NotFound();
    }

    [Test]
    public async Task UpdateCategory_Conflict()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category first = new()
        {
            Name = "Update First",
            Slug = "updatecat-first",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        Category toConflict = new()
        {
            Name = "To conflict",
            Slug = "updatecat-to-conflict",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.AddRange(first, toConflict);
        await context.SaveChangesAsync();
        first.Id.Should().NotBe(default);
        toConflict.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(toConflict.Id, new()
        {
            Slug = "updatecat-first"
        });

        response.Should().Be409Conflict().And.Satisfy<ConflictDetails<CategoryViewModel>>(details =>
        {
            details.Title.Should().Be("Conflict");
            details.Conflicting.Id.Should().Be(first.Id);
        });

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        Category toNotConflict = new()
        {
            Name = "Update Should Not Conflict Deleted",
            Slug = "updatecat-no-conflict-deleted",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.AddRange(deleted, toNotConflict);
        await context.SaveChangesAsync();
        deleted.Id.Should().NotBe(default);
        toNotConflict.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(toNotConflict.Id, new()
        {
            Slug = "updatecat-deleted"
        });

        response.Should().Be204NoContent();

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
            LeaderboardId = _leaderboard.Id,
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(toNotConflict.Id, new()
        {
            Slug = first.Slug
        });

        response.Should().Be204NoContent();

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
            LeaderboardId = _leaderboard.Id,
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.UpdateCategory(cat.Id, updateRequest);
        response.Should().Be422UnprocessableEntity().And.Satisfy<ValidationProblemDetails>(details =>
        {
            if (slug is not null)
            {
                details.Errors["Slug"].Should().Equal([SlugRule.SLUG_FORMAT]);
            }
            else
            {
                details.Errors[""].Should().Equal(["PredicateValidator"]);
            }
        });

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"categories/{cat.Id}",
            new
            {
                Type = RunType.Time
            },
            TestInitCommonFields.JsonSerializerOptions);

        response.Should().Be422UnprocessableEntity();
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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.DeleteCategory(cat.Id);
        response.Should().Be204NoContent();

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _client.DeleteCategory(cat.Id);
        response.Should().Be401Unauthorized();

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

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);

        HttpResponseMessage response = await _client.LoginUser(registerRequest.Email, registerRequest.Password);
        LoginResponse res = (await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions))!;
        _client.DefaultRequestHeaders.Authorization = new("Bearer", res.Token);

        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;

        Category cat = new()
        {
            Name = "Bad Role",
            Slug = $"deletecat-bad-role-{role}",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time,
        };

        context.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response2 = await _client.DeleteCategory(cat.Id);
        response2.Should().Be403Forbidden();

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task DeleteCategory_NotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.DeleteCategory(int.MaxValue);

        response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(
            details => details.Title.Should().Be("Not Found"));
    }

    [Test]
    public async Task DeleteCategory_NotFound_AlreadyDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Deleted",
            Slug = "deletedcat-already-deleted",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);
        HttpResponseMessage response = await _client.DeleteCategory(cat.Id);

        response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(
            details => details.Title.Should().Be("Already Deleted"));

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        await _client.UpdateCategory(cat.Id, new()
        {
            Status = Status.Published
        });

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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        HttpResponseMessage response = await _client.UpdateCategory(cat.Id, new()
        {
            Status = Status.Published
        });

        response.Should().Be401Unauthorized();

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
            LeaderboardId = _leaderboard.Id,
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

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        HttpResponseMessage response = await _client.LoginUser(registerRequest.Email, registerRequest.Password);
        LoginResponse res = (await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions))!;
        _client.DefaultRequestHeaders.Authorization = new("Bearer", res.Token);

        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        HttpResponseMessage response2 = await _client.UpdateCategory(cat.Id, new()
        {
            Status = Status.Published
        });

        response2.Should().Be403Forbidden();

        Category? retrieved = await context.FindAsync<Category>(cat.Id);
        retrieved!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task RestoreCategory_NotFound()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(int.MaxValue, new()
        {
            Status = Status.Published
        });

        response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(
            details => details.Title.Should().Be("Not Found"));
    }

    [Test]
    public async Task RestoreCategory_WasNeverDeleted_OK()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Category cat = new()
        {
            Name = "Restore Cat Never Deleted",
            Slug = "restorecat-never-deleted",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Categories.Add(cat);
        await context.SaveChangesAsync();
        cat.Id.Should().NotBe(default);

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(cat.Id, new()
        {
            Status = Status.Published
        });

        response.Should().Be204NoContent();
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
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
            DeletedAt = _clock.GetCurrentInstant(),
        };

        Category conflicting = new()
        {
            Name = "Restore Cat Conflicting",
            Slug = "restorecat-to-conflict",
            LeaderboardId = _leaderboard.Id,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Score,
        };

        context.Categories.AddRange(deleted, conflicting);
        await context.SaveChangesAsync();
        deleted.Id.Should().NotBe(default);
        conflicting.Id.Should().NotBe(default);
        context.ChangeTracker.Clear();

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        HttpResponseMessage response = await _client.UpdateCategory(deleted.Id, new()
        {
            Status = Status.Published
        });

        response.Should().Be409Conflict().And.Satisfy<ConflictDetails<CategoryViewModel>>(detail =>
        {
            detail.Title.Should().Be("Conflict");
            detail.Conflicting.Should().BeEquivalentTo(CategoryViewModel.MapFrom(conflicting));
        });

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
            LeaderboardId = _leaderboard.Id,
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
        deleted.Id.Should().NotBe(default).And.NotBe(_leaderboard.Id);
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

        _client.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

        await _client.UpdateCategory(notConflicting.Id, new()
        {
            Status = Status.Published
        });

        Category? verify = await context.FindAsync<Category>(notConflicting.Id);
        verify!.DeletedAt.Should().BeNull();
    }
}
