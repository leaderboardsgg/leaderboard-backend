using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
{
	private static TestApiClient s_ApiClient = null!;
	private static TestApiFactory s_Factory = null!;
	private static string? s_Jwt;

	[SetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();
		s_Jwt = (await s_ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static void GetLeaderboard_NotFound()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_ApiClient.Get<Leaderboard>($"/api/leaderboards/2", new()))!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task CreateLeaderboard_GetLeaderboard_OK()
	{
		Leaderboard createdLeaderboard = await s_ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString()
				},
				Jwt = s_Jwt
			});

		Leaderboard retrievedLeaderboard = await s_ApiClient.Get<Leaderboard>(
			$"/api/leaderboards/{createdLeaderboard?.Id}",
			new());

		Assert.AreEqual(createdLeaderboard, retrievedLeaderboard);
	}

	[Test]
	public static async Task CreateLeaderboards_GetLeaderboards()
	{
		HashSet<Leaderboard> createdLeaderboards = new();

		for (int i = 0; i < 5; i++)
		{
			createdLeaderboards.Add(
				await s_ApiClient.Post<Leaderboard>(
					"/api/leaderboards",
					new()
					{
						Body = new CreateLeaderboardRequest
						{
							Name = Generators.GenerateRandomString(),
							Slug = Generators.GenerateRandomString()
						},
						Jwt = s_Jwt
					}));
		}

		IEnumerable<long> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
		string leaderboardIdQuery = ListToQueryString(leaderboardIds, "ids");

		List<Leaderboard> leaderboards = await s_ApiClient.Get<List<Leaderboard>>(
			$"api/leaderboards?{leaderboardIdQuery}",
			new());

		foreach (Leaderboard leaderboard in leaderboards)
		{
			Assert.IsTrue(createdLeaderboards.Contains(leaderboard));
			createdLeaderboards.Remove(leaderboard);
		}

		Assert.AreEqual(0, createdLeaderboards.Count);
	}

	private static string ListToQueryString<T>(IEnumerable<T> list, string key)
	{
		IEnumerable<string> queryList = list.Select(l => $"{key}={l}");
		return string.Join("&", queryList);
	}
}
