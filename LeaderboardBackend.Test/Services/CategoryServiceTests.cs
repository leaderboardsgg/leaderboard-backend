using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Test.Helpers;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Services;

public class CategoryServiceTests
{
	private static CategoryService _service = null!;

	[SetUp]
	public void Setup()
	{
		_service = new CategoryService(ApplicationContextFactory.CreateNewContext());
	}

	[Test]
	public async Task GetCategory_GetsAnExistingCategory()
	{
		await _service.CreateCategory(new() {
			LeaderboardId = 1,
			Name = "Test",
			Slug = "test",
			PlayersMax = 1,
			PlayersMin = 1
		});

		var result = await _service.GetCategory(1);

		Assert.NotNull(result);
		Assert.AreEqual(1, result?.Id);
		Assert.AreEqual(1, result?.PlayersMax);
		Assert.AreEqual(1, result?.PlayersMin);
	}

	[Test]
	public async Task GetCategory_ReturnsNullForNonExistingID()
	{
		var result = await _service.GetCategory(0);

		Assert.Null(result);
	}
}
