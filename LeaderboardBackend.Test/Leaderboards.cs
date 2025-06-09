using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions.Specialized;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
public class Leaderboards
{
    private static TestApiClient _apiClient = null!;
    private static WebApplicationFactory<Program> _factory = null!;
    private static readonly FakeClock _clock = new(new());
    private static string? _jwt;

    private readonly Faker<CreateLeaderboardRequest> _createBoardReqFaker =
        new AutoFaker<CreateLeaderboardRequest>().RuleFor(
            x => x.Slug,
            b => string.Join('-', b.Lorem.Words(2))
        );

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
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory.Dispose();

    [Test]
    public async Task GetLeaderboard_NotFound() => await FluentActions.Awaiting(
        async () => await _apiClient.Get<LeaderboardViewModel>(
            $"/api/leaderboard/{long.MaxValue}",
            new()
        )
    ).Should().ThrowAsync<RequestFailureException>()
    .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task CreateLeaderboard_GetLeaderboard_OK()
    {
        CreateLeaderboardRequest req = new()
        {
            Name = "Super Mario 64",
            Slug = "super-mario-64",
            Info = "The iQue is not allowed."
        };

        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);

        LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        );

        createdLeaderboard.CreatedAt.Should().Be(now);

        LeaderboardViewModel retrievedLeaderboard = await _apiClient.Get<LeaderboardViewModel>(
            $"/api/leaderboard/{createdLeaderboard?.Id}",
            new()
        );

        retrievedLeaderboard.Should().BeEquivalentTo(req);
    }

    [Test]
    public async Task CreateLeaderboard_Unauthenticated()
    {
        CreateLeaderboardRequest req = new()
        {
            Name = "Super Mario Sunshine",
            Slug = "Super-mario-sunshine",
            Info = "This leaderboard should not be created."
        };

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateLeaderbaord_SlugInUse()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        context.Leaderboards.Add(new()
        {
            Name = "Super Mario Galaxy",
            Slug = "super-mario-galaxy",
        });

        await context.SaveChangesAsync();

        CreateLeaderboardRequest req = new()
        {
            Name = "Super Mario Galaxy (again)",
            Slug = "super-mario-galaxy",
            Info = "This leaderboard should not be created."
        };

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);

        ConflictDetails<LeaderboardViewModel>? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ConflictDetails<LeaderboardViewModel>>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails!.Title.Should().Be("Conflict");
        problemDetails!.Conflicting!.Slug.Should().Be(req.Slug);
    }

    [Test]
    public async Task CreateLeaderboard_MissingData()
    {
        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
        "/leaderboards/create",
        new()
        {
            Body = new
            {
                Name = "Super Mario Bros. 2"
            },
            Jwt = _jwt
        }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
        "/leaderboards/create",
        new()
        {
            Body = new
            {
                Slug = "super-mario-bros-2"
            },
            Jwt = _jwt
        }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);
    }

    [TestCase("", "super-mario-bros")]
    [TestCase(" ", "super-mario-bros")]
    [TestCase("Super Mario Bros.", "")]
    [TestCase("Super Mario Bros.", "m")]
    [TestCase("Super Mario Bros.", "super mario bros")]
    [TestCase("Super Mario Bros.", "super-mario-bros.")]
    [TestCase("Super Mario Bros.", "1985-nintendo-nes-famicom-fds-gbc-gba-gcn-wiivc-3dsvc-wiiuvc-super-marios-bros-best-game")]
    [TestCase("Super Mario Bros.", "スーパーマリオブラザーズ")]
    public async Task CreateLeaderboard_BadData(string name, string slug)
    {
        CreateLeaderboardRequest req = new()
        {
            Name = name,
            Slug = slug,
            Info = "This leaderboard should not be created."
        };

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task CreateLeaderboard_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        RegisterRequest registerRequest = new()
        {
            Email = $"testuser.createlb.{role}@example.com",
            Password = "Passw0rd",
            Username = $"CreateLBTest{role}"
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        CreateLeaderboardRequest req = new()
        {
            Name = "Super Mario Bros. 3",
            Slug = "super-mario-bros-3",
            Info = "You don't have permission to create this!"
        };

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = res.Token }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetLeaderboard_BySlug_OK()
    {
        CreateLeaderboardRequest req = _createBoardReqFaker.Generate();

        await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        );

        LeaderboardViewModel leaderboard = await _apiClient.Get<LeaderboardViewModel>(
            $"api/leaderboard?slug={req.Slug}",
            new()
        );

        leaderboard.Should().BeEquivalentTo(req);
    }

    [Test]
    public async Task GetLeaderboard_BySlug_NotFound()
    {
        // populate with unrelated boards
        foreach (CreateLeaderboardRequest req in _createBoardReqFaker.Generate(2))
        {
            await _apiClient.Post<LeaderboardViewModel>(
                "/leaderboards/create",
                new() { Body = req, Jwt = _jwt }
            );
        }

        CreateLeaderboardRequest reqForInexistentBoard = _createBoardReqFaker.Generate();
        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Get<LeaderboardViewModel>($"/api/leaderboard?slug={reqForInexistentBoard.Slug}", new());
        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetLeaderboard_Deleted_BySlug_NotFound()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();
        Leaderboard board = new()
        {
            Name = "Should 404",
            Slug = "should-404",
            UpdatedAt = _clock.GetCurrentInstant() - Duration.FromMinutes(1),
            DeletedAt = _clock.GetCurrentInstant() - Duration.FromMinutes(1),
        };

        context.Leaderboards.Add(board);
        await context.SaveChangesAsync();

        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Get<LeaderboardViewModel>($"/api/leaderboard?slug={board.Slug}", new());
        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeletedBoardsDontConsumeSlugs()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard deletedBoard = new()
        {
            Name = "Super Mario World (OLD)",
            Slug = "super-mario-world",
            DeletedAt = _clock.GetCurrentInstant()
        };

        context.Leaderboards.Add(deletedBoard);
        await context.SaveChangesAsync();
        deletedBoard.Id.Should().NotBe(default);

        CreateLeaderboardRequest lbRequest = new()
        {
            Name = "Super Mario World",
            Info = "new and improved",
            Slug = "super-mario-world"
        };

#pragma warning disable IDE0008 // Use explicit type
        var res = await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>("/leaderboards/create", new()
        {
            Body = lbRequest,
            Jwt = _jwt
        })).Should().NotThrowAsync();
