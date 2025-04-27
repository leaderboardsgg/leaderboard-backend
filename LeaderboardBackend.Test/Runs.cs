using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;

namespace LeaderboardBackend.Test
{
    [TestFixture]
    public class Runs
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
        public async Task GetRunsForCategory_OK()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();
            await context.Runs.ExecuteDeleteAsync();

            Run[] runs = [
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(1)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(400).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(2)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                    DeletedAt = _clock.GetCurrentInstant(),
                },
            ];

            context.AddRange(runs);
            await context.SaveChangesAsync();

            foreach (Run run in runs)
            {
                // Needed for resolving the run type for viewmodel mapping
                context.Entry(run).Reference(r => r.Category).Load();
            }

            ListView<TimedRunViewModel> returned = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/category/{_categoryId}/runs?limit=9999999", new());
            returned.Data.Should().BeEquivalentTo(runs.Take(2).Select(RunViewModel.MapFrom));
            returned.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned2 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/category/{_categoryId}/runs?includeDeleted=false&limit=1024", new());
            returned2.Data.Should().BeEquivalentTo(runs.Take(2).Select(RunViewModel.MapFrom));
            returned2.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned3 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/category/{_categoryId}/runs?includeDeleted=true&limit=1024", new());
            returned3.Data.Should().BeEquivalentTo(new Run[] { runs[0], runs[2], runs[1] }.Select(RunViewModel.MapFrom), config => config.WithStrictOrdering());
            returned3.Total.Should().Be(3);

            ListView<TimedRunViewModel> returned4 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/category/{_categoryId}/runs?limit=1", new());
            returned4.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[0]));
            returned4.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned5 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/category/{_categoryId}/runs?limit=1&includeDeleted=true&offset=1", new());
            returned5.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[2]));
            returned5.Total.Should().Be(3);
        }

        [TestCase(-1, 0)]
        [TestCase(1024, -1)]
        public async Task GetRunsForCategory_BadPageData(int limit, int offset) =>
            await _apiClient.Awaiting(
                a => a.Get<RunViewModel>(
                    $"/api/category/{_categoryId}/runs?limit={limit}&offset={offset}",
                    new()
                )
            ).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableContent);

        [Test]
        public async Task GetRunsForCategory_CategoryNotFound()
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(
                a => a.Get<RunViewModel>(
                    "/api/category/0/runs",
                    new()
                )
            ).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Category Not Found");
        }

        [TestCase(UserRole.Confirmed)]
        [TestCase(UserRole.Administrator)]
        public async Task CreateRun_GetRun_OK(UserRole role)
        {
            IUserService service = _factory.Services.GetRequiredService<IUserService>();
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            CreateUserResult result = await service.CreateUser(new()
            {
                Email = $"testuser.createrun.{role}@example.com",
                Password = "P4ssword",
                Username = $"CreateRunTest{role}",
            });

            User? user = await context.FindAsync<User>(result.AsT0.Id);
            user!.Role = role;

            await context.SaveChangesAsync();

            LoginResponse login = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");

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
                    Jwt = login.Token,
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
                    UserId = user.Id,
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
            problemDetails!.Title.Should().Be("Category Not Found");
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
            problemDetails!.Title.Should().Be("Category Is Deleted");
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
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
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

        [Test]
        public async Task UpdateRun_Admin_OK()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.Patch(
                $"/run/{run.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        runType = nameof(RunType.Time),
                        info = "new info",
                    },
                    Jwt = _jwt,
                }
            );
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            Run? updated = await context.FindAsync<Run>(run.Id);
            updated!.Info.Should().Be("new info");
            updated!.CreatedAt.Should().Be(_clock.GetCurrentInstant());
            updated!.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
        }

        [Test]
        public async Task UpdateRun_Confirmed_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            RegisterRequest registerRequest = new()
            {
                Email = "updaterun.ok@example.com",
                Password = "Passw0rd",
                Username = "updaterunok",
            };

            CreateUserResult createdUserResult = await users.CreateUser(registerRequest);
            User? user = await context.FindAsync<User>(createdUserResult.AsT0.Id);
            user!.Role = UserRole.Confirmed;

            Run run = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                User = user!,
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            user.Role.Should().Be(UserRole.Confirmed);
            context.ChangeTracker.Clear();

            LoginResponse userLoginResponse = await _apiClient.LoginUser(
                "updaterun.ok@example.com", "Passw0rd");

            HttpResponseMessage userResponse = await _apiClient.Patch(
                $"/run/{run.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        runType = nameof(RunType.Time),
                        info = "new info",
                    },
                    Jwt = userLoginResponse.Token,
                }
            );
            userResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            Run? updated = await context.FindAsync<Run>(run.Id);
            updated!.Info.Should().Be("new info");
            updated!.CreatedAt.Should().Be(_clock.GetCurrentInstant());
            updated!.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
        }

        [Test]
        public async Task UpdateRun_Unauthenticated()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            Run created = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            await _apiClient.Awaiting(a => a.Patch(
                $"run/{created.Id}",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        Info = "won't work",
                    }
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

            Run? retrieved = await context.FindAsync<Run>(created.Id);
            retrieved!.Info.Should().BeEmpty();
        }

        [TestCase(UserRole.Banned)]
        [TestCase(UserRole.Registered)]
        [TestCase(UserRole.Confirmed)]
        public async Task UpdateRun_BadRole(UserRole role)
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run created = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            string email = $"testuser.updaterun.{role}@example.com";

            RegisterRequest registerRequest = new()
            {
                Email = email,
                Password = "Passw0rd",
                Username = $"UpdateRunTest{role}"
            };

            CreateUserResult result = await users.CreateUser(registerRequest);
            result.IsT0.Should().BeTrue();

            // Log user in first to get their token before updating their role.
            LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            User user = result.AsT0;
            context.Update(user!);
            user!.Role = role;
            await context.SaveChangesAsync();

            await _apiClient.Awaiting(a => a.Patch(
                $"run/{created.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        Info = "should not work",
                    },
                    Jwt = res.Token,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

            context.ChangeTracker.Clear();
            Run? retrieved = await context.FindAsync<Run>(created.Id);
            retrieved!.Info.Should().BeEmpty();
        }

        [Test]
        public async Task UpdateRun_NotFound() =>
            await _apiClient.Awaiting(a => a.Patch(
                $"run/{Guid.Empty.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        Info = "should not work",
                    },
                    Jwt = _jwt,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public async Task UpdateRun_FieldNotAllowed()
        {
            ApplicationContext context = _factory.Services.GetRequiredService<ApplicationContext>();

            Run created = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(
                a => a.Patch(
                    $"run/{created.Id.ToUrlSafeBase64String()}",
                    new()
                    {
                        Body = new
                        {
                            RunType = nameof(RunType.Score),
                            Score = 1L,
                        },
                        Jwt = _jwt,
                    }
                )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.UnprocessableEntity);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Detail.Should().Be("The request's runType does not match the category's.");
        }
    }
}
