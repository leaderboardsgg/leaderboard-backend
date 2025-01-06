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
    [HttpPost("categories/create")]
    [SwaggerOperation("Creates a new Category. This request is restricted to Moderators.", OperationId = "createCategory")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create Categories.")]
    [SwaggerResponse(409, "A Category with the specified slug already exists.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(422, $"The request contains errors. The following errors can occur: NotEmptyValidator, {SlugRule.SLUG_FORMAT}", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<CategoryViewModel>> CreateCategory(
        [FromBody, SwaggerRequestBody(Required = true)] CreateCategoryRequest request
    )
    {
        CreateCategoryResult r = await categoryService.CreateCategory(request);

        return r.Match<ActionResult<CategoryViewModel>>(
            category => CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                CategoryViewModel.MapFrom(category)
            ),
            conflict =>
            {
                ModelState.AddModelError(nameof(request.Slug), "SlugAlreadyUsed");

                return Conflict(new ValidationProblemDetails(ModelState));
            },
            notFound => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Leaderboard Not Found"))
        );
    }
}
