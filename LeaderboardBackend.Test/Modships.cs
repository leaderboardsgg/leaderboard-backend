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
	private static TestApiClient s_ApiClient = null!;
	private static TestApiFactory s_Factory = null!;
	private static string s_Jwt = null!;

	[SetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();
		s_Jwt = (await s_ApiClient.LoginAdminUser()).Token;
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

		try
		{
			// Confirm that modship is deleted -> 404 NotFound -> Api Client throws RequestFailureException
			await GetModship();
			Assert.Fail("GetModship should have failed, because the Modship should not exist anymore");
		}
		catch (RequestFailureException e)
		{
			Assert.AreEqual(created, retrieved);
			Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
		}
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
		return await s_ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = "Mario Goes to Jail II",
					Slug = "mario-goes-to-jail-ii"
				},
				Jwt = s_Jwt
			});
	}

	private static async Task<Modship> GetModship()
	{
		return await s_ApiClient.Get<Modship>(
			$"/api/modships/{TestInitCommonFields.Admin.Id}",
			new()
			{
				Jwt = s_Jwt
			});
	}

	private static async Task<Modship> CreateModship(long leaderboardId)
	{
		return await s_ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id,
				},
				Jwt = s_Jwt
			});
	}

	private static async Task<HttpResponseMessage> DeleteModship(long leaderboardId)
	{
		return await s_ApiClient.Delete(
			"/api/modships",
			new()
			{
				Body = new RemoveModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id,
				},
				Jwt = s_Jwt
			});
	}
}
