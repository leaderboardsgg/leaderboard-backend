using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using Microsoft.AspNetCore.Http;
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
        private static HttpClient _apiClient = null!;
        private static WebApplicationFactory<Program> _factory = null!;
        private static string _jwt = null!;
        private static long[] _categoryIds = [];
        private static readonly FakeClock _clock = new(Instant.FromUtc(2025, 01, 01, 0, 0));

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _factory = new TestApiFactory().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    services.AddSingleton<IClock, FakeClock>(_ => _clock)
                )
            );

            _apiClient = _factory.CreateClient();
            using IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await TestApiFactory.ResetDatabase(context);

            HttpResponseMessage response = await _apiClient.LoginAdminUser();
            LoginResponse? loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
            _jwt = loginResponse!.Token;

            Leaderboard board = new()
            {
                Name = "Super Mario 64",
                Slug = "super_mario_64",
            };

            Category[] categories = [
                new ()
                {
                    Name = "120 Stars",
                    Slug = "120_stars",
                    Info = "120 stars",
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Time,
                    Leaderboard = board,
                },
                new ()
                {
                    Name = "Min Stars",
                    Slug = "min_stars",
                    Info = "Min stars",
                    SortDirection = SortDirection.Ascending,
                    Type = RunType.Time,
                    Leaderboard = board,
                },
            ];

            context.AddRange(categories);
            context.SaveChanges();

            _categoryIds = [.. categories.Select(cat => cat.Id)];
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _apiClient.Dispose();
            _factory.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _apiClient.DefaultRequestHeaders.Authorization = null;
        }

        [Test]
        public async Task GetRun_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryIds[0],
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

            HttpResponseMessage response = await _apiClient.GetRun(run.Id);
            response.Should().Be200Ok().And.BeAs(
                (TimedRunViewModel)RunViewModel.MapFrom(run)
            );
        }

        [TestCase("1")]
        [TestCase("AAAAAAAAAAAAAAAAAAAAAA")]
        public async Task GetRun_NotFound(string id)
        {
            HttpResponseMessage response = await _apiClient.GetAsync($"/api/runs/{id}");
            response.Should().Be404NotFound();
        }

        [Test]
        public async Task GetRunsForCategory_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await context.Runs.ExecuteDeleteAsync();

            Run[] runs = [
                new()
                {
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(1)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(400).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(2)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                    DeletedAt = _clock.GetCurrentInstant(),
                },
                new()
                {
                    CategoryId = _categoryIds[1],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().ToDateTimeUtc()),
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

            HttpResponseMessage response = await _apiClient.GetRunsForCategory(_categoryIds[0], 9999999);

            response.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Should().BeEquivalentTo(runs.Take(2).Select(TimedRunViewModel.MapFrom));
                listView.Total.Should().Be(2);
            });

            HttpResponseMessage response2 = await _apiClient.GetRunsForCategory(
                _categoryIds[0],
                1024,
                null,
                StatusFilter.Published);

            response2.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Should().BeEquivalentTo(runs.Take(2).Select(RunViewModel.MapFrom));
                listView.Total.Should().Be(2);
            });

            HttpResponseMessage response3 = await _apiClient.GetRunsForCategory(
                _categoryIds[0],
                1024,
                null,
                StatusFilter.Published);

            response3.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Should().BeEquivalentTo(new Run[]
                {
                    runs[0],
                    runs[1]
                }.Select(RunViewModel.MapFrom), config => config.WithStrictOrdering());

                listView.Total.Should().Be(2);
            });

            HttpResponseMessage response4 = await _apiClient.GetRunsForCategory(_categoryIds[0], 1);

            response4.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[0]));
                listView.Total.Should().Be(2);
            });

            HttpResponseMessage response5 = await _apiClient.GetRunsForCategory(
                _categoryIds[0],
                1,
                1,
                StatusFilter.Any);

            response5.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Single().Should().BeEquivalentTo(RunViewModel.MapFrom(runs[2]));
                listView.Total.Should().Be(3);
            });
        }

        [TestCase(-1, 0)]
        [TestCase(1024, -1)]
        public async Task GetRunsForCategory_BadPageData(int limit, int offset)
        {
            HttpResponseMessage response = await _apiClient.GetRunsForCategory(_categoryIds[0], limit, offset);
            response.Should().Be422UnprocessableEntity();
        }

        [Test]
        public async Task GetRunsForCategory_CategoryNotFound()
        {
            HttpResponseMessage response = await _apiClient.GetRunsForCategory(0);

            response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(details =>
            {
                details.Title.Should().Be("Category Not Found");
            });
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
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(1)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(400).ToInt64Nanoseconds(),
                    UserId = TestInitCommonFields.Admin.Id,
                },
                new()
                {
                    CategoryId = _categoryIds[0],
                    Info = "",
                    PlayedOn = LocalDate.FromDateTime(_clock.GetCurrentInstant().Plus(Duration.FromDays(2)).ToDateTimeUtc()),
                    TimeOrScore = Duration.FromSeconds(390).ToInt64Nanoseconds(),
                    UserId = user.Id,
                },
                new()
                {
                    CategoryId = _categoryIds[0],
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

            HttpResponseMessage response = await _apiClient.GetRecordsForCategory(_categoryIds[0], 9999999);

            response.Should().Be200Ok().And.Satisfy<ListView<TimedRunViewModel>>(listView =>
            {
                listView.Data.Should().BeEquivalentTo(
                    [
                        RunViewModel.MapFrom(new RankedRun
                        {
                            Rank = 1,
                            Run = runs[0]
                        }),
                        RunViewModel.MapFrom(new RankedRun
                        {
                            Rank = 1,
                            Run = runs[2]
                        }),
                        RunViewModel.MapFrom(new RankedRun
                        {
                            Rank = 3,
                            Run = runs[3]
                        })],
                    config => config.WithStrictOrdering());

                listView.Total.Should().Be(3);
            });
        }

        [TestCase(-1, 0)]
        [TestCase(1024, -1)]
        public async Task GetRecordsForCategory_BadPageData(int limit, int offset)
        {
            HttpResponseMessage response = await _apiClient.GetRecordsForCategory(_categoryIds[0], limit, offset);
            response.Should().Be422UnprocessableEntity();
        }

        [Test]
        public async Task GetRecordsForCategory_CategoryNotFound()
        {
            HttpResponseMessage response = await _apiClient.GetRecordsForCategory(0);

            response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(details =>
                details.Title.Should().Be("Category Not Found"));
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

            HttpResponseMessage response = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");
            LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
            _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", login?.Token);

            HttpResponseMessage response2 = await _apiClient.CreateRun(
                _categoryIds[0],
                new CreateTimedRunRequest
                {
                    Info = "",
                    PlayedOn = new(2025, 1, 1),
                    Time = Duration.FromTimeSpan(new(0, 0, 10, 22, 111)),
                });

            TimedRunViewModel? created = await response2.Content.ReadFromJsonAsync<TimedRunViewModel>(
                TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage response3 = await _apiClient.GetRun(created!.Id);

            response3.Should().Be200Ok().And.BeAs(created).And.BeAs(new TimedRunViewModel()
            {
                PlayedOn = new(2025, 1, 1),
                Info = "",
                Time = Duration.FromTimeSpan(new(0, 0, 10, 22, 111)),
                User = UserViewModel.MapFrom(user),
                CategoryId = _categoryIds[0],
                CreatedAt = _clock.GetCurrentInstant(),
                DeletedAt = null,
                Id = created.Id,
                Status = Status.Published,
                UpdatedAt = null
            });
        }

        [Test]
        public async Task CreateRun_Unauthenticated()
        {
            HttpResponseMessage response = await _apiClient.CreateRun(_categoryIds[0], new CreateTimedRunRequest()
            {
                PlayedOn = new(2025, 1, 1),
                Info = ""
            });

            response.Should().Be401Unauthorized();
        }

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
            HttpResponseMessage response = await _apiClient.LoginUser($"testuser.createrun.{role}@example.com", "P4ssword");
            LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
            _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", login!.Token);

            createUserResult.IsT0.Should().BeTrue();
            User user = createUserResult.AsT0;
            context.Update(user);
            user.Role = role;
            await context.SaveChangesAsync();

            HttpResponseMessage response2 = await _apiClient.CreateRun(_categoryIds[0], new CreateTimedRunRequest
            {
                PlayedOn = new(2025, 1, 1),
                Time = Duration.FromMinutes(10)
            });

            response2.Should().Be403Forbidden();
        }

        [Test]
        public async Task CreateRun_CategoryNotFound()
        {
            _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

            HttpResponseMessage response = await _apiClient.CreateRun(0, new CreateTimedRunRequest()
            {
                PlayedOn = new(2025, 1, 1),
                Info = "",
                Time = Duration.FromMinutes(1)
            });

            response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(details =>
            {
                details.Title.Should().Be("Category Not Found");
            });
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

            _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

            HttpResponseMessage response = await _apiClient.CreateRun(
                deleted.Id,
                new CreateTimedRunRequest()
                {
                    PlayedOn = new(2025, 1, 1),
                    Info = "",
                    Time = Duration.FromMinutes(39)
                });

            response.Should().Be404NotFound().And.Satisfy<ProblemDetails>(
                details => details.Title.Should().Be("Category Is Deleted"));
        }

        [TestCase("", "", "00:10:30.111")]
        [TestCase(null, "", "00:10:30.111")]
        [TestCase("2025-01-01", "", "aaaa")]
        [TestCase("2025-01-01", "", "123123")]
        [TestCase("2025-01-01", "", null)]
        public async Task CreateRun_BadData(string? playedOn, string info, string? time)
        {
            _apiClient.DefaultRequestHeaders.Authorization = new("Bearer", _jwt);

            HttpResponseMessage response = await _apiClient.PostAsJsonAsync(
                $"/categories/{_categoryIds[0]}/runs",
                $$"""
                {
                    "$type": "Time",
                    "playedOn": {{playedOn}},
                    "info": {{info}},
                    "time": {{time}}
                }
                """,
                TestInitCommonFields.JsonSerializerOptions);

            response.Should().Be400BadRequest();
        }

        [Test]
        public async Task GetCategoryForRun_OK()
        {
            HttpResponseMessage runResponse = await _apiClient.CreateRun(
                _categoryIds[0],
                new CreateTimedRunRequest
                {
                    PlayedOn = LocalDate.MinIsoValue,
                    Info = "",
                    Time = Duration.FromMilliseconds(390000),
                },
                _jwt
            );

            TimedRunViewModel? createdRun = await runResponse.Content.ReadFromJsonAsync<TimedRunViewModel>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage categoryResponse = await _apiClient.GetCategoryForRun(
                createdRun!.Id
            );

            CategoryViewModel? category = await categoryResponse.Content.ReadFromJsonAsync<CategoryViewModel>(TestInitCommonFields.JsonSerializerOptions);

            category.Should().NotBeNull();
            category.Id.Should().Be(_categoryIds[0]);
        }

        [Test]
        public async Task UpdateRun_Admin_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.UpdateRun(
                run.Id,
                new UpdateTimedRunRequest
                {
                    Info = "new info",
                },
                _jwt
            );
            response.Should().Be204NoContent();

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
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                User = user!,
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            user.Role.Should().Be(UserRole.Confirmed);
            context.ChangeTracker.Clear();

            HttpResponseMessage userLoginResponseMessage = await _apiClient.LoginUser(
                "updaterun.ok@example.com", "Passw0rd");
            LoginResponse? userLoginResponse = await userLoginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage userResponse = await _apiClient.UpdateRun(
                run.Id,
                new UpdateTimedRunRequest
                {
                    Info = "new info",
                },
                userLoginResponse!.Token
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
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Info = "won't work",
                }
            );
            response.Should().Be401Unauthorized();

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
                CategoryId = _categoryIds[0],
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
            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);
            User user = result.AsT0;
            context.Update(user);
            user.Role = role;
            await context.SaveChangesAsync();

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Info = "should not work",
                },
                res!.Token
            );
            response.Should().Be403Forbidden();

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
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);

            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Info = "should not work",
                },
                res!.Token
            );
            response.Should().Be403Forbidden();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("User Does Not Own Run");
        }

        [Test]
        public async Task UpdateRun_UserCannotRestoreRuns()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            IUserService users = scope.ServiceProvider.GetRequiredService<IUserService>();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            RegisterRequest registerRequest = new()
            {
                Email = "testuser.updaterun.cannotrestore@example.com",
                Password = "Passw0rd",
                Username = $"UpdateRunTestCannotRestoreRuns"
            };

            CreateUserResult result = await users.CreateUser(registerRequest);
            result.IsT0.Should().BeTrue();
            User user = result.AsT0;
            context.Update(user);
            user.Role = UserRole.Confirmed;

            Run created = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = user.Id,
                DeletedAt = _clock.GetCurrentInstant(),
            };

            // To assert that role check supersedes user ID check
            Run created1 = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                Time = Duration.FromSeconds(390),
                UserId = TestInitCommonFields.Admin.Id,
                DeletedAt = _clock.GetCurrentInstant(),
            };

            context.AddRange(created, created1);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            created1.Id.Should().NotBe(Guid.Empty);

            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Status = Status.Published,
                },
                res!.Token
            );
            response.Should().Be403Forbidden();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("User Cannot Change Status of Runs");

            HttpResponseMessage response1 = await _apiClient.UpdateRun(
                created1.Id,
                new UpdateTimedRunRequest
                {
                    Status = Status.Published,
                },
                res!.Token
            );
            response.Should().Be403Forbidden();

            ProblemDetails? problemDetails1 = await response1.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails1.Should().NotBeNull();
            problemDetails1!.Title.Should().Be("User Cannot Change Status of Runs");
        }

        [Test]
        public async Task UpdateRun_NotFound()
        {
            HttpResponseMessage response = await _apiClient.UpdateRun(
                Guid.Empty,
                new UpdateTimedRunRequest
                {
                    Info = "should not work",
                },
                _jwt
            );
            response.Should().Be404NotFound();
        }

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
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                UserId = user.Id,
                Time = Duration.FromSeconds(390),
                DeletedAt = _clock.GetCurrentInstant(),
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);

            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Info = "should not work"
                },
                res!.Token
            );
            response.Should().Be404NotFound();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
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

            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(registerRequest.Email, registerRequest.Password);
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateTimedRunRequest
                {
                    Info = "should not work",
                },
                res!.Token
            );
            response.Should().Be404NotFound();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Leaderboard Is Deleted");
        }

        [Test]
        public async Task UpdateRun_FieldNotAllowed()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run created = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.UpdateRun(
                created.Id,
                new UpdateScoredRunRequest
                {
                    Score = 1L,
                },
                _jwt
            );
            response.Should().Be422UnprocessableEntity();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Incorrect Run Type");
        }

        [Test]
        public async Task DeleteRun_OK()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run created = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(created);
            await context.SaveChangesAsync();
            created.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage message = await _apiClient.DeleteRun(
                created.Id,
                _jwt
            );
            message.Should().Be204NoContent();

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
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(run);
            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.DeleteRun(run.Id);
            response.Should().Be401Unauthorized();

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

            HttpResponseMessage loginResponseMessage = await _apiClient.LoginUser(email, "Passw0rd");
            LoginResponse? res = await loginResponseMessage.Content.ReadFromJsonAsync<LoginResponse>(TestInitCommonFields.JsonSerializerOptions);

            Run run = new()
            {
                CategoryId = _categoryIds[0],
                PlayedOn = LocalDate.MinIsoValue,
                UserId = TestInitCommonFields.Admin.Id,
                Time = Duration.FromSeconds(390),
            };

            context.Add(run);

            await context.SaveChangesAsync();
            run.Id.Should().NotBe(Guid.Empty);
            context.ChangeTracker.Clear();

            HttpResponseMessage response = await _apiClient.DeleteRun(
                run.Id,
                res!.Token
            );
            response.Should().Be403Forbidden();

            Run? retrieved = await context.FindAsync<Run>(run.Id);
            retrieved!.DeletedAt.Should().BeNull();
        }

        [Test]
        public async Task DeleteRun_NotFound()
        {
            HttpResponseMessage response = await _apiClient.DeleteRun(
                Guid.Empty,
                _jwt
            );
            response.Should().Be404NotFound();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Not Found");
        }

        [Test]
        public async Task DeleteRun_NotFound_AlreadyDeleted()
        {
            IServiceScope scope = _factory.Services.CreateScope();
            ApplicationContext context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

            Run run = new()
            {
                CategoryId = _categoryIds[0],
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

            HttpResponseMessage response = await _apiClient.DeleteRun(
                run.Id,
                _jwt
            );
            response.Should().Be404NotFound();

            ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestInitCommonFields.JsonSerializerOptions);
            problemDetails!.Title.Should().Be("Already Deleted");

            Run? retrieved = await context.FindAsync<Run>(run.Id);
            retrieved!.UpdatedAt.Should().BeNull();
        }
    }
}
