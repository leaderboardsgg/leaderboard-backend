using NUnit.Framework;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Modships
{
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();
	}

	[Test]
	public static async Task MakeMod_Success()
	{
		string jwt = (await ApiClient.LoginAdminUser()).Token;

		Leaderboard createdLeaderboard = await CreateLeaderboard(jwt);

		// Make user a mod
		Modship created = await ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = createdLeaderboard.Id,
					UserId = TestInitCommonFields.Admin.Id,
				},
				Jwt = jwt
			}
		);

		Modship retrieved = await ApiClient.Get<Modship>(
			$"/api/modships/{TestInitCommonFields.Admin.Id}",
			new()
			{
				Jwt = jwt
			}
		);

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	private static async Task<Leaderboard> CreateLeaderboard(string jwt)
	{
		return await ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = "Mario Goes to Jail II",
					Slug = "mario-goes-to-jail-ii"
				},
				Jwt = jwt
			}
		);
	}
}
