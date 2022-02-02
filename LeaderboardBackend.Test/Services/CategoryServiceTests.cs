using NUnit.Framework;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LeaderboardBackend.Test.Services;

public class CategoryServiceTests
{
	private static CategoryService _service = null!;

	[SetUp]
	public void Setup()
	{
		_service = new CategoryService(
			new ApplicationContext(
				new DbContextOptionsBuilder<ApplicationContext>().UseNpgsql(
					"Host=localhost;Port=5433;Database=leaderboardstest;Username=admin;Password=example;"
				).Options
			)
		);
	}

	[Test]
	public async Task GetCategory_GetsAnExistingCategory()
	{
		var result = await _service.GetCategory(1);

		Assert.NotNull(result);
		Assert.AreEqual(1, result?.Id);
	}

	[Test]
	public async Task GetCategory_ReturnsNullForNonExistingID()
	{
		var result = await _service.GetCategory(0);

		Assert.Null(result);
	}
}
