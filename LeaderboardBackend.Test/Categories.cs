using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
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
    public static async Task GetCategory_NotFound() =>
        await _apiClient.Awaiting(
            a => a.Get<CategoryViewModel>(
                $"/api/cateogries/69",
                new() { Jwt = _jwt }
            )
        ).Should()
        .ThrowAsync<RequestFailureException>()
        .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

    [Test]
    public static async Task CreateCategory_GetCategory_OK()
    {
        Instant now = Instant.FromUnixTimeSeconds(1);
        _clock.Reset(now);

        LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
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

        CreateCategoryRequest request = new()
        {
            Name = "1 Player",
            Slug = "1_player",
            LeaderboardId = createdLeaderboard.Id,
            Info = null,
            SortDirection = SortDirection.Ascending,
            Type = RunType.Time
        };

        CategoryViewModel createdCategory = await _apiClient.Post<CategoryViewModel>(
            "/categories/create",
            new()
            {
                Body = request,
                Jwt = _jwt
            }
        );

        createdCategory.CreatedAt.Should().Be(now);

        CategoryViewModel retrievedCategory = await _apiClient.Get<CategoryViewModel>(
            $"/api/category/{createdCategory?.Id}", new() { }
        );

        retrievedCategory.Should().BeEquivalentTo(request, opts => opts.Excluding(c => c.LeaderboardId));
    }
}
