using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
	private readonly ICategoryService _categoryService;

	public CategoriesController(
		ICategoryService categoryService
	)
	{
		_categoryService = categoryService;
	}

	/// <summary>Gets a Category from its ID.</summary>
	/// <response code="200">The Category with the provided ID.</response>
	/// <response code="404">If no Category can be found.</response>
	[HttpGet("{id}")]
	public async Task<ActionResult<Category>> GetCategory(ulong id)
	{
		Category? category = await _categoryService.GetCategory(id);
		if (category == null)
		{
			return NotFound();
		}

		return category;
	}
}
