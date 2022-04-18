using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Categories;
using LeaderboardBackend.Models.Requests.Leaderboards;
using LeaderboardBackend.Test.Lib;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test;

[TestFixture]
internal class Categories
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};
	private static string? Jwt;

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
		Jwt = UserHelpers.Login(ApiClient, Factory.GetAdmin().Email, Factory.GetAdmin().Password, JsonSerializerOptions).Result.Token;
	}

	[Test]
	public static async Task GetCategory_NoCategories()
	{
		long id = 1;
		HttpRequestMessage request = new(HttpMethod.Get, $"/api/categories/{id}")
		{
			Headers =
			{
				Authorization = new(JwtBearerDefaults.AuthenticationScheme, Jwt)
			}
		};
		HttpResponseMessage response = await ApiClient.SendAsync(request);
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task CreateCategory_GetCategory()
	{
		CreateLeaderboardRequest createLeaderboardBody = new()
		{
			Name = Generators.GenerateRandomString(),
			Slug = Generators.GenerateRandomString(),
		};

		Leaderboard createdLeaderboard = await HttpHelpers.Post<Leaderboard>(
			ApiClient,
			"/api/leaderboards",
			new()
			{
				Body = createLeaderboardBody,
				Jwt = Jwt,
			},
			JsonSerializerOptions
		);

		CreateCategoryRequest createCategoryBody = new()
		{
			Name = Generators.GenerateRandomString(),
			Slug = Generators.GenerateRandomString(),
			LeaderboardId = createdLeaderboard.Id,
		};

		Category createdCategory = await HttpHelpers.Post<Category>(
			ApiClient,
			"/api/categories",
			new()
			{
				Body = createCategoryBody,
				Jwt = Jwt,
			},
			JsonSerializerOptions
		);

		Assert.AreEqual(1, createdCategory.PlayersMax);

		Category retrievedCategory = await HttpHelpers.Get<Category>(
			ApiClient,
			$"/api/categories/{createdCategory?.Id}",
			new()
			{
				Jwt = Jwt,
			},
			JsonSerializerOptions
		);

		Assert.AreEqual(createdCategory, retrievedCategory);
	}
}
