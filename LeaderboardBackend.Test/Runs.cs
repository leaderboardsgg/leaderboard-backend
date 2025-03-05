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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test
{
    [TestFixture]
    internal class Runs
    {
        private static TestApiClient _apiClient = null!;
        private static WebApplicationFactory<Program> _factory = null!;
        private static string _jwt = null!;
        private static long _categoryId;
        private static readonly FakeClock _clock = new(new());

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

            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();
            Leaderboard board = new()
            {
                Name = "Super Mario 64",
                Slug = "super_mario_64",
            };

            Category category = new()
            {
                Name = "120 Stars",
                Slug = "120_stars",
                Info = "120 stars",
                SortDirection = SortDirection.Ascending,
                Type = RunType.Time,
                Leaderboard = board,
            };

            context.Add(category);
            context.SaveChanges();

            _categoryId = category.Id;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => _factory.Dispose();

        [Test]
        public async Task GetRun_OK()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryId,
                Info = "",
                PlayedOn = LocalDate.FromDateTime(new()),
                TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(run);
            await context.SaveChangesAsync();
            // Needed for resolving the run type for viewmodel mapping
            context.Entry(run).Reference(r => r.Category).Load();

            TimedRunViewModel retrieved = await _apiClient.Get<TimedRunViewModel>(
                $"/api/run/{run.Id.ToUrlSafeBase64String()}",
                new() { }
            );

            retrieved.Should().BeEquivalentTo(RunViewModel.MapFrom(run));
        }

        [TestCase("1")]
        [TestCase("AAAAAA")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task GetRun_NotFound(string id) =>
            await FluentActions.Awaiting(() =>
                _apiClient.Get<RunViewModel>(
                $"/api/run/{id}",
                new() { }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public async Task CreateRun_OK()
        {
            RunViewModel created = await CreateRun();

            RunViewModel retrieved = await GetRun<RunViewModel>(created.Id);

            created.Should().NotBeNull();
            created.Id.Should().Be(retrieved.Id);
        }

        [Test]
        public async Task GetCategoryForRun_OK()
        {
            RunViewModel createdRun = await CreateRun();

            CategoryViewModel category = await _apiClient.Get<CategoryViewModel>(
                $"api/run/{createdRun.Id.ToUrlSafeBase64String()}/category",
                new() { Jwt = _jwt }
            );

            category.Should().NotBeNull();
            category.Id.Should().Be(_categoryId);
        }

        private static async Task<RunViewModel> CreateRun() =>
            await _apiClient.Post<RunViewModel>(
                "/runs/create",
                new()
                {
                    Body = new CreateRunRequest
                    {
                        PlayedOn = LocalDate.MinIsoValue,
                        Info = null,
                        CategoryId = _categoryId
                    },
                    Jwt = _jwt
                }
            );

        private static async Task<T> GetRun<T>(Guid id) where T : RunViewModel =>
            await _apiClient.Get<T>(
                $"/api/run/{id.ToUrlSafeBase64String()}",
                new() { Jwt = _jwt }
            );
    }
}
