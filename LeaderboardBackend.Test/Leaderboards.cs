using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
{
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;
	private static string? Jwt;

	[SetUp]
	public static async Task SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();
		Jwt = (await ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static void GetLeaderboard_NoLeaderboards()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await ApiClient.Get<Leaderboard>(
				$"/api/leaderboards/2",
				new()
			)
		)!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task CreateLeaderboard_GetLeaderboard()
	{
		Leaderboard createdLeaderboard = await ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
				},
				Jwt = Jwt,
			}
		);

		Leaderboard retrievedLeaderboard = await ApiClient.Get<Leaderboard>(
			$"/api/leaderboards/{createdLeaderboard?.Id}",
			new()
		);

		Assert.AreEqual(createdLeaderboard, retrievedLeaderboard);
	}

	[Test]
	public static async Task CreateLeaderboards_GetLeaderboards()
	{
		HashSet<Leaderboard> createdLeaderboards = new();
		for (int i = 0; i < 5; i++)
		{
			createdLeaderboards.Add(
				await ApiClient.Post<Leaderboard>(
					"/api/leaderboards",
					new()
					{
						Body = new CreateLeaderboardRequest
						{
							Name = Generators.GenerateRandomString(),
							Slug = Generators.GenerateRandomString(),
						},
						Jwt = Jwt,
					}
				)
			);
		}

		IEnumerable<long> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
		string leaderboardIdQuery = ListToQueryString(leaderboardIds, "ids");
		List<Leaderboard> leaderboards = await ApiClient.Get<List<Leaderboard>>(
			$"api/leaderboards?{leaderboardIdQuery}",
			new()
		);
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
