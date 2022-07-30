using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Bans
{
	private static string? s_AdminJwt;
	private static TestApiClient s_ApiClient = null!;
	private static Leaderboard s_DefaultLeaderboard = null!;
	private static TestApiFactory s_Factory = null!;
	private static string? s_ModJwt;
	private static User s_ModUser = null!;
	private static User s_NormalUser = null!;

	[SetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();
		s_AdminJwt = (await s_ApiClient.LoginAdminUser()).Token;

		// Set up users and leaderboard
		s_NormalUser = await s_ApiClient.RegisterUser(
			"normal",
			"normal@email.com",
			"Passw0rd!"
		);

		s_ModUser = await s_ApiClient.RegisterUser(
			"mod",
			"mod@email.com",
			"Passw0rd!"
		);

		s_DefaultLeaderboard = await s_ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
				},
				Jwt = s_AdminJwt,
			}
		);

		await s_ApiClient.Post<Modship>(
			"/api/modships",
			new()
			{
				Body = new CreateModshipRequest
				{
					LeaderboardId = s_DefaultLeaderboard.Id,
					UserId = s_ModUser.Id,
				},
				Jwt = s_AdminJwt,
			}

		);

		s_ModJwt = (await s_ApiClient.LoginUser("mod@email.com", "Passw0rd!")).Token;
	}

	[Test]
	public static async Task CreateSiteBan_Ok()
	{
		Ban created = await CreateSiteBan(s_NormalUser.Id, "reason");
		Ban retrieved = await GetBan(created.Id);

		Assert.IsNotNull(created);
		Assert.AreEqual(retrieved.Id, created.Id);
		Assert.AreEqual(retrieved.BannedUserId, created.BannedUserId);
		Assert.AreEqual(retrieved.BanningUserId, created.BanningUserId);
		Assert.IsNull(retrieved.LeaderboardId);
	}

	[Test]
	public static async Task CreateSiteBan_TryBanAdmin_Unauthorized()
	{
		try
		{
			await CreateSiteBan(TestInitCommonFields.Admin.Id, "reason");
			Assert.Fail("You should not be able to ban an admin (yourself).");
		}
		catch (RequestFailureException e)
		{
			Assert.AreEqual(HttpStatusCode.Forbidden, e.Response.StatusCode);
		}
	}

	[Test]
	public static async Task CreateLeaderboardBan_Ok()
	{
		Ban created = await CreateLeaderboardBan(s_NormalUser.Id, "reason");
		Ban retrieved = await GetBan(created.Id);

		Assert.IsNotNull(created);
		Assert.AreEqual(retrieved.Id, created.Id);
		Assert.AreEqual(retrieved.BannedUserId, created.BannedUserId);
		Assert.AreEqual(retrieved.BanningUserId, created.BanningUserId);
		Assert.AreEqual(retrieved.LeaderboardId, created.LeaderboardId);
	}

	[Test]
	public static async Task CreateLeaderboardBan_TryMod_Unauthorized()
	{
		try
		{
			await CreateLeaderboardBan(s_ModUser.Id, "reason");
			Assert.Fail("You should not be able to ban a mod (yourself).");
		}
		catch (RequestFailureException e)
		{
			Assert.AreEqual(HttpStatusCode.Forbidden, e.Response.StatusCode);
		}
	}

	[Test]
	public static async Task CreateSiteBan_DeleteBan_Ok()
	{
		Ban created = await CreateSiteBan(s_NormalUser.Id, "weenie was a mega meanie");
		HttpResponseMessage response = await DeleteBan(created.Id);
		Ban retrieved = await GetBan(created.Id);
		Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
		Assert.NotNull(retrieved.DeletedAt);
	}

	[Test]
	public static async Task CreateLeaderboardBan_DeleteBan_Ok()
	{
		Ban created = await CreateLeaderboardBan(s_NormalUser.Id, "weenie was a mega meanie");
		Assert.NotNull(created.LeaderboardId);
		HttpResponseMessage response = await DeleteLeaderboardBan(created.Id, (long)created.LeaderboardId!);
		Ban retrieved = await GetBan(created.Id);
		Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
		Assert.NotNull(retrieved.DeletedAt);
	}

	private static async Task<User> CreateUser(string username, string email, string password)
	{
		return await s_ApiClient.Post<User>(
			"/api/users/register",
			new()
			{
				Body = new RegisterRequest()
				{
					Username = username,
					Email = email,
					Password = password,
					PasswordConfirm = password
				},
				Jwt = s_AdminJwt
			}
		);
	}

	private static async Task<Ban> CreateSiteBan(Guid userId, string reason)
	{
		return await s_ApiClient.Post<Ban>(
			"api/bans",
			new()
			{
				Body = new CreateSiteBanRequest()
				{
					UserId = userId,
					Reason = reason
				},
				Jwt = s_AdminJwt
			}
		);
	}

	private static async Task<Ban> CreateLeaderboardBan(Guid userId, string reason)
	{
		return await s_ApiClient.Post<Ban>(
			"api/bans/leaderboard",
			new()
			{
				Body = new CreateLeaderboardBanRequest()
				{
					UserId = userId,
					Reason = reason,
					LeaderboardId = s_DefaultLeaderboard.Id
				},
				Jwt = s_ModJwt
			}
		);
	}

	private static async Task<Ban> GetBan(long id)
	{
		return await s_ApiClient.Get<Ban>(
			$"api/bans/{id}",
			new()
			{
				Jwt = s_AdminJwt
			}
		);
	}

	private static async Task<HttpResponseMessage> DeleteBan(long id)
	{
		return await s_ApiClient.Delete(
			$"api/bans/{id}",
			new()
			{
				Jwt = s_AdminJwt
			}
		);
	}

	private static async Task<HttpResponseMessage> DeleteLeaderboardBan(long id, long leaderboardId)
	{
		return await s_ApiClient.Delete(
			$"api/bans/{id}/leaderboards/{leaderboardId}",
			new()
			{
				Jwt = s_AdminJwt
			}
		);
	}
}
