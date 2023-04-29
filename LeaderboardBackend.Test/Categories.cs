using System.Net;
using System.Threading.Tasks;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Test.Lib;
using LeaderboardBackend.Test.TestApi;
using LeaderboardBackend.Test.TestApi.Extensions;
using NUnit.Framework;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Categories
{
	private static TestApiClient s_apiClient = null!;
	private static TestApiFactory s_factory = null!;
	private static string? s_jwt;

	[OneTimeSetUp]
	public async Task OneTimeSetUp()
	{
		s_factory = new TestApiFactory();
		s_apiClient = s_factory.CreateTestApiClient();

		s_factory.ResetDatabase();
		s_jwt = (await s_apiClient.LoginAdminUser()).Token;
	}

	[OneTimeTearDown]
	public void OneTimeTearDown()
	{
		s_factory.Dispose();
	}

	[Test]
	public static void GetCategory_Unauthorized()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_apiClient.Get<CategoryViewModel>($"/api/categories/1", new()))!;

		Assert.AreEqual(HttpStatusCode.Unauthorized, e.Response.StatusCode);
	}

	[Test]
	public static void GetCategory_NotFound()
	{
		RequestFailureException e = Assert.ThrowsAsync<RequestFailureException>(async () =>
			await s_apiClient.Get<CategoryViewModel>($"/api/categories/69", new() { Jwt = s_jwt }))!;

		Assert.AreEqual(HttpStatusCode.NotFound, e.Response.StatusCode);
	}

	[Test]
	public static async Task CreateCategory_GetCategory_OK()
	{
		LeaderboardViewModel createdLeaderboard = await s_apiClient.Post<LeaderboardViewModel>(
			"/api/leaderboards",
			new()
			{
				Body = new CreateLeaderboardRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString()
				},
				Jwt = s_jwt
			});

		CategoryViewModel createdCategory = await s_apiClient.Post<CategoryViewModel>(
			"/api/categories",
			new()
			{
				Body = new CreateCategoryRequest()
				{
					Name = Generators.GenerateRandomString(),
					Slug = Generators.GenerateRandomString(),
					LeaderboardId = createdLeaderboard.Id
				},
				Jwt = s_jwt
			});

		Assert.AreEqual(1, createdCategory.PlayersMax);

		CategoryViewModel retrievedCategory = await s_apiClient.Get<CategoryViewModel>(
			$"/api/categories/{createdCategory?.Id}",
			new() { Jwt = s_jwt });

		Assert.AreEqual(createdCategory, retrievedCategory);
	}
}
