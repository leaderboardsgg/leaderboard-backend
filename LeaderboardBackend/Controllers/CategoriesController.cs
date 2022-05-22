using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
	private readonly ICategoryService CategoryService;

	public CategoriesController(
		ICategoryService categoryService
	)
	{
		CategoryService = categoryService;
	}

	/// <summary>Gets a Category from its ID.</summary>
	/// <response code="200">The Category with the provided ID.</response>
	/// <response code="404">If no Category can be found.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Category>> GetCategory(long id)
	{
		Category? category = await CategoryService.GetCategory(id);
		if (category == null)
		{
			return NotFound();
		}

		return Ok(category);
	}

	// FIXME: Allow only mods to call this
	/// <summary>Creates a new Category. Mod-only.</summary>
	/// <param name="body">A CreateCategoryRequest instance.</param>
	/// <response code="201">The created Category.</response>
	/// <response code="400">If the request is malformed.</response>
	/// <response code="404">If a non-mod calls this.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	[HttpPost]
	public async Task<ActionResult<Category>> CreateCategory([FromBody] CreateCategoryRequest body)
	{
		Category category = new()
		{
			Name = body.Name,
			Slug = body.Slug,
			Rules = body.Rules,
			PlayersMin = body.PlayersMin ?? 1,
			PlayersMax = body.PlayersMax ?? body.PlayersMin ?? 1,
			LeaderboardId = body.LeaderboardId,
		};

		await CategoryService.CreateCategory(category);
		return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
	}
}
