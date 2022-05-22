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
	private static TestApiFactory Factory = null!;
	private static TestApiClient ApiClient = null!;
	private static string? Jwt;

	[OneTimeSetUp]
	public static async Task SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateTestApiClient();
		Jwt = (await ApiClient.LoginAdminUser()).Token;
	}

	[Test]
	public static void GetCategory_Unauthorized()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await ApiClient.Get<Category>(
				$"/api/categories/1",
				new()
			)
		)!;

		Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
	}

	[Test]
	public static void GetCategory_NotFound()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await ApiClient.Get<Category>(
				$"/api/categories/69",
				new()
				{
					Jwt = Jwt,
				}
			)
		)!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task CreateCategory_GetCategory_OK()
	{
		Leaderboard createdLeaderboard = await ApiClient.Post<Leaderboard>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
				},
				Jwt = Jwt,
			}
		);

		Category createdCategory = await ApiClient.Post<Category>(
			"/api/categories",
			new()
			{
				Body = new CreateCategoryRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
					LeaderboardId = createdLeaderboard.Id,
				},
				Jwt = Jwt,
			}
		);
		Assert.AreEqual(1, createdCategory.PlayersMax);

		Category retrievedCategory = await ApiClient.Get<Category>(
			$"/api/categories/{createdCategory?.Id}",
			new()
			{
				Jwt = Jwt,
			}
		);
		Assert.AreEqual(createdCategory, retrievedCategory);
	}
}
