using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Leaderboards;
using LeaderboardBackend.Models.Requests.Users;
using LeaderboardBackend.Test.Lib;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Leaderboards
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static string Token = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
		Token = LogInAdmin().Result.Token;
	}

	public static void TearDown()
	{
		ApiClient.Dispose();
		Factory.Dispose();
	}

	[Test]
	public static async Task GetLeaderboard_NoLeaderboards()
	{
		ulong id = 10;
		HttpResponseMessage response = await ApiClient.GetAsync($"/api/leaderboards/{id}");
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task CreateLeaderboard_GetLeaderboard()
	{
		CreateLeaderboardRequest body = new()
		{
			Name = Generators.GenerateRandomString(),
			Slug = Generators.GenerateRandomString(),
		};

		Leaderboard createdLeaderboard = await HttpHelpers.Post<Leaderboard>(
			ApiClient,
			"/api/leaderboards",
			new()
			{
				Body = body,
				Jwt = Token,
			},
			JsonSerializerOptions
		);

		Leaderboard retrievedLeaderboard = await HttpHelpers.Get<Leaderboard>(
			ApiClient,
			$"/api/leaderboards/{createdLeaderboard?.Id}",
			new(),
			JsonSerializerOptions
		);

		Assert.AreEqual(createdLeaderboard, retrievedLeaderboard);
	}

	[Test]
	public static async Task CreateLeaderboards_GetLeaderboards()
	{
		HashSet<Leaderboard> createdLeaderboards = new();
		for (int i = 0; i < 5; i++)
		{
			CreateLeaderboardRequest createBody = new()
			{
				Name = Generators.GenerateRandomString(),
				Slug = Generators.GenerateRandomString(),
			};
			createdLeaderboards.Add(
				await HttpHelpers.Post<Leaderboard>(
					ApiClient,
					"/api/leaderboards",
					new()
					{
						Body = createBody,
						Jwt = Token,
					},
					JsonSerializerOptions
				)
			);
		}

		IEnumerable<long> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
		string leaderboardIdQuery = ListToQueryString(leaderboardIds, "ids");
		HttpResponseMessage getResponse = await ApiClient.GetAsync($"api/leaderboards?{leaderboardIdQuery}");
		List<Leaderboard> leaderboards = await HttpHelpers.ReadFromResponseBody<List<Leaderboard>>(getResponse, JsonSerializerOptions);
		foreach (var leaderboard in leaderboards)
		{
			Assert.IsTrue(createdLeaderboards.Contains(leaderboard));
			createdLeaderboards.Remove(leaderboard);
		}
		Assert.AreEqual(0, createdLeaderboards.Count);
	}

	private static async Task<LoginResponse> LogInAdmin() =>
		await UserHelpers.Login(ApiClient, Factory.GetAdmin().Email, Factory.GetAdmin().Password, JsonSerializerOptions);

	private static string ListToQueryString<T>(IEnumerable<T> list, string key)
	{
		IEnumerable<string> queryList = list.Select(l => $"{key}={l}");
		return string.Join("&", queryList);
	}
}
