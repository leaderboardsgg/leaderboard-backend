using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
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

			Leaderboard createdLeaderboard = await s_apiClient.Post<Leaderboard>(
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

			Category createdCategory = await s_apiClient.Post<Category>(
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
			Run created = await CreateRun();

			Run retrieved = await GetRun(created.Id);

			Assert.NotNull(created);
			Assert.AreEqual(created.Id, retrieved.Id);
		}

		[Test]
		public static async Task GetParticipation_OK()
		{
			Run createdRun = await CreateRun();

			Participation createdParticipation = await s_apiClient.Post<Participation>(
				"api/participations",
				new()
				{
					Body = new CreateParticipationRequest
					{
						Comment = "comment",
						Vod = "vod",
						RunId = createdRun.Id,
						RunnerId = TestInitCommonFields.Admin.Id
					},
					Jwt = s_jwt
				});

			List<Participation> retrieved = await s_apiClient.Get<List<Participation>>(
				$"api/runs/{createdRun.Id}/participations",
				new() { Jwt = s_jwt });

			Assert.NotNull(retrieved);
			Assert.AreEqual(createdParticipation.Id, retrieved[0].Id);
		}

		[Test]
		public static async Task GetCategory_OK()
		{
			Run createdRun = await CreateRun();

			Category category = await s_apiClient.Get<Category>(
				$"api/runs/{createdRun.Id}/category",
				new()
				{
					Jwt = s_jwt
				}
			);

			Assert.NotNull(category);
			Assert.AreEqual(category.Id, s_categoryId);
		}

		private static async Task<Run> CreateRun()
		{
			return await s_apiClient.Post<Run>(
				"/api/runs",
				new()
				{
					Body = new CreateRunRequest
					{
						PlayedOn = LocalDate.MinIsoValue,
						SubmittedAt = Instant.MaxValue,
						Status = RunStatus.Created,
						CategoryId = s_categoryId
					},
					Jwt = s_jwt
				});
		}

		private static async Task<Run> GetRun(Guid id)
		{
			return await s_apiClient.Get<Run>($"/api/runs/{id}", new() { Jwt = s_jwt });
		}
	}
}
