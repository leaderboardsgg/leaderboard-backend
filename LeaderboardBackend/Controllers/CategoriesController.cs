using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class CategoriesController(ICategoryService categoryService) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/category/{id}")]
    [SwaggerOperation("Gets a Category by its ID.", OperationId = "getCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategory(long id)
    {
        Category? category = await categoryService.GetCategory(id);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboard/{id:long}/categories/create")]
    [SwaggerOperation("Creates a new Category for a Leaderboard with ID `id`. This request is restricted to Administrators.", OperationId = "createCategory")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create Categories.")]
    [SwaggerResponse(404, "The Leaderboard with ID `id` could not be found.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(409, "A Category with the specified slug already exists.", typeof(CategoryViewModel))]
    [SwaggerResponse(422, $"The request contains errors. The following errors can occur: NotEmptyValidator, {SlugRule.SLUG_FORMAT}", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<CategoryViewModel>> CreateCategory(
        [FromRoute, SwaggerParameter(Required = true)] long id,
        [FromBody, SwaggerRequestBody(Required = true)] CreateCategoryRequest request
    )
    {
        CreateCategoryResult r = await categoryService.CreateCategory(id, request);

        return r.Match<ActionResult<CategoryViewModel>>(
            category => CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                CategoryViewModel.MapFrom(category)
            ),
            conflict => Conflict(conflict.Conflicting),
            notFound => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Leaderboard Not Found"))
        );
    }
}
