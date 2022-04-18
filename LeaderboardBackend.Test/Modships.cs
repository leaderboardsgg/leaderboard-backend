using NUnit.Framework;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests.Leaderboards;
using LeaderboardBackend.Models.Requests.Modships;
using LeaderboardBackend.Models.Entities;
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

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
	}

	[Test]
	public static async Task MakeMod_Success()
	{
		User admin = Factory.GetAdmin();
		string jwt = await Login(admin);
		Leaderboard createdLeaderboard = await CreateLeaderboard(jwt);

		// Make user a mod
		CreateModshipRequest makeModBody = new()
		{
			LeaderboardId = createdLeaderboard.Id,
			UserId = admin.Id,
		};

		Modship created = await HttpHelpers.Post<Modship>(
			ApiClient,
			"/api/modships",
			new()
			{
				Body = makeModBody,
				Jwt = jwt
			},
			JsonSerializerOptions
		);

		Modship retrieved = await HttpHelpers.Get<Modship>(
			ApiClient,
			$"/api/modships/{admin.Id}",
			new()
			{
				Jwt = jwt
			},
			JsonSerializerOptions
		);

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	private static async Task<Leaderboard> CreateLeaderboard(string jwt)
	{
		return await HttpHelpers.Post<Leaderboard>(
			ApiClient,
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = "Mario Goes to Jail II",
					Slug = "mario-goes-to-jail-ii"
				},
				Jwt = jwt
			},
			JsonSerializerOptions
		);
	}

	private static async Task<string> Login(User user) =>
		(await UserHelpers.Login(ApiClient, user.Email, user.Password, JsonSerializerOptions)).Token;
}
