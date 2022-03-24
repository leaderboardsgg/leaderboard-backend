
using NUnit.Framework;
using Moq;
using LeaderboardBackend.Controllers;
using LeaderboardBackend.Services;
using LeaderboardBackend.Models.Entities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

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

		ActionResult<Category> response = await _controller.GetCategory((long)1);

		NotFoundResult? actual = response.Result as NotFoundResult;
		Assert.NotNull(actual);
		Assert.AreEqual(404, actual!.StatusCode);
	}

	[Test]
	public async Task GetCategory_Ok_CategoryExists()
	{
		_categoryServiceMock
			.Setup(x => x.GetCategory(It.IsAny<long>()))
			.Returns(Task.FromResult<Category?>(new Category { Id = 1 }));

		ActionResult<Category> response = await _controller.GetCategory(1);
		Category? category = Helpers.GetValueFromObjectResult<OkObjectResult, Category>(response.Result);

		Assert.NotNull(category);
		Assert.AreEqual(1, category!.Id);
	}
}
