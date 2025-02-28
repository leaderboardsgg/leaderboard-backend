using System;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
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
                "/leaderboards/create",
                new()
                {
                    Body = new CreateLeaderboardRequest()
                    {
                        Name = "Super Mario 64",
                        Slug = "super_mario_64",
                    },
                    Jwt = _jwt,
                }
            );

            CategoryViewModel createdCategory = await _apiClient.Post<CategoryViewModel>(
                $"/leaderboard/{createdLeaderboard.Id}/categories/create",
                new()
                {
                    Body = new CreateCategoryRequest()
                    {
                        Name = "120 Stars",
                        Slug = "120_stars",
                        Info = "120 stars",
                        SortDirection = SortDirection.Ascending,
                        Type = RunType.Time
                    },
                    Jwt = _jwt,
                }
            );

            _categoryId = createdCategory.Id;
        }

        [Test]
        public static async Task GetRun_OK()
        {
            Run created = await CreateRun(
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.MinIsoValue,
                    TimeOrScore = 1000,
                    UserId = TestInitCommonFields.Admin.Id,
                }
            );

            await FluentActions.Awaiting(() => _apiClient.Get<TimedRunViewModel>(
                $"/api/run/{created.Id.ToUrlSafeBase64String()}",
                new() { }
            ))
                .Should()
                .NotThrowAsync()
                .WithResult(new()
                {
                    Id = created.Id,
                    Info = created.Info,
                    PlayedOn = created.PlayedOn,
                    CreatedAt = created.CreatedAt,
                    UpdatedAt = created.UpdatedAt,
                    DeletedAt = created.DeletedAt,
                    CategoryId = created.CategoryId,
                    UserId = created.UserId,
                    Time = created.Time,
                });
        }

        [Test]
        public static async Task GetRun_NotFound_BadRouteParamType() =>
            await FluentActions.Awaiting(() =>
                _apiClient.Get<RunViewModel>(
                "/api/run/1",
                new() { }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public static async Task GetRun_NotFound_RunDoesNotExist() =>
            await FluentActions.Awaiting(() =>
                _apiClient.Get<RunViewModel>(
                $"/api/run/{Guid.Empty.ToUrlSafeBase64String()}",
                new() { }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public static async Task CreateRun_OK()
        {
            Run created = await CreateRun(
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.MinIsoValue,
                    TimeOrScore = 1000,
                    UserId = TestInitCommonFields.Admin.Id,
                }
            );

            RunViewModel retrieved = await GetRun<RunViewModel>(created.Id);

            created.Should().NotBeNull();
            created.Id.Should().Be(retrieved.Id);
        }

        [Test]
        public static async Task GetCategoryForRun_OK()
        {
            Run createdRun = await CreateRun(
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.MinIsoValue,
                    TimeOrScore = 1000,
                    UserId = TestInitCommonFields.Admin.Id,
                }
            );

            CategoryViewModel category = await _apiClient.Get<CategoryViewModel>(
                $"api/run/{createdRun.Id.ToUrlSafeBase64String()}/category",
                new() { Jwt = _jwt }
            );

            category.Should().NotBeNull();
            category.Id.Should().Be(_categoryId);
        }

        private static async Task<Run> CreateRun(Run run)
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await context.AddAsync(run);
            await context.SaveChangesAsync();
            return run;
        }

        private static async Task<T> GetRun<T>(Guid id) where T : RunViewModel =>
            await _apiClient.Get<T>(
                $"/api/run/{id.ToUrlSafeBase64String()}",
                new() { Jwt = _jwt }
            );
    }
}
