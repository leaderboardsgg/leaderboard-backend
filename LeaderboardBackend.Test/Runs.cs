using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test
{
	[TestFixture]
	internal class Runs
	{
		private static TestApiFactory Factory = null!;
		private static TestApiClient ApiClient = null!;
		private static string Jwt = null!;

		[SetUp]
		public static async Task SetUp()
		{
			Factory = new TestApiFactory();
			ApiClient = Factory.CreateTestApiClient();
			Jwt = (await ApiClient.LoginAdminUser()).Token;
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

			Participation createdParticipation = await ApiClient.Post<Participation>(
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
					Jwt = Jwt
				}
			);

			List<Participation> retrieved = await ApiClient.Get<List<Participation>>(
				$"api/runs/participations/{createdRun.Id}",
				new()
				{
					Jwt = Jwt
				}
			);

			Assert.NotNull(retrieved);
			Assert.AreEqual(createdParticipation.Id, retrieved[0].Id);
		}

		private static async Task<Run> CreateRun()
		{
			return await ApiClient.Post<Run>(
				"/api/runs",
				new()
				{
					Body = new CreateRunRequest
					{
						Played = DateTime.MinValue,
						Submitted = DateTime.MaxValue,
						Status = RunStatus.CREATED
					},
					Jwt = Jwt
				}
			);
		}

		private static async Task<Run> GetRun(Guid id)
		{
			return await ApiClient.Get<Run>(
				$"/api/runs/{id}",
				new()
				{
					Jwt = Jwt
				}
			);
		}

	}
}
