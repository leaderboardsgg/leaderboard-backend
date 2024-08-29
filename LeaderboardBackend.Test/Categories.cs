using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Categories
{
    private static TestApiClient _apiClient = null!;
    private static TestApiFactory _factory = null!;
    private static string? _jwt;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new TestApiFactory();
        _apiClient = _factory.CreateTestApiClient();

        _factory.ResetDatabase();
        _jwt = (await _apiClient.LoginAdminUser()).Token;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public static void GetCategory_NotFound()
    {
        RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(
            async () =>
                await _apiClient.Get<CategoryViewModel>(
                    $"/api/categories/69",
                    new() { Jwt = _jwt }
                )
        )!;

        Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
    }

    [Test]
    public static async Task CreateCategory_GetCategory_OK()
    {
        LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
            "/leaderboards/create",
            new()
            {
                Body = new CreateLeaderboardRequest()
                {
                    Name = "Super Mario Bros.",
                    Slug = "super_mario_bros",
                    Info = null
                },
                Jwt = _jwt
            }
        );

        CategoryViewModel createdCategory = await _apiClient.Post<CategoryViewModel>(
            "/categories/create",
            new()
            {
                Body = new CreateCategoryRequest()
                {
                    Name = "1 Player",
                    Slug = "1_player",
                    LeaderboardId = createdLeaderboard.Id,
                    Info = null,
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Time
                },
                Jwt = _jwt
            }
        );

        CategoryViewModel retrievedCategory = await _apiClient.Get<CategoryViewModel>(
            $"/api/category/{createdCategory?.Id}", new() { }
        );

        Assert.AreEqual(createdCategory, retrievedCategory);
    }
}
