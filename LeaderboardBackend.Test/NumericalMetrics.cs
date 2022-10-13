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

namespace LeaderboardBackend.Test;

[TestFixture]
internal class NumericalMetrics
{
	private static TestApiClient s_ApiClient = null!;
	private static TestApiFactory s_Factory = null!;
	private static string s_Jwt = null!;

	[OneTimeSetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();
		s_Jwt = (await s_ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static async Task CreateNumericalMetric_GetNumericalMetric_OK()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard();
		Category createdCategory = await CreateCategory(createdLeaderboard);

		NumericalMetric createdMetric = await CreateNumericalMetric(
			"Score",
			null,
			null,
			new[] { createdCategory.Id },
			null
		);

		NumericalMetric retrievedMetric = await GetNumericalMetric(createdMetric.Id);

		Assert.AreEqual(createdMetric, retrievedMetric);
	}

	private static async Task<Leaderboard> CreateLeaderboard()
	{
		return await s_ApiClient.Post<Leaderboard>(
					"/api/leaderboards",
					new()
					{
						Body = new CreateLeaderboardRequest()
						{
							Name = Generators.GenerateRandomString(),
							Slug = Generators.GenerateRandomString()
						},
						Jwt = s_Jwt
					});
	}

	private static async Task<Category> CreateCategory(Leaderboard createdLeaderboard)
	{
		return await s_ApiClient.Post<Category>(
					"/api/categories",
					new()
					{
						Body = new CreateCategoryRequest()
						{
							Name = Generators.GenerateRandomString(),
							Slug = Generators.GenerateRandomString(),
							LeaderboardId = createdLeaderboard.Id
						},
						Jwt = s_Jwt
					});
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
							Status = RunStatus.Created
						},
						Jwt = s_Jwt
					});
	}

	private static async Task<NumericalMetric> CreateNumericalMetric(
		string name,
		long? min,
		long? max,
		long[] categoryIds,
		Guid[]? runIds
	)
	{
		CreateNumericalMetricRequest request = new()
		{
			Name = name,
			CategoryIds = categoryIds,
		};

		if (min is not null)
		{
			request.Min = (long)min;
		}

		if (max is not null)
		{
			request.Max = (long)max;
		}

		if (runIds is not null)
		{
			request.RunIds = (Guid[])runIds;
		}

		return await s_ApiClient.Post<NumericalMetric>(
			"/api/numericalmetrics",
			new()
			{
				Body = request,
				Jwt = s_Jwt
			});
	}

	private static async Task<NumericalMetric> GetNumericalMetric(long id)
	{
		return await s_ApiClient.Get<NumericalMetric>(
					$"/api/numericalmetrics/{id}",
					new()
					{
						Jwt = s_Jwt,
					}
				);
	}
}