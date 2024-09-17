using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
{
    private static TestApiClient _apiClient = null!;
    private static TestApiFactory _factory = null!;
    private static string? _jwt;

    private readonly Faker<CreateLeaderboardRequest> _createBoardReqFaker =
        new AutoFaker<CreateLeaderboardRequest>().RuleFor(
            x => x.Slug,
            b => string.Join('-', b.Lorem.Words(2))
        );

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestApiFactory();
        _apiClient = _factory.CreateTestApiClient();

        _factory.ResetDatabase();
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
        CreateLeaderboardRequest req = _createBoardReqFaker.Generate();
        LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new() { Body = req, Jwt = _jwt }
        );

        LeaderboardViewModel retrievedLeaderboard = await _apiClient.Get<LeaderboardViewModel>(
            $"/api/leaderboard/{createdLeaderboard?.Id}",
            new()
        );

        createdLeaderboard.Should().NotBeNull();
        (string, string) expectedCreatedBoard = ValueTuple.Create(req.Name, req.Slug);
        (string, string) actualCreatedBoard = ValueTuple.Create(
            createdLeaderboard!.Name,
            createdLeaderboard.Slug
        );
        expectedCreatedBoard.Should().BeEquivalentTo(actualCreatedBoard);

        createdLeaderboard.Should().BeEquivalentTo(retrievedLeaderboard);
    }

    [Test]
    public async Task CreateLeaderboards_GetLeaderboards()
    {
        IEnumerable<Task<LeaderboardViewModel>> boardCreationTasks = _createBoardReqFaker
            .GenerateBetween(3, 10)
            .Select(
                req =>
                    _apiClient.Post<LeaderboardViewModel>(
                        "/leaderboards/create",
                        new() { Body = req, Jwt = _jwt }
                    )
            );
        LeaderboardViewModel[] createdLeaderboards = await Task.WhenAll(boardCreationTasks);

        IEnumerable<long> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
        string leaderboardIdQuery = ListToQueryString(leaderboardIds, "ids");

        List<LeaderboardViewModel> leaderboards = await _apiClient.Get<List<LeaderboardViewModel>>(
            $"api/leaderboards?{leaderboardIdQuery}",
            new()
        );

        leaderboards.Should().BeEquivalentTo(createdLeaderboards);
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

        leaderboard.Should().BeEquivalentTo(createdLeaderboard);
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
            CreatedAt = Instant.FromUnixTimeSeconds(0),
            UpdatedAt = Instant.FromUnixTimeSeconds(0),
            DeletedAt = Instant.FromUnixTimeSeconds(0),
        };

        context.Leaderboards.Add(board);
        await context.SaveChangesAsync();

        Func<Task<LeaderboardViewModel>> act = async () => await _apiClient.Get<LeaderboardViewModel>($"/api/leaderboard?slug={board.Slug}", new());
        await act.Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);
    }

    private static string ListToQueryString<T>(IEnumerable<T> list, string key)
    {
        IEnumerable<string> queryList = list.Select(l => $"{key}={l}");
        return string.Join("&", queryList);
    }
}
