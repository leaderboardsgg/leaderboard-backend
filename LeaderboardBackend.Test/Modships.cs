using NUnit.Framework;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests.Leaderboards;
using LeaderboardBackend.Models.Requests.Modships;
using LeaderboardBackend.Models.Requests.Users;
using LeaderboardBackend.Models.Entities;
using System.Net.Http.Json;
using LeaderboardBackend.Test.Lib;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Modships
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private static readonly string ValidUsername = "Test";
	private static readonly string ValidPassword = "c00l_pAssword";
	private static readonly string ValidEmail = "test@email.com";

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
	}

	[Test]
	public static async Task MakeMod_Success()
	{
		User createdUser = await CreateUser();
		Leaderboard createdLeaderboard = await CreateLeaderboard();

		// Make user a mod
		CreateModshipRequest makeModBody = new()
		{
			LeaderboardId = createdLeaderboard.Id,
			UserId = createdUser.Id,
		};

		HttpResponseMessage makeModResponse = await ApiClient.PostAsJsonAsync("/api/modships", makeModBody, JsonSerializerOptions);
		makeModResponse.EnsureSuccessStatusCode();
		Modship createdModship = await HttpHelpers.ReadFromResponseBody<Modship>(makeModResponse, JsonSerializerOptions);

		HttpResponseMessage getModshipResponse = await ApiClient.GetAsync($"/api/modships/{createdUser?.Id}");
		getModshipResponse.EnsureSuccessStatusCode();
		Modship retrievedModship = await HttpHelpers.ReadFromResponseBody<Modship>(getModshipResponse, JsonSerializerOptions);

		Assert.AreEqual(createdModship, retrievedModship);
	}

	private static async Task<User> CreateUser()
	{
		RegisterRequest registerBody = new()
		{
			Username = ValidUsername,
			Password = ValidPassword,
			PasswordConfirm = ValidPassword,
			Email = ValidEmail,
		};
		HttpResponseMessage registerResponse = await ApiClient.PostAsJsonAsync("/api/users/register", registerBody, JsonSerializerOptions);
		registerResponse.EnsureSuccessStatusCode();
		return await HttpHelpers.ReadFromResponseBody<User>(registerResponse, JsonSerializerOptions);
	}

	private static async Task<Leaderboard> CreateLeaderboard()
	{
		CreateLeaderboardRequest createLeaderboardBody = new()
		{
			Name = "Mario Goes to Jail II",
			Slug = "mario-goes-to-jail-ii"
		};

		HttpResponseMessage createLeaderboardResponse = await ApiClient.PostAsJsonAsync("/api/leaderboards", createLeaderboardBody, JsonSerializerOptions);
		createLeaderboardResponse.EnsureSuccessStatusCode();
		return await HttpHelpers.ReadFromResponseBody<Leaderboard>(createLeaderboardResponse, JsonSerializerOptions);
	}
}
