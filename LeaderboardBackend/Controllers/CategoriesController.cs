using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class CategoriesController : ApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [AllowAnonymous]
    [HttpGet("api/category/{id}")]
    [SwaggerOperation("Gets a Category by its ID.", OperationId = "getCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategory(long id)
    {
        Category? category = await _categoryService.GetCategory(id);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("categories/create")]
    [SwaggerOperation("Creates a new Category. This request is restricted to Moderators.", OperationId = "createCategory")]
    [SwaggerResponse(201)]
    [SwaggerResponse(403)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<CategoryViewModel>> CreateCategory(
        [FromBody] CreateCategoryRequest request
    )
    {
        Category category =
            new()
            {
                Name = request.Name,
                Slug = request.Slug,
                Info = request.Info,
                LeaderboardId = request.LeaderboardId,
            };

        await _categoryService.CreateCategory(category);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            CategoryViewModel.MapFrom(category)
        );
    }
}
