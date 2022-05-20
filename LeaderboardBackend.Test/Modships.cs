using NUnit.Framework;
using System.Text.Json;
using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using System;

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
		Modship created = await CreateModship(createdLeaderboard.Id, jwt);

		// Confirm that user is a mod
		Modship retrieved = await GetModship(jwt);

		Assert.NotNull(created.User);
		Assert.AreEqual(created, retrieved);
	}

	[Test]
	public static async Task RemoveMod_Success()
	{
		string jwt = (await ApiClient.LoginAdminUser()).Token;

		Leaderboard createdLeaderboard = await CreateLeaderboard(jwt);

		// Make user a mod
		Modship created = await CreateModship(createdLeaderboard.Id, jwt);

		// Confirm that user is a mod
		Modship retrieved = await GetModship(jwt);

		// Remove the modship
		await RemoveModship(createdLeaderboard.Id, jwt);

		try
		{
			// Confirm that modship is deleted -> 404 NotFound -> Api Client throws RequestFailureException
			await GetModship(jwt);
			Assert.Fail("GetModship should have failed, because the Modship should not exist anymore");
		}
		catch (RequestFailureException e)
		{
			Assert.AreEqual(created, retrieved);
			Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
		}
	}

	[Test]
	public static async Task RemoveMod_Fail()
	{
		string jwt = (await ApiClient.LoginAdminUser()).Token;

		Leaderboard createdLeaderboard = await CreateLeaderboard(jwt);

		Assert.ThrowsAsync<RequestFailureException>(async () => await RemoveModship(createdLeaderboard.Id, jwt));
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

	private static async Task RemoveModship(long leaderboardId, string jwt)
	{
		await ApiClient.Delete<object>(
			"api/modships",
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
