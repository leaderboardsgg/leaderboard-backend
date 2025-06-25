using System;
using System.Collections.Generic;
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

            using IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
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
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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
            await context.Entry(run).Reference(r => r.Category).LoadAsync();
            await context.Entry(run).Reference(r => r.User).LoadAsync();

            TimedRunViewModel retrieved = await _apiClient.Get<TimedRunViewModel>(
                $"/api/runs/{run.Id.ToUrlSafeBase64String()}",
                new() { }
            );

            retrieved.Should().BeEquivalentTo(RunViewModel.MapFrom(run));
        }

        [TestCase("1")]
        [TestCase("AAAAAAAAAAAAAAAAAAAAAA")]
        public async Task GetRun_NotFound(string id) =>
            await FluentActions.Awaiting(() =>
                _apiClient.Get<RunViewModel>(
                $"/api/runs/{id}",
                new() { }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

        [Test]
        public async Task GetRunsForCategory_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
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
                await context.Entry(run).Reference(r => r.Category).LoadAsync();
                await context.Entry(run).Reference(r => r.User).LoadAsync();
            }

            ListView<TimedRunViewModel> returned = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/runs?limit=9999999", new());
            returned.Data.Should().BeEquivalentTo(runs.Take(2).Select(RunViewModel.MapFrom));
            returned.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned2 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/runs?status=published&limit=1024", new());
            returned2.Data.Should().BeEquivalentTo(runs.Take(2).Select(RunViewModel.MapFrom));
            returned2.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned3 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/runs?status=any&limit=1024", new());
            returned3.Data.Should().BeEquivalentTo(new Run[] { runs[0], runs[2], runs[1] }.Select(RunViewModel.MapFrom), config => config.WithStrictOrdering());
            returned3.Total.Should().Be(3);

            ListView<TimedRunViewModel> returned4 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/runs?limit=1", new());
            returned4.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[0]));
            returned4.Total.Should().Be(2);

            ListView<TimedRunViewModel> returned5 = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/runs?limit=1&status=any&offset=1", new());
            returned5.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[2]));
            returned5.Total.Should().Be(3);
        }

        [TestCase(-1, 0)]
        [TestCase(1024, -1)]
        public async Task GetRunsForCategory_BadPageData(int limit, int offset) =>
            await _apiClient.Awaiting(
                a => a.Get<RunViewModel>(
                    $"/api/categories/{_categoryId}/runs?limit={limit}&offset={offset}",
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
                    "/api/categories/0/runs",
                    new()
                )
            ).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Category Not Found");
        }

        [Test]
        public async Task GetRecordsForCategory_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            await context.Runs.ExecuteDeleteAsync();

            CreateUserResult userResult = await users.CreateUser(new()
            {
                Username = "getrecords-ok",
                Email = "getrecords_ok@leaderboards.gg",
                Password = "P4ssword",
            });
            userResult.IsT0.Should().BeTrue();
            User user = userResult.AsT0;

            CreateUserResult user1Result = await users.CreateUser(new()
            {
                Username = "getrecords-ok1",
                Email = "getrecords_ok1@leaderboards.gg",
                Password = "P4ssword",
            });
            user1Result.IsT0.Should().BeTrue();
            User user1 = user1Result.AsT0;

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
                    UserId = user.Id,
                },
                new()
                {
                    CategoryId = _categoryId,
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(2)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(400).ToInt64Nanoseconds(),
                    UserId = user1.Id,
                },
            ];

            context.AddRange(runs);
            await context.SaveChangesAsync();

            foreach (Run run in runs)
            {
                // Needed for resolving the run type for viewmodel mapping
                await context.Entry(run).Reference(r => r.Category).LoadAsync();
                await context.Entry(run).Reference(r => r.User).LoadAsync();
            }

            // TODO: Test for rank. As of this writing, rank calculation will
            // be handled in another PR. (And of course remove this comment
            // in that PR)

            ListView<TimedRunViewModel> returned = await _apiClient.Get<ListView<TimedRunViewModel>>($"/api/categories/{_categoryId}/records?limit=9999999", new());
            returned.Data.Should().BeEquivalentTo(
                new List<Run>([runs[0], runs[2], runs[3]]).Select(RunViewModel.MapFrom),
                config => config.WithStrictOrdering()
            );
            returned.Total.Should().Be(3);
        }

        [TestCase(-1, 0)]
        [TestCase(1024, -1)]
        public async Task GetRecordsForCategory_BadPageData(int limit, int offset) =>
            await _apiClient.Awaiting(
                a => a.Get<RunViewModel>(
                    $"/api/categories/{_categoryId}/records?limit={limit}&offset={offset}",
                    new()
                )
            ).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(ex => ex.Response.StatusCode == HttpStatusCode.UnprocessableContent);

        [Test]
        public async Task GetRecordsForCategory_CategoryNotFound()
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(
                a => a.Get<RunViewModel>(
                    "/api/categories/0/records",
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
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService service = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            CreateUserResult result = await service.CreateUser(new()
            {
                Email = $"testuser.createrun.{role}@example.com",
                Password = "P4ssword",
                Username = $"CreateRunTest{role}",
            });

            result.IsT0.Should().BeTrue();
            User user = result.AsT0;
            context.Update(user);
            user!.Role = role;

            await context.SaveChangesAsync();

            LoginResponse login = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");

            TimedRunViewModel created = await _apiClient.Post<TimedRunViewModel>(
                $"/categories/{_categoryId}/runs",
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
                $"/api/runs/{created.Id.ToUrlSafeBase64String()}",
                new() { }
            );

            retrieved.Should().BeEquivalentTo(created);
            retrieved.Should().BeEquivalentTo(
                new
                {
                    PlayedOn = LocalDate.FromDateTime(new(2025, 1, 1)),
                    Info = "",
                    Time = Duration.FromMilliseconds(622111),
                    User = UserViewModel.MapFrom(user)
                }
            );
        }

        [Test]
        public async Task CreateRun_Unauthenticated() =>
            await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/categories/{_categoryId}/runs",
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
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            IUserService service = scope.ServiceProvider.GetRequiredService<IUserService>();

            CreateUserResult createUserResult = await service.CreateUser(new()
            {
                Email = $"testuser.createrun.{role}@example.com",
                Password = "P4ssword",
                Username = $"CreateRunTest{role}"
            });

            // Log in to get a token first, then update the user's role
            LoginResponse res = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");

            createUserResult.IsT0.Should().BeTrue();
            User user = createUserResult.AsT0;
            context.Update(user);
            user.Role = role;
            await context.SaveChangesAsync();

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                $"/categories/{_categoryId}/runs",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        PlayedOn = "2025-01-01",
                        Time = "00:10:00.000"
                    },
                    Jwt = res.Token,
                }
            )).Should()
            .ThrowAsync<RequestFailureException>()
            .Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task CreateRun_CategoryNotFound()
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Post<RunViewModel>(
                "/categories/0/runs",
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
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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
                $"/categories/{deleted.Id}/runs",
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
                $"/categories/{_categoryId}/runs",
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
                $"/categories/{_categoryId}/runs",
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
                $"api/runs/{createdRun.Id.ToUrlSafeBase64String()}/category",
                new() { Jwt = _jwt }
            );

            category.Should().NotBeNull();
            category.Id.Should().Be(_categoryId);
        }

        [Test]
        public async Task UpdateRun_Admin_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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
                $"/runs/{run.Id.ToUrlSafeBase64String()}",
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

            CreateUserResult createUserResult = await users.CreateUser(registerRequest);
            createUserResult.IsT0.Should().BeTrue();
            User user = createUserResult.AsT0;
            context.Update(user);
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
                $"/runs/{run.Id.ToUrlSafeBase64String()}",
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
            IServiceScope scope = _factory.Services.CreateScope();
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
            context.ChangeTracker.Clear();

            await _apiClient.Awaiting(a => a.Patch(
                $"runs/{created.Id}",
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
            context.Update(user);
            user.Role = role;
            await context.SaveChangesAsync();

            await _apiClient.Awaiting(a => a.Patch(
                $"runs/{created.Id.ToUrlSafeBase64String()}",
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
        public async Task UpdateRun_UserDoesNotOwnRun()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            RegisterRequest registerRequest = new()
            {
                Email = "testuser.updaterun.doesnotown@example.com",
                Password = "Passw0rd",
                Username = $"UpdateRunTestDoesNotOwnRun"
            };

            CreateUserResult result = await users.CreateUser(registerRequest);
            result.IsT0.Should().BeTrue();
            User user = result.AsT0;
            context.Update(user);
            user.Role = UserRole.Confirmed;

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

            LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
                $"runs/{created.Id.ToUrlSafeBase64String()}",
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

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("User Does Not Own Run");
        }

        [Test]
        public async Task UpdateRun_NotFound() =>
            await _apiClient.Awaiting(a => a.Patch(
                $"runs/{Guid.Empty.ToUrlSafeBase64String()}",
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
        public async Task UpdateRun_RunAlreadyDeleted()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            string email = "testuser.updaterun.alreadydeleted@example.com";

            RegisterRequest registerRequest = new()
            {
                Email = email,
                Password = "Passw0rd",
                Username = "UpdateRunTestAlreadyDeleted",
            };

            CreateUserResult result = await users.CreateUser(registerRequest);
            result.IsT0.Should().BeTrue();
            User user = result.AsT0;
            context.Update(user);
            user.Role = UserRole.Confirmed;

            Run created = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                UserId = user.Id,
                Time = Duration.FromSeconds(390),
                DeletedAt = _clock.GetCurrentInstant(),
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);

            LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
                $"runs/{created.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        Info = "should not work",
                    },
                    Jwt = res.Token,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Run Is Deleted");
        }

        [Test]
        public async Task UpdateRun_LeaderboardAlreadyDeleted()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            string email = "testuser.updaterun.boardalreadydeleted@example.com";

            RegisterRequest registerRequest = new()
            {
                Email = email,
                Password = "Passw0rd",
                Username = "UpdateRunTestBoardAlreadyDeleted",
            };

            CreateUserResult result = await users.CreateUser(registerRequest);
            result.IsT0.Should().BeTrue();
            User user = result.AsT0;
            context.Update(user);
            user.Role = UserRole.Confirmed;

            Leaderboard leaderboard = new()
            {
                Name = "UpdateRunDeletedBoardBoard",
                Slug = "update-run-deleted-board-board",
                DeletedAt = _clock.GetCurrentInstant(),
            };

            Category category = new()
            {
                Name = "UpdateRunDeletedBoardCat",
                Slug = "update-run-deleted-board-cat",
                SortDirection = SortDirection.Ascending,
                Type = RunType.Time,
                Leaderboard = leaderboard,
            };

            Run created = new()
            {
                Category = category,
                PlayedOn = LocalDate.MinIsoValue,
                UserId = user.Id,
                Time = Duration.FromSeconds(390),
            };

            context.AddRange(leaderboard, category, created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);

            LoginResponse res = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Patch(
                $"runs/{created.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Body = new
                    {
                        RunType = nameof(RunType.Time),
                        Info = "should not work",
                    },
                    Jwt = res.Token,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Leaderboard Is Deleted");
        }

        [Test]
        public async Task UpdateRun_FieldNotAllowed()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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
                    $"runs/{created.Id.ToUrlSafeBase64String()}",
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
            problemDetails!.Title.Should().Be("Incorrect Run Type");
        }

        [Test]
        public async Task DeleteRun_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

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

            HttpResponseMessage message = await _apiClient.Delete(
                $"runs/{created.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Jwt = _jwt,
                }
            );

            message.StatusCode.Should().Be(HttpStatusCode.NoContent);

            Run? deleted = await context.FindAsync<Run>(created.Id);
            deleted!.UpdatedAt.Should().Be(_clock.GetCurrentInstant());
            deleted!.DeletedAt.Should().Be(_clock.GetCurrentInstant());
        }

        [Test]
        public async Task DeleteRun_Unauthenticated()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            await _apiClient.Awaiting(a => a.Delete(
                $"runs/{run.Id.ToUrlSafeBase64String()}",
                new() { }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Unauthorized);

            Run? retrieved = await context.FindAsync<Run>(run.Id);
            retrieved!.DeletedAt.Should().BeNull();
        }

        [TestCase(UserRole.Banned)]
        [TestCase(UserRole.Confirmed)]
        [TestCase(UserRole.Registered)]
        public async Task DeleteRun_BadRole(UserRole role)
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            string email = $"deleterun.badrole.{role}@example.com";
            CreateUserResult createUserResult = await users.CreateUser(new()
            {
                Email = email,
                Password = "Passw0rd",
                Username = $"DeleteRunTest{role}",
            });

            createUserResult.IsT0.Should().BeTrue();
            User user = createUserResult.AsT0;
            context.Update(user);
            user.Role = role;

            LoginResponse res = await _apiClient.LoginUser(email, "Passw0rd");

            Run run = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(run);

            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            await _apiClient.Awaiting(a => a.Delete(
                $"runs/{run.Id.ToUrlSafeBase64String()}",
                new() { Jwt = res.Token }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.Forbidden);

            Run? retrieved = await context.FindAsync<Run>(run.Id);
            retrieved!.DeletedAt.Should().BeNull();
        }

        [Test]
        public async Task DeleteRun_NotFound()
        {
            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Delete(
                $"runs/{Guid.Empty.ToUrlSafeBase64String()}",
                new()
                {
                    Jwt = _jwt,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Not Found");
        }

        [Test]
        public async Task DeleteRun_NotFound_AlreadyDeleted()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryId,
                PlayedOn = LocalDate.MinIsoValue,
                DeletedAt = _clock.GetCurrentInstant(),
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            run.DeletedAt.Should().NotBeNull();
            context.ChangeTracker.Clear();

            ExceptionAssertions<RequestFailureException> exAssert = await _apiClient.Awaiting(a => a.Delete(
                $"runs/{run.Id.ToUrlSafeBase64String()}",
                new()
                {
                    Jwt = _jwt,
                }
            )).Should().ThrowAsync<RequestFailureException>().Where(e => e.Response.StatusCode == HttpStatusCode.NotFound);

            ProblemDetails? problemDetails = await exAssert.Which.Response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Already Deleted");

            Run? retrieved = await context.FindAsync<Run>(run.Id);
            retrieved!.UpdatedAt.Should().BeNull();
        }
    }
}
