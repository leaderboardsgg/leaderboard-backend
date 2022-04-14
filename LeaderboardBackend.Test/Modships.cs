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
		// Note: Retrieved Modship doesn't retrieve relations. I.e. it's User (but not User ID) field is null
		// Also GET modship endpoint doesn't return arrays, although it should.
		User admin = Factory.GetAdmin();
		string jwt = await Login(admin);
		Leaderboard createdLeaderboard = await CreateLeaderboard(jwt);

		// Make user a mod
		CreateModshipRequest makeModBody = new()
		{
			LeaderboardId = createdLeaderboard.Id,
			UserId = admin.Id,
		};

		Modship created = await HttpHelpers.Post<CreateModshipRequest, Modship>(
			"/api/modships",
			makeModBody,
			ApiClient,
			JsonSerializerOptions,
			jwt
		);

		Modship retrieved = await HttpHelpers.Get<Modship>(
			$"/api/modships/{admin.Id}",
			ApiClient,
			JsonSerializerOptions,
			jwt
		);

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	private static async Task<Leaderboard> CreateLeaderboard(string jwt)
	{
		CreateLeaderboardRequest createLeaderboardBody = new()
		{
			Name = "Mario Goes to Jail II",
			Slug = "mario-goes-to-jail-ii"
		};

		return await HttpHelpers.Post<CreateLeaderboardRequest, Leaderboard>(
			"/api/leaderboards",
			createLeaderboardBody,
			ApiClient,
			JsonSerializerOptions,
			jwt
		);
	}

	private static async Task<string> Login(User user) =>
		(await UserHelpers.Login(ApiClient, user.Email, user.Password, JsonSerializerOptions)).Token;
}
