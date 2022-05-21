using NUnit.Framework;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using System.Net.Http;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Modships
{
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;
	private static string Jwt = null!;

	[SetUp]
	public static async Task SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();
		Jwt = (await ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static async Task CreateModship_OK()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard(Jwt);

		// Make user a mod
		Modship created = await CreateModship(createdLeaderboard.Id, Jwt);

		// Confirm that user is a mod
		Modship retrieved = await GetModship(Jwt);

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	[Test]
	public static async Task DeleteModship_OK()
	{
		Leaderboard createdLeaderboard = await CreateLeaderboard(Jwt);

		// Make user a mod
		Modship created = await CreateModship(createdLeaderboard.Id, Jwt);

		// Confirm that user is a mod
		Modship retrieved = await GetModship(Jwt);

		// Remove the modship
		HttpResponseMessage response = await DeleteModship(createdLeaderboard.Id, Jwt);
		Assert.IsTrue(response.IsSuccessStatusCode);

		try
		{
			// Confirm that modship is deleted -> 404 NotFound -> Api Client throws RequestFailureException
			await GetModship(Jwt);
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
		Leaderboard createdLeaderboard = await CreateLeaderboard(Jwt);

		RequestFailureException? e = Assert.ThrowsAsync<RequestFailureException>(async () => await DeleteModship(createdLeaderboard.Id, Jwt));

		Assert.NotNull(e);
		Assert.AreEqual(HttpStatusCode.NotFound, e!.Response.StatusCode);
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

	private static async Task<Modship> GetModship(string jwt)
	{
		return await ApiClient.Get<Modship>(
			$"/api/modships/{TestInitCommonFields.Admin.Id}",
			new()
			{
				Jwt = jwt
			}
		);
	}

	private static async Task<Modship> CreateModship(long leaderboardId, string jwt)
	{
		return await ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id,
				},
				Jwt = jwt
			}
		);
	}

	private static async Task<HttpResponseMessage> DeleteModship(long leaderboardId, string jwt)
	{
		return await ApiClient.Delete(
			"/api/modships",
			new()
			{
				Body = new RemoveModshipRequest
				{
					LeaderboardId = leaderboardId,
					UserId = TestInitCommonFields.Admin.Id,
				},
				Jwt = jwt
			}
		);
	}
}
