using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions.Specialized;
using LeaderboardBackend.Models;
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
        private static readonly FakeClock _clock = new(Instant.FromUtc(2025, 01, 01, 0, 0));

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
        [TestCase("AAAAAAAAAAAAAAAAAAAAAA")]
        public async Task GetRun_NotFound(string id) =>
            await FluentActions.Awaiting(() =>
                _apiClient.Get<RunViewModel>(
                $"/api/run/{id}",
                new() { }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public async Task CreateRun_GetRun_OK()
        {
            TimedRunViewModel created = await _apiClient.Post<TimedRunViewModel>(
                $"/category/{_categoryId}/runs/create",
                new()
                {
                    Body = new
                    {
                        runType = nameof(RunType.Time),
                        info = "",
                        playedOn = "2025-01-01",
                        time = "00:10:22.111",
                    },
                    Jwt = _jwt
                }
            );

            TimedRunViewModel retrieved = await _apiClient.Get<TimedRunViewModel>(
                $"/api/run/{created.Id.ToUrlSafeBase64String()}",
                new() { }
            );

            retrieved.Should().BeEquivalentTo(created);
            retrieved.Should().BeEquivalentTo(
                new
                {
                    PlayedOn = LocalDate.FromDateTime(new(2025, 1, 1)),
                    Info = "",
                    Time = Duration.FromMilliseconds(622111),
                }
            );
        }

        [Test]
        public async Task CreateRun_Unauthenticated() =>
            await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/category/{_categoryId}/runs/create",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        PlayedOn = "2025-01-01",
                        Info = "",
                    }
                }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

        [TestCase(UserRole.Banned)]
        [TestCase(UserRole.Registered)]
        public async Task CreateRun_BadRole(UserRole role)
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService service = scope.ServiceProvider.GetRequiredService<IUserService>();

            await service.CreateUser(new()
            {
                Email = $"testuser.createrun.{role}@example.com",
                Password = "P4ssword",
                Username = $"CreateRunTest{role}"
            });

            LoginResponse user = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/category/{_categoryId}/runs/create",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        PlayedOn = "2025-01-01",
                        Time = "00:10:00.000"
                    },
                    Jwt = user.Token,
                }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task CreateRun_CategoryNotFound()
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                "/category/0/runs/create",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        PlayedOn = "2025-01-01",
                        Info = "",
                        Time = Duration.FromMinutes(12)
                    },
                    Jwt = _jwt,
                }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Category Not Found.");
        }

        [Test]
        public async Task CreateRun_CategoryDeleted()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            Category deleted = new()
            {
                Name = "createrun-deletedcat",
                Slug = "createrun-deletedcat",
                DeletedAt = _clock.GetCurrentInstant(),
                Leaderboard = context.Leaderboards.First(),
                SortDirection = SortDirection.Ascending,
                Type = RunType.Time,
            };

            context.Add(deleted);
            await context.SaveChangesAsync();

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/category/{deleted.Id}/runs/create",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        PlayedOn = "2025-01-01",
                        Info = "",
                        Time = Duration.FromMinutes(39)
                    },
                    Jwt = _jwt,
                }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Category Is Deleted.");
        }

        [TestCase("", "", "00:10:30.111")]
        [TestCase(null, "", "00:10:30.111")]
        [TestCase("2025-01-01", "", "aaaa")]
        [TestCase("2025-01-01", "", "123123")]
        [TestCase("2025-01-01", "", null)]
        public async Task CreateRun_BadData(string? playedOn, string info, string? time)
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/category/{_categoryId}/runs/create",
                new()
                {
                    Body = new
                    {
                        runType = nameof(RunType.Time),
                        playedOn = playedOn,
                        info = info,
                        time = time,
                    },
                    Jwt = _jwt,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task GetCategoryForRun_OK()
        {
            TimedRunViewModel createdRun = await _apiClient.Post<TimedRunViewModel>(
                $"/category/{_categoryId}/runs/create",
                new()
                {
                    Body = new CreateTimedRunRequest
                    {
                        PlayedOn = LocalDate.MinIsoValue,
                        Info = "",
                        Time = Duration.FromMilliseconds(390000),
                    },
                    Jwt = _jwt
                }
            );

            CategoryViewModel category = await _apiClient.Get<CategoryViewModel>(
                $"api/run/{createdRun.Id.ToUrlSafeBase64String()}/category",
                new() { Jwt = _jwt }
            );

            category.Should().NotBeNull();
            category.Id.Should().Be(_categoryId);
        }
    }
}
