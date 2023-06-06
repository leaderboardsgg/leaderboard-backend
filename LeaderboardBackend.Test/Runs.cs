using System;
using System.Threading.Tasks;
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
        private static TestApiClient s_apiClient = null!;
        private static TestApiFactory s_factory = null!;
        private static string s_jwt = null!;
        private static long s_categoryId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            s_factory = new TestApiFactory();
            s_apiClient = s_factory.CreateTestApiClient();
        }

        [SetUp]
        public async Task SetUp()
        {
            s_factory.ResetDatabase();

            s_jwt = (await s_apiClient.LoginAdminUser()).Token;

            LeaderboardViewModel createdLeaderboard = await s_apiClient.Post<LeaderboardViewModel>(
                "/api/leaderboards",
                new()
                {
                    Body = new CreateLeaderboardRequest()
                    {
                        Name = Generators.GenerateRandomString(),
                        Slug = Generators.GenerateRandomString(),
                    },
                    Jwt = s_jwt,
                }
            );

            CategoryViewModel createdCategory = await s_apiClient.Post<CategoryViewModel>(
                "/api/categories",
                new()
                {
                    Body = new CreateCategoryRequest()
                    {
                        Name = Generators.GenerateRandomString(),
                        Slug = Generators.GenerateRandomString(),
                        LeaderboardId = createdLeaderboard.Id,
                    },
                    Jwt = s_jwt,
                }
            );

            s_categoryId = createdCategory.Id;
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

            CategoryViewModel category = await s_apiClient.Get<CategoryViewModel>(
                $"api/runs/{createdRun.Id}/category",
                new() { Jwt = s_jwt }
            );

            Assert.NotNull(category);
            Assert.AreEqual(category.Id, s_categoryId);
        }

        private static async Task<RunViewModel> CreateRun()
        {
            return await s_apiClient.Post<RunViewModel>(
                "/api/runs",
                new()
                {
                    Body = new CreateRunRequest
                    {
                        PlayedOn = LocalDate.MinIsoValue,
                        SubmittedAt = Instant.MaxValue,
                        CategoryId = s_categoryId
                    },
                    Jwt = s_jwt
                }
            );
        }

        private static async Task<RunViewModel> GetRun(Guid id)
        {
            return await s_apiClient.Get<RunViewModel>($"/api/runs/{id}", new() { Jwt = s_jwt });
        }
    }
}
