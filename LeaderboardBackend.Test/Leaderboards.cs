using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
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
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IClock, FakeClock>(_ => _clock);
            });
        });

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

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Conflict);
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
        IUserService userService = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IUserService>();

        RegisterRequest registerRequest = new()
        {
            Email = $"testuser.createlb.{role}@example.com",
            Password = "Passw0rd",
            Username = $"CreateLBTest{role}"
        };

        await userService.CreateUser(registerRequest);

        CreateLeaderboardRequest req = new()
        {
            Name = "Super Mario Bros. 3",
            Slug = "super-mario-bros-3",
            Info = "You don't have permission to create this!"
        };

#pragma warning disable IDE0008
        var res = await FluentActions.Awaiting(() => _apiClient.LoginUser(registerRequest.Email, registerRequest.Password)).Should().NotThrowAsync();
#pragma warning restore IDE0008

        await FluentActions.Awaiting(() => _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = res.Subject.Token }
        )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetLeaderboards_BySlug_OK()
    {
        CreateLeaderboardRequest createReqBody = _createBoardReqFaker.Generate();
        LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = createReqBody, Jwt = _jwt }
        );

        // create random unrelated boards
        foreach (CreateLeaderboardRequest req in _createBoardReqFaker.Generate(2))
        {
            await _apiClient.Post<LeaderboardViewModel>(
                "/leaderboards/create",
                new() { Body = req, Jwt = _jwt }
            );
        }

        LeaderboardViewModel leaderboard = await _apiClient.Get<LeaderboardViewModel>(
            $"api/leaderboard?slug={createReqBody.Slug}",
            new()
        );

        leaderboard.Should().BeEquivalentTo(createReqBody);
    }

    [Test]
    public async Task GetLeaderboards_BySlug_NotFound()
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
    public async Task GetLeaderboards_Deleted_BySlug_NotFound()
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
    public async Task GetLeaderboards()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();
        await context.Leaderboards.ExecuteDeleteAsync();

        Leaderboard[] boards = [
            new()
            {
                Name = "The Legend of Zelda",
                Slug = "legend-of-zelda",
                Info = "The original for the NES"
            },
            new()
            {
                Name = "Zelda II: The Adventure of Link",
                Slug = "adventure-of-link",
                Info = "The daring sequel"
            },
            new()
            {
                Name = "Link: The Faces of Evil",
                Slug = "link-faces-of-evil",
                Info = "Nobody should play this one.",
                DeletedAt = _clock.GetCurrentInstant()
            }
        ];

        context.Leaderboards.AddRange(boards);
        await context.SaveChangesAsync();
        LeaderboardViewModel[] returned = await _apiClient.Get<LeaderboardViewModel[]>("/api/leaderboards", new());
        returned.Should().BeEquivalentTo(boards.Take(2), config => config.Excluding(lb => lb.Categories));
    }

    [Test]
    public async Task RestoreLeaderboard_OK()
    {
        ApplicationContext context = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<ApplicationContext>();

        Leaderboard deletedBoard = new()
        {
            Name = "Super Mario World",
            Slug = "super-mario-world-deleted",
            DeletedAt = _clock.GetCurrentInstant()
        };

        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);

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
        res.UpdatedAt.Should().Be(res.CreatedAt + Duration.FromMinutes(1));
        res.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task RestoreLeaderboard_Unauthenticated()
    {
        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/100/restore", new()
        {
            Jwt = ""
        });

        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RestoreLeaderboard_Unauthorized()
    {
        IUserService userService = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<IUserService>();

        RegisterRequest registerRequest = new()
        {
            Email = "user@example.com",
            Password = "Passw0rd",
            Username = "unauthorized"
        };

        await userService.CreateUser(registerRequest);

        string jwt = (await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password)).Token;

        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/100/restore", new()
        {
            Jwt = jwt,
        });

        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RestoreLeaderboard_NotFound()
    {
        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/100/restore", new()
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
            Name = "Super Mario World",
            Slug = "super-mario-world-non-deleted",
        };

        context.Leaderboards.Add(board);
        await context.SaveChangesAsync();
        board.Id.Should().NotBe(default);

        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Put<LeaderboardViewModel>($"/leaderboard/{board.Id}/restore", new()
        {
            Jwt = _jwt
        });

        await act.Should().ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);
        // TODO: Don't know how to test for the response message.
    }
}
