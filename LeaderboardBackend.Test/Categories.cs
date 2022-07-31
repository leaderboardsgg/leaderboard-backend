using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Categories
{
	private static TestApiClient s_ApiClient = null!;
	private static TestApiFactory s_Factory = null!;
	private static string? s_Jwt;

	[OneTimeSetUp]
	public static async Task SetUp()
	{
		s_Factory = new TestApiFactory();
		s_ApiClient = s_Factory.CreateTestApiClient();
		s_Jwt = (await s_ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static void GetCategory_Unauthorized()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_ApiClient.Get<Category>($"/api/categories/1", new()))!;

		Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
	}

	[Test]
	public static void GetCategory_NotFound()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_ApiClient.Get<Category>($"/api/categories/69", new() { Jwt = s_Jwt }))!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task CreateCategory_GetCategory_OK()
	{
		Leaderboard createdLeaderboard = await s_ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString()
				},
				Jwt = s_Jwt
			});

		Category createdCategory = await s_ApiClient.Post<Category>(
			"/api/categories",
			new()
			{
				Body = new CreateCategoryRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
					LeaderboardId = createdLeaderboard.Id
				},
				Jwt = s_Jwt
			});

		Assert.AreEqual(1, createdCategory.PlayersMax);

		Category retrievedCategory = await s_ApiClient.Get<Category>(
			$"/api/categories/{createdCategory?.Id}",
			new() { Jwt = s_Jwt });

		Assert.AreEqual(createdCategory, retrievedCategory);
	}
}
