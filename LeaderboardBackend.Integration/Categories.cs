using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Categories;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardBackend.Integration;

[TestFixture]
internal class Categories
{
	private static TestApiFactory Factory = null!;
	private static HttpClient ApiClient = null!;
	private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	[SetUp]
	public static void SetUp()
	{
		Factory = new TestApiFactory();
		ApiClient = Factory.CreateClient();
	}

	[Test]
	public static async Task GetCategory_NoCategories()
	{
		long id = 1;
		HttpResponseMessage response = await ApiClient.GetAsync($"/api/categories/{id}");
		Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Test]
	public static async Task CreateCategory_GetCategory()
	{
		CreateCategoryRequest createBody = new()
		{
			Name = Generators.GenerateRandomString(),
			Slug = Generators.GenerateRandomString(),
			LeaderboardId = 1
		};
		HttpResponseMessage createResponse = await ApiClient.PostAsJsonAsync("/api/categories", createBody, JsonSerializerOptions);
		createResponse.EnsureSuccessStatusCode();
		Category createdCategory = await HttpHelpers.ReadFromResponseBody<Category>(createResponse, JsonSerializerOptions);

		Assert.AreEqual(1, createdCategory.PlayersMax);

		HttpResponseMessage getResponse = await ApiClient.GetAsync($"/api/categories/{createdCategory?.Id}");
		getResponse.EnsureSuccessStatusCode();
		Category retrievedCategory = await HttpHelpers.ReadFromResponseBody<Category>(getResponse, JsonSerializerOptions);

		Assert.AreEqual(createdCategory, retrievedCategory);
	}
}