#pragma warning restore IDE0008 // Use explicit type

        Leaderboard? created = await context.Leaderboards.FindAsync(res.Subject.Id);
        created.Should().NotBeNull().And.BeEquivalentTo(lbRequest);
        created!.CreatedAt.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task GetLeaderboards_OK()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();
        await context.Leaderboards.ExecuteDeleteAsync();

        Instant now = _clock.GetCurrentInstant();
        _clock.AdvanceSeconds(1);

        Leaderboard[] boards = [
            new()
            {
                Name = "The Legend of Zelda",
                Slug = "legend-of-zelda",
                Info = "The original for the NES",
            },
            new()
            {
                Name = "The Legend of Zelda",
                Slug = "legend-of-zelda-copy",
                Info = "Asserts ID-based tie-break",
            },
            new()
            {
                Name = "Zelda II: The Adventure of Link",
                Slug = "adventure-of-link",
                Info = "The daring sequel",
            },
            new()
            {
                Name = "Link: The Faces of Evil",
                Slug = "link-faces-of-evil",
                Info = "Nobody should play this one.",
                UpdatedAt = now,
                DeletedAt = now
            },
        ];

        context.Leaderboards.AddRange(boards);
        await context.SaveChangesAsync();
        ListView<LeaderboardViewModel> returned = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?limit=9999999", new());
        returned.Data.Should().BeEquivalentTo(boards.Take(3).OrderBy(b => b.Name), config => config.ExcludingMissingMembers());
        returned.Total.Should().Be(3);
        returned.LimitDefault.Should().Be(64);

        ListView<LeaderboardViewModel> returned2 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?status=published&limit=1024", new());
        returned2.Data.Should().BeEquivalentTo(boards.Take(3).OrderBy(b => b.Name), config => config.ExcludingMissingMembers());
        returned2.Total.Should().Be(3);

        ListView<LeaderboardViewModel> returned3 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?status=any&limit=1024", new());
        returned3.Data.Should().BeEquivalentTo(boards.OrderBy(b => b.Name), config => config.ExcludingMissingMembers().WithStrictOrdering());
        returned3.Total.Should().Be(4);

        ListView<LeaderboardViewModel> returned4 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?limit=1", new());
        returned4.Total.Should().Be(3);
        returned4.Data.Single().Should().BeEquivalentTo(boards[0], config => config.ExcludingMissingMembers());

        ListView<LeaderboardViewModel> returned5 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?limit=1&status=any&offset=1", new());
        returned5.Total.Should().Be(4);
        returned5.Data.Single().Should().BeEquivalentTo(boards[0], config => config.ExcludingMissingMembers());

        ListView<LeaderboardViewModel> returned6 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?sortBy=name_desc", new());
        returned6.Total.Should().Be(3);
        returned6.Data.Should().BeEquivalentTo([boards[2], boards[0], boards[1]], config => config.ExcludingMissingMembers().WithStrictOrdering());

        ListView<LeaderboardViewModel> returned7 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards?sortBy=createdAt_desc", new());
        returned7.Total.Should().Be(3);
        returned7.Data.Should().BeEquivalentTo([boards[2], boards[0], boards[1]], config => config.ExcludingMissingMembers().WithStrictOrdering());
    }

    [TestCase(-1, 0)]
    [TestCase(1024, -1)]
    public async Task GetLeaderboards_BadPageData(int limit, int offset) =>
        await FluentActions.Awaiting(
            () =>
                _apiClient.Get<ListView<LeaderboardViewModel>>(
                    $"/api/leaderboards?limit={limit}&offset={offset}",
                    new()
                )
            )
            .Should()
            .ThrowAsync<RequestFailureException>()
            .Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableContent);

    [Test]
    public async Task GetLeaderboards_BadQueryParam()
    {
        ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(
            a => a.Get<LeaderboardViewModel>(
                "/api/leaderboards?sortBy=invalid&status=invalid",
                new()
            )
        ).Should().ThrowAsync<RequestFailureException>().Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableEntity);

        ValidationProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors["status"].Single().Should().Be("The value 'invalid' is not valid.");
        problemDetails!.Errors["sortBy"].Single().Should().Be("The value 'invalid' is not valid.");
    }

    [Test]
    public async Task RestoreLeaderboard_OK()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard deletedBoard = new()
        {
            Name = "Super Mario World",
            Slug = "super-mario-world-to-restore",
            DeletedAt = _clock.GetCurrentInstant()
        };

        context.Leaderboards.Add(deletedBoard);
        await context.SaveChangesAsync();
        deletedBoard.Id.Should().NotBe(default);

        _clock.AdvanceMinutes(1);

        LeaderboardViewModel res = await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/{deletedBoard.Id}/restore", new()
        {
            Jwt = _jwt
        });

        res.Id.Should().Be(deletedBoard.Id);
        res.Slug.Should().Be(deletedBoard.Slug);
        res.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
        res.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task RestoreLeaderboard_Unauthenticated()
    {
        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/100/restore", new());

        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RestoreLeaderboard_Banned_Unauthorized()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        string email = "restore-leaderboard-banned@example.com";
        string password = "P4ssword";

        CreateUserResult createUserResult = await userService.CreateUser(
            new()
            {
                Email = email,
                Password = password,
                Username = "RestoreBoardBanned",
            }
        );
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = UserRole.Banned;

        string jwt = (await _apiClient.LoginUser(email, password)).Token;

        await context.SaveChangesAsync();

        await FluentActions.Awaiting(
            async () => await _apiClient.Put<LeaderboardViewModel>(
                $"/leaderboard/1/restore",
                new()
                {
                    Jwt = jwt,
                }
            )
        ).Should().ThrowAsync<RequestFailureException>()
        .Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [TestCase("restore-leaderboard-unauth1@example.com", "RestoreBoard1", UserRole.Confirmed)]
    [TestCase("restore-leaderboard-unauth2@example.com", "RestoreBoard2", UserRole.Registered)]
    public async Task RestoreLeaderboard_Unauthorized(string email, string username, UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        CreateUserResult createUserResult = await userService.CreateUser(
            new()
            {
                Email = email,
                Password = "P4ssword",
                Username = username,
            }
        );
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        string jwt = (await _apiClient.LoginUser(email, "P4ssword")).Token;

        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/100/restore", new()
        {
            Jwt = jwt,
        });

        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RestoreLeaderboard_NotFound()
    {
        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/{1e10}/restore", new()
        {
            Jwt = _jwt
        });

        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RestoreLeaderboard_NotFound_WasNeverDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard board = new()
        {
            Name = "Hyper Mario World Not Deleted",
            Slug = "hyper-mario-world-non-deleted",
        };

        context.Leaderboards.Add(board);
        await context.SaveChangesAsync();
        board.Id.Should().NotBe(default);

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() =>
            _apiClient.Put<LeaderboardViewModel>(
                $"/leaderboard/{board.Id}/restore",
                new()
                {
                    Jwt = _jwt,
                }
            )
        ).Should().ThrowAsync<RequestFailureException>().Where(ex => ex.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Not Deleted");
    }

    [Test]
    public async Task RestoreLeaderboard_Conflict()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard deleted = new()
        {
            Name = "Conflicted Mario World",
            Slug = "conflicted-mario-world",
            DeletedAt = _clock.GetCurrentInstant()
        };

        Leaderboard reclaimed = new()
        {
            Name = "Reclaimed Mario World",
            Slug = "conflicted-mario-world",
        };

        context.Leaderboards.Add(deleted);
        context.Leaderboards.Add(reclaimed);
        await context.SaveChangesAsync();

        ExceptionAssertions<RequestFailureException> exAssert = await FluentActions.Awaiting(() =>
            _apiClient.Put<LeaderboardViewModel>(
                $"/leaderboard/{deleted.Id}/restore",
                new()
                {
                    Jwt = _jwt,
                }
            )
        ).Should().ThrowAsync<RequestFailureException>().Where(ex => ex.Response.StatusCode == HttpStatusCode.Conflict);

        ConflictDetails<LeaderboardViewModel>? conflictDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ConflictDetails<LeaderboardViewModel>>(TestInitCommonFields.JsonSerializerOptions);
        conflictDetails.Should().NotBeNull();
        LeaderboardViewModel? conflicting = conflictDetails!.Conflicting;
        conflicting.Should().NotBeNull();
        conflicting!.Id.Should().Be(reclaimed.Id);
    }

    [Test]
    public async Task DeleteLeaderboard_Unauthenticated()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "The Witness",
            Slug = "the-witness",
            Info = "Time ends upon achieving enlightenment."
        };

        context.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/leaderboard/{lb.Id}",
            new()
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        Leaderboard? found = await context.Leaderboards.FindAsync(lb.Id);
        found.Should().NotBeNull();
        found!.DeletedAt.Should().BeNull();
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task DeleteLeaderboard_BadRole(UserRole role)
    {
        IUserService userService = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        string email = $"testuser.deletelb.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"DeleteLBTest{role}"
        };

        Leaderboard lb = new()
        {
            Name = "LB Delete Bad Role Test Board",
            Slug = $"lb-delete-bad-role-test-{role}",
        };

        context.Leaderboards.Add(lb);
        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();

        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/leaderboard/{lb.Id}",
            new() { Jwt = res.Token }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        context.ChangeTracker.Clear();
        Leaderboard? found = await context.Leaderboards.FindAsync(lb.Id);
        found.Should().NotBeNull();
        found!.DeletedAt.Should().BeNull();
    }

    [TestCase(long.MaxValue)]
    [TestCase("sansundertale")]
    public async Task DeleteLeaderboard_NotFound(object id) =>
        await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/leaderboard/{id}",
            new() { Jwt = _jwt }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task DeleteLeaderboard_AlreadyDeleted()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();
        Instant now = _clock.GetCurrentInstant();

        Leaderboard lb = new()
        {
            Name = "The Elder Scrolls V: Skyrim",
            Slug = "tesv-skyrim",
            UpdatedAt = now,
            DeletedAt = now
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();

        ExceptionAssertions<RequestFailureException> ex = await FluentActions.Awaiting(() => _apiClient.Delete(
            $"/leaderboard/{lb.Id}",
            new() { Jwt = _jwt }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        ProblemDetails? problemDetails = await ex.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(
            TestInitCommonFields.JsonSerializerOptions
        );

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Already Deleted");
    }

    [Test]
    public async Task DeleteLeaderboard_Success()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Minecraft",
            Slug = "minecraft"
        };

        context.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        _clock.AdvanceMinutes(1);
        HttpResponseMessage res = await _apiClient.Delete($"/leaderboard/{lb.Id}", new() { Jwt = _jwt });
        res.Should().HaveStatusCode(HttpStatusCode.NoContent);
        Leaderboard? found = await context.Leaderboards.FindAsync(lb.Id);
        found.Should().NotBeNull();
        found!.DeletedAt.Should().NotBeNull();
        found!.DeletedAt!.Value.Should().Be(_clock.GetCurrentInstant());
        found!.UpdatedAt.Should().NotBeNull();
        found!.UpdatedAt!.Value.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task UpdateLeaderboard_Unauthenticated()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Celeste",
            Slug = "celest",
        };

        context.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        _clock.AdvanceMinutes(1);

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb.Id}", new()
            {
                Body = new UpdateLeaderboardRequest()
                {
                    Slug = "celeste"
                }
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        Leaderboard? found = await context.Leaderboards.FindAsync(lb.Id);
        found.Should().BeEquivalentTo(lb, config => config.Excluding(l => l.Categories));
    }

    [TestCase(UserRole.Banned)]
    [TestCase(UserRole.Confirmed)]
    [TestCase(UserRole.Registered)]
    public async Task UpdateLeaderboard_BadRole(UserRole role)
    {
        IServiceScope scope = _factory.Services.CreateScope();
        IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

        string email = $"testuser.updatelb.{role}@example.com";

        RegisterRequest registerRequest = new()
        {
            Email = email,
            Password = "Passw0rd",
            Username = $"UpdateLBTest{role}"
        };

        Leaderboard lb = new()
        {
            Name = "LB Update Bad Role Test Board",
            Slug = $"lb-update-bad-role-test-{role}",
        };

        CreateUserResult createUserResult = await userService.CreateUser(registerRequest);
        context.Leaderboards.Add(lb);
        LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
        createUserResult.IsT0.Should().BeTrue();
        User user = createUserResult.AsT0;
        context.Update(user);
        user.Role = role;
        await context.SaveChangesAsync();
        _clock.AdvanceMinutes(1);

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Body = new UpdateLeaderboardRequest()
                {
                    Slug = "amogus"
                },
                Jwt = res.Token
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

        context.ChangeTracker.Clear();
        Leaderboard? found = await context.Leaderboards.FindAsync(lb.Id);
        found.Should().NotBeNull();
        found.Should().BeEquivalentTo(lb, config => config.Excluding(l => l.Categories));
    }

    [TestCase(long.MaxValue)]
    [TestCase("partyrockersinthehousetonight")]
    public async Task UpdateLeaderboard_NotFound(object id) =>
        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{id}",
            new()
            {
                Body = new UpdateLeaderboardRequest()
                {
                    Slug = "fnaf",
                    Info = "Actually, it's \"party rock is in the house tonight.\""
                },
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public async Task UpdateLeaderboard_NoFields()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Hotel Mario",
            Slug = "hotel-mario"
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Body = new UpdateLeaderboardRequest(),
                Jwt = _jwt
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);
    }

    [Test]
    public async Task UpdateLeaderboard_SlugAlreadyUsed()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Prey (2006)",
            Slug = "prey"
        };

        Leaderboard lb2 = new()
        {
            Name = "Prey (2017)",
            Slug = "prey-2017"
        };

        context.Leaderboards.AddRange(lb, lb2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb2.Id}",
            new()
            {
                Jwt = _jwt,
                Body = new UpdateLeaderboardRequest()
                {
                    Name = "Prey",
                    Slug = "prey"
                }
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);

        Leaderboard found = await context.Leaderboards.SingleAsync(l => l.Id == lb2.Id);
        found.Should().BeEquivalentTo(lb2, config => config.Excluding(l => l.Categories));
    }

    [TestCase("Grand Theft Auto Five", "Grand Theft Auto V", "gtav", "")]
    [TestCase("n", "N", "n-2004", "n")]
    [TestCase("Super Mario Brothers", "Super Mario Bros.", "super-mario-bros", "super mario bros")]
    [TestCase("Super Smash Brothers Brawl", "Super Smash Bros. Brawl", "ssbb", "super-smash-bros.-brawl")]
    [TestCase(
        "Dr. Langeskov, The Tiger, and The Terribly Cursed Emerald",
        "Dr. Langeskov, The Tiger, and The Terribly Cursed Emerald: A Whirlwind Heist",
        "dr-langeskov-the-tiger-and-the-terribly-cursed-emerald",
        "dr-langeskov-the-tiger-and-the-terribly-cursed-emerald-a-whirlwind-heist-crows-crows-crows"
    )]
    [TestCase("The Legendary Starfy", "伝説のスタフィー", "densetsu-no-stafy", "デンセツノスタフィー")]
    [TestCase("Resident Evil", "", "resident-evil", null)]
    public async Task UpdateLeaderboard_BadData(string oldName, string? newName, string oldSlug, string? newSlug)
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = oldName,
            Slug = oldSlug
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        UpdateLeaderboardRequest update = new();

        if (newName != null)
        {
            update.Name = newName;
        }

        if (newSlug != null)
        {
            update.Slug = newSlug;
        }

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Jwt = _jwt,
                Body = update,
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);

        Leaderboard found = await context.Leaderboards.SingleAsync(l => l.Id == lb.Id);
        found.Should().BeEquivalentTo(lb, config => config.Excluding(l => l.Categories));
    }

    [Test]
    public async Task UpdateLeaderboard_InvalidFields()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Terraria",
            Slug = "terraria"
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Instant instant = _clock.GetCurrentInstant() - Duration.FromMinutes(1);

        await FluentActions.Awaiting(() => _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Jwt = _jwt,
                Body = new
                {
                    CreatedAt = instant,
                    UpdatedAt = instant,
                    DeletedAt = instant
                }
            }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableContent);

        Leaderboard found = await context.Leaderboards.SingleAsync(l => l.Id == lb.Id);
        found.Should().BeEquivalentTo(lb, config => config.Excluding(l => l.Categories));
    }

    [Test]
    public async Task UpdateLeaderboards_OK()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "The Witcher 3",
            Slug = "witcher-3-wild-hunt"
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        UpdateLeaderboardRequest update = new()
        {
            Name = "The Witcher 3: Wild Hunt",
            Slug = "witcher-3",
            Info = "The best game evar!"
        };

        await _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Jwt = _jwt,
                Body = update
            }
        );

        Leaderboard found = await context.Leaderboards.SingleAsync(l => l.Id == lb.Id);
        found.Should().BeEquivalentTo(update, config => config.ExcludingMissingMembers());
        found.UpdatedAt.Should().NotBeNull();
        found.UpdatedAt!.Value.Should().Be(_clock.GetCurrentInstant());
    }

    [Test]
    public async Task UpdateLeaderboards_OK_Partial()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard lb = new()
        {
            Name = "Pokemon Yellow",
            Slug = "pokemon-yelow",
            Info = "This info is important and shouldn't be deleted."
        };

        context.Leaderboards.Add(lb);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        string newSlug = "pokemon-yellow";

        await _apiClient.Patch(
            $"/leaderboard/{lb.Id}",
            new()
            {
                Jwt = _jwt,
                Body = new UpdateLeaderboardRequest()
                {
                    Slug = newSlug
                }
            }
        );

        Leaderboard updated = await context.Leaderboards.SingleAsync(l => l.Id == lb.Id);
        updated.UpdatedAt.Should().NotBeNull();
        updated.UpdatedAt!.Value.Should().Be(_clock.GetCurrentInstant());
        updated.Name.Should().Be(lb.Name);
        updated.Slug.Should().Be(newSlug);
        updated.Info.Should().Be(lb.Info);
    }

    [Test]
    public async Task SearchLeaderboards_NoQuery() => await FluentActions.Awaiting(
            () => _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search", new())
        ).Should().ThrowAsync<RequestFailureException>().Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableContent);

    [TestCase(-1, 0)]
    [TestCase(1024, -1)]
    public async Task SearchLeaderboards_BadPageData(int limit, int offset) =>
        await FluentActions.Awaiting(() => _apiClient.Get<ListView<LeaderboardViewModel>>($"/api/leaderboards/search?query=big+chungus&limit={limit}&offset={offset}", new()))
            .Should().ThrowAsync<RequestFailureException>().Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableContent);

    [Test]
    public async Task SearchLeaderboards_OK()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard croc = new()
        {
            Name = "Croc: Legend of the Gobbos",
            Info = "Save the Gobbos!",
            Slug = "croc"
        };

        Leaderboard gta = new()
        {
            Name = "Grand Theft Auto IV",
            Info = "Let's go bowling!",
            Slug = "gtaiv"
        };

        context.Leaderboards.AddRange(croc, gta);
        await context.SaveChangesAsync();

        ListView<LeaderboardViewModel> results = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search?q=croc&limit=1024", new());
        results.Data.Should().ContainEquivalentOf(croc, config => config.ExcludingMissingMembers());
        results.Data.Should().NotContainEquivalentOf(gta, config => config.ExcludingMissingMembers());

        ListView<LeaderboardViewModel> results2 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search?q=gobbos&limit=1024", new());
        results2.Data.Should().ContainEquivalentOf(croc, config => config.ExcludingMissingMembers());
        results2.Data.Should().NotContainEquivalentOf(gta, config => config.ExcludingMissingMembers());

        ListView<LeaderboardViewModel> results3 = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search?q=gtaiv&limit=1024", new());
        results3.Data.Should().ContainEquivalentOf(gta, config => config.ExcludingMissingMembers());
        results3.Data.Should().NotContainEquivalentOf(croc, config => config.ExcludingMissingMembers());
    }

    [Test]
    public async Task SearchLeaderboards_Ranked_OK()
    {
        using IServiceScope serviceScope = _factory.Services.CreateScope();
        using ApplicationContext context = serviceScope.ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard okami = new()
        {
            Name = "Okami",
            Info = "Can't believe she's not called Amy",
            Slug = "okami",
        };

        Leaderboard okami2 = new()
        {
            Name = "Okami 2",
            Info = "Why does mom let you have *two* Catwalk Skips",
            Slug = "okami-2",
        };

        Leaderboard momo4 = new()
        {
            Name = "Momodora: Reverie Under the Moonlight",
            Info = "I still want a Moka pot",
            Slug = "momo4",
        };

        context.Leaderboards.AddRange(okami, okami2, momo4);
        await context.SaveChangesAsync();

        ListView<LeaderboardViewModel> results = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search?q=okami&limit=1024", new());
        results.Data.First().Should().BeEquivalentTo(okami, config => config.ExcludingMissingMembers());
        results.Data[1].Should().BeEquivalentTo(okami2, config => config.ExcludingMissingMembers());
        results.Data.Should().NotContain(LeaderboardViewModel.MapFrom(momo4));

        ListView<LeaderboardViewModel> resultsVerifyCount = await _apiClient.Get<ListView<LeaderboardViewModel>>("/api/leaderboards/search?q=okami&limit=1", new());
        resultsVerifyCount.Data.First().Should().BeEquivalentTo(okami, config => config.ExcludingMissingMembers());
        resultsVerifyCount.Data.Should().ContainSingle();
        resultsVerifyCount.Total.Should().Be(2);
    }
}
