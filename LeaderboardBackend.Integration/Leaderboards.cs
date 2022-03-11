using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Leaderboards;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Integration;

[TestFixture]
internal class Leaderboards
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();	
	}

	[Test]
	public static async Task GetLeaderboard_NoLeaderboards()
	{
		ulong id = 1;
		HttpResponseMessage response = await ApiClient.GetAsync($"/api/leaderboards/{id}");
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task CreateLeaderboard_GetLeaderboard()
	{
		CreateLeaderboardRequest createBody = new() 
		{
			Name = Generators.GenerateRandomString(),
			Slug = Generators.GenerateRandomString(),
		};
		HttpResponseMessage createResponse = await ApiClient.PostAsJsonAsync("/api/leaderboards", createBody, JsonSerializerOptions);
		createResponse.EnsureSuccessStatusCode();
		Leaderboard createdLeaderboard = await HttpHelpers.ReadFromResponseBody<Leaderboard>(createResponse, JsonSerializerOptions);

		HttpResponseMessage getResponse = await ApiClient.GetAsync($"/api/leaderboards/{createdLeaderboard?.Id}");
		getResponse.EnsureSuccessStatusCode();
		Leaderboard retrievedLeaderboard = await HttpHelpers.ReadFromResponseBody<Leaderboard>(getResponse, JsonSerializerOptions);

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
			HttpResponseMessage createResponse = await ApiClient.PostAsJsonAsync("/api/leaderboards", createBody, JsonSerializerOptions);
			createResponse.EnsureSuccessStatusCode();
			createdLeaderboards.Add(await HttpHelpers.ReadFromResponseBody<Leaderboard>(createResponse, JsonSerializerOptions));
		}

		IEnumerable<ulong> leaderboardIds = createdLeaderboards.Select(l => l.Id).ToList();
		string leaderboardIdQuery = HttpHelpers.ListToQueryString(leaderboardIds, "ids");
		HttpResponseMessage getResponse = await ApiClient.GetAsync($"api/leaderboards?{leaderboardIdQuery}");
		List<Leaderboard> leaderboards = await HttpHelpers.ReadFromResponseBody<List<Leaderboard>>(getResponse, JsonSerializerOptions);
		foreach(var leaderboard in leaderboards)
		{
			Assert.IsTrue(createdLeaderboards.Contains(leaderboard));
			createdLeaderboards.Remove(leaderboard);
		}
		Assert.AreEqual(0, createdLeaderboards.Count);
	}
}
