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
		private static TestApiClient s_ApiClient = null!;
		private static TestApiFactory s_Factory = null!;
		private static string s_Jwt = null!;
		private static long s_CategoryId;

		[SetUp]
		public static async Task SetUp()
		{
			s_Factory = new TestApiFactory();
			s_ApiClient = s_Factory.CreateTestApiClient();
			s_Jwt = (await s_ApiClient.LoginAdminUser()).Token;

			Leaderboard createdLeaderboard = await s_ApiClient.Post<Leaderboard>(
				"/api/leaderboards",
				new()
				{
					Body = new CreateLeaderboardRequest()
					{
						Name = Generators.GenerateRandomString(),
						Slug = Generators.GenerateRandomString(),
					},
					Jwt = s_Jwt,
				}
			);

			Category createdCategory = await s_ApiClient.Post<Category>(
				"/api/categories",
				new()
				{
					Body = new CreateCategoryRequest()
					{
						Name = Generators.GenerateRandomString(),
						Slug = Generators.GenerateRandomString(),
						LeaderboardId = createdLeaderboard.Id,
					},
					Jwt = s_Jwt,
				}
			);

			s_CategoryId = createdCategory.Id;
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

			Participation createdParticipation = await s_ApiClient.Post<Participation>(
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
					Jwt = s_Jwt
				});

			List<Participation> retrieved = await s_ApiClient.Get<List<Participation>>(
				$"api/runs/{createdRun.Id}/participations",
				new() { Jwt = s_Jwt });

			Assert.NotNull(retrieved);
			Assert.AreEqual(createdParticipation.Id, retrieved[0].Id);
		}

		[Test]
		public static async Task GetCategory_OK()
		{
			Run createdRun = await CreateRun();

			Category category = await s_ApiClient.Get<Category>(
				$"api/runs/{createdRun.Id}/category",
				new()
				{
					Jwt = s_Jwt
				}
			);

			Assert.NotNull(category);
			Assert.AreEqual(category.Id, s_CategoryId);
		}

		private static async Task<Run> CreateRun()
		{
			return await s_ApiClient.Post<Run>(
				"/api/runs",
				new()
				{
					Body = new CreateRunRequest
					{
						PlayedOn = LocalDate.MinIsoValue,
						SubmittedAt = Instant.MaxValue,
						Status = RunStatus.Created,
						CategoryId = s_CategoryId
					},
					Jwt = s_Jwt
				});
		}

		private static async Task<Run> GetRun(Guid id)
		{
			return await s_ApiClient.Get<Run>($"/api/runs/{id}", new() { Jwt = s_Jwt });
		}
	}
}
