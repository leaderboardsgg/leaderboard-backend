using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
	private readonly ICategoryService _categoryService;

	public CategoriesController(ICategoryService categoryService)
	{
		_categoryService = categoryService;
	}

	/// <summary>
	///     Gets a Category by its ID.
	/// </summary>
	/// <param name="id">The ID of the `Category` which should be retrieved.</param>
	/// <response code="200">The `Category` was found and returned successfully.</response>
	/// <response code="404">No `Category` with the requested ID could be found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Category>> GetCategory(long id)
	{
		// NOTE: Should this use [AllowAnonymous]? - Ero

		try
		{
			Category category = await _categoryService.Get(id);
			return Ok(category);
		}
		catch (System.Exception)
		{
			return NotFound();
		}
	}

	/// <summary>
	///     Creates a new Category.
	///     This request is restricted to Moderators.
	/// </summary>
	/// <param name="request">
	///     The `CreateCategoryRequest` instance from which to create the `Category`.
	/// </param>
	/// <response code="201">The `Category` was created and returned successfully.</response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="404">
	///     The requesting `User` is unauthorized to create a `Category`.
	/// </response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[HttpPost]
	public async Task<ActionResult<Category>> CreateCategory(
		[FromBody] CreateCategoryRequest request)
	{
		// FIXME: Allow only moderators to call this! - Ero
		// NOTE: Allow administrators to call this as well? - Ero

		// NOTE: Check that body.PlayersMax > body.PlayersMin? - Ero
		Category category = new()
		{
			Name = request.Name,
			Slug = request.Slug,
			Rules = request.Rules,
			PlayersMin = request.PlayersMin ?? 1,
			PlayersMax = request.PlayersMax ?? request.PlayersMin ?? 1,
			LeaderboardId = request.LeaderboardId,
		};

		await _categoryService.Create(category);

		return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
	}
}
