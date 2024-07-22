using System;
using System.Threading.Tasks;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NodaTime;
using NUnit.Framework;

namespace LeaderboardBackend.Test
{
    [TestFixture]
    internal class Runs
    {
        private static TestApiClient _apiClient = null!;
        private static TestApiFactory _factory = null!;
        private static string _jwt = null!;
        private static long _categoryId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new TestApiFactory();
            _apiClient = _factory.CreateTestApiClient();
        }

        [SetUp]
        public async Task SetUp()
        {
            _factory.ResetDatabase();

            _jwt = (await _apiClient.LoginAdminUser()).Token;

            LeaderboardViewModel createdLeaderboard = await _apiClient.Post<LeaderboardViewModel>(
                "/api/leaderboards",
                new()
                {
                    Body = new CreateLeaderboardRequest()
                    {
                        Name = "Super Mario 64",
                        Slug = "super_mario_64",
                        Info = null
                    },
                    Jwt = _jwt,
                }
            );

            CategoryViewModel createdCategory = await _apiClient.Post<CategoryViewModel>(
                "/api/categories",
                new()
                {
                    Body = new CreateCategoryRequest()
                    {
                        Name = "120 Stars",
                        Slug = "120_stars",
                        LeaderboardId = createdLeaderboard.Id,
                        Info = null,
                        SortDirection = SortDirection.Ascending,
                        Type = RunType.Time
                    },
                    Jwt = _jwt,
                }
            );

            _categoryId = createdCategory.Id;
        }

        [Test]
        public static async Task CreateRun_OK()
        {
            RunViewModel created = await CreateRun();

            RunViewModel retrieved = await GetRun(created.Id);

            Assert.NotNull(created);
            Assert.AreEqual(created.Id, retrieved.Id);
        }

        [Test]
        public static async Task GetCategory_OK()
        {
            RunViewModel createdRun = await CreateRun();

            CategoryViewModel category = await _apiClient.Get<CategoryViewModel>(
                $"api/runs/{createdRun.Id.ToUrlSafeBase64String()}/category",
                new() { Jwt = _jwt }
            );

            Assert.NotNull(category);
            Assert.AreEqual(category.Id, _categoryId);
        }

        private static async Task<RunViewModel> CreateRun()
        {
            return await _apiClient.Post<RunViewModel>(
                "/api/runs",
                new()
                {
                    Body = new CreateRunRequest
                    {
                        PlayedOn = LocalDate.MinIsoValue,
                        Info = null,
                        TimeOrScore = Duration.FromHours(2).ToInt64Nanoseconds(),
                        CategoryId = _categoryId
                    },
                    Jwt = _jwt
                }
            );
        }

        private static async Task<RunViewModel> GetRun(Guid id)
        {
            return await _apiClient.Get<RunViewModel>($"/api/runs/{id.ToUrlSafeBase64String()}", new() { Jwt = _jwt });
        }
    }
}
