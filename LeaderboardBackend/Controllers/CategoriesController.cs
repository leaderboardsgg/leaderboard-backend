using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
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
    [SwaggerResponse(404, "The Leaderboard with ID `id` could not be found.", typeof(ProblemDetails))]
    [SwaggerResponse(409, "A Category with the specified slug already exists.", typeof(ConflictDetails<CategoryViewModel>))]
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
            conflict =>
                {
                    ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                    problemDetails.Extensions.Add("conflicting", CategoryViewModel.MapFrom(conflict.Conflicting));
                    return Conflict(problemDetails);
                },
            notFound => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Leaderboard Not Found"))
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("category/{id:long}")]
    [SwaggerOperation("Deletes a Category. This request is restricted to Administrators.", OperationId = "deleteLeaderboard")]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(
        404,
        """
        The Category does not exist (Not Found) or was already deleted (Already Deleted).
        Use the `title` field of the response to differentiate between the two cases if necessary.
        """,
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult> DeleteCategory([FromRoute, SwaggerParameter(Required = true)] long id)
    {
        DeleteResult res = await categoryService.DeleteCategory(id);

        return res.Match<ActionResult>(
            success => NoContent(),
            notFound => NotFound(),
            alreadyDeleted => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Already Deleted"))
        );
    }
}
