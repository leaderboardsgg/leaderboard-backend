using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Modships
{
	private static TestApiClient s_apiClient = null!;
	private static TestApiFactory s_factory = null!;
	private static string s_jwt = null!;

	[SetUp]
	public static async Task SetUp()
	{
		s_factory = new TestApiFactory();
		s_apiClient = s_factory.CreateTestApiClient();
		s_jwt = (await s_apiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static async Task CreateModship_OK()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard();

		// Make user a mod
		Modship created = await CreateModship(createdLeaderboard.Id);

		// Confirm that user is a mod
		Modship retrieved = await GetModship();

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	[Test]
	public static async Task DeleteModship_OK()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard();

		// Make user a mod
		Modship created = await CreateModship(createdLeaderboard.Id);

		// Confirm that user is a mod
		Modship retrieved = await GetModship();

		// Remove the modship
		HttpResponseMessage response = await DeleteModship(createdLeaderboard.Id);
		Assert.IsTrue(response.IsSuccessStatusCode);

		Modship deleted = await GetModship();
		Assert.AreEqual(retrieved.Id, deleted.Id);
		Assert.NotNull(deleted.DeletedAt);
	}

	[Test]
	public static async Task DeleteModship_NotFound()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard();

		RequestFailureException? e = Assert.ThrowsAsync<RequestFailureException>(async () => await DeleteModship(createdLeaderboard.Id));

		Assert.NotNull(e);
		Assert.AreEqual(HttpStatusCode.NotFound, e!.Response.StatusCode);
	}

	private static async Task<Leaderboard> CreateLeaderboard()
	{
		return await s_apiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = "Mario Goes to Jail II",
					Slug = "mario-goes-to-jail-ii"
				},
				Jwt = s_jwt
			});
	}

	private static async Task<Modship> GetModship()
	{
		return await s_apiClient.Get<Modship>(
			$"/api/modships/{TestInitCommonFields.Admin.Id}",
			new() { Jwt = s_jwt });
	}

	private static async Task<Modship> CreateModship(long leaderboardId)
	{
		return await s_apiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id
				},
				Jwt = s_jwt
			});
	}

	private static async Task<HttpResponseMessage> DeleteModship(long leaderboardId)
	{
		return await s_apiClient.Delete(
			"/api/modships",
			new()
			{
				Body = new RemoveModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id
				},
				Jwt = s_jwt
			});
	}
}
