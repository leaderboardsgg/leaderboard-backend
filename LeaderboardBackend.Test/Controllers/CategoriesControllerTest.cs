
using NUnit.Framework;
using Moq;
using LeaderboardBackend.Controllers;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Test.Helpers;

namespace LeaderboardBackend.Test.Controllers;

public class CategoryTests
{
	private Mock<ICategoryService> _categoryServiceMock = null!;

	private CategoriesController _controller = null!;

	[SetUp]
	public void Setup()
	{
		_categoryServiceMock = new Mock<ICategoryService>();

		_controller = new CategoriesController(
			_categoryServiceMock.Object
		);
	}

	[Test]
	public async Task GetCategory_NotFound_CategoryDoesNotExist()
	{
		_categoryServiceMock
			.Setup(x => x.GetCategory(It.IsAny<long>()))
			.Returns(Task.FromResult<Category?>(null));

		ActionResult<Category> response = await _controller.GetCategory(1);

		ObjectResultHelpers.AssertResponseNotFound(response);
	}

	[Test]
	public async Task GetCategory_Ok_CategoryExists()
	{
		_categoryServiceMock
			.Setup(x => x.GetCategory(It.IsAny<long>()))
			.Returns(Task.FromResult<Category?>(new Category { Id = 1 }));

		ActionResult<Category> response = await _controller.GetCategory(1);

		Category? category = ObjectResultHelpers.GetValueFromObjectResult<Category, OkObjectResult>(response);
		Assert.NotNull(category);
		Assert.AreEqual(1, category!.Id);
	}
}
