using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
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
    [HttpGet("api/categories/{id:long}")]
    [SwaggerOperation("Gets a Category by its ID.", OperationId = "getCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategory([FromRoute] long id)
    {
        Category? category = await categoryService.GetCategory(id);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards/{id:long}/categories/{slug}")]
    [SwaggerOperation("Gets a Category of Leaderboard `id` by its slug. Will not return deleted Categories.", OperationId = "getCategoryBySlug")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Category either doesn't exist for the Leaderboard, or it has been deleted.", typeof(ProblemDetails))]
    public async Task<ActionResult<CategoryViewModel>> GetCategoryBySlug(
        [FromRoute] long id,
        [FromRoute] string slug
    )
    {
        Category? category = await categoryService.GetCategoryBySlug(id, slug);

        if (category == null)
        {
            return NotFound();
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards/{id:long}/categories")]
    [Paginated]
    [SwaggerOperation("Gets all Categories of Leaderboard `id`.", OperationId = "getCategoriesForLeaderboard")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Leaderboard with ID `id` could not be found.", typeof(ProblemDetails))]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ListView<CategoryViewModel>>> GetCategoriesForLeaderboard(
        [FromRoute] long id,
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published
    )
    {
        GetCategoriesForLeaderboardResult r = await categoryService.GetCategoriesForLeaderboard(id, status, page);

        return r.Match<ActionResult<ListView<CategoryViewModel>>>(
            categories => Ok(new ListView<CategoryViewModel>
            {
                Data = categories.Items.Select(CategoryViewModel.MapFrom).ToList(),
                Total = categories.ItemsTotal
            }),
            notFound => NotFound()
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboards/{id:long}/categories")]
    [SwaggerOperation("Creates a new Category for a Leaderboard with ID `id`. This request is restricted to Administrators.", OperationId = "createCategory")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create Categories.")]
    [SwaggerResponse(404, "The Leaderboard with ID `id` could not be found.", typeof(ProblemDetails))]
    [SwaggerResponse(409, "A Category with the specified slug already exists.", typeof(ConflictDetails<CategoryViewModel>))]
    [SwaggerResponse(422, $"The request contains errors. The following errors can occur: NotEmptyValidator, {SlugRule.SLUG_FORMAT}", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<CategoryViewModel>> CreateCategory(
        [FromRoute] long id,
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
            notFound => Problem(
                null,
                null,
                404,
                "Leaderboard Not Found"
            )
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPatch("categories/{id:long}")]
    [SwaggerOperation(
        "Updates a category with the specified new fields. This request is restricted to administrators. " +
        "Note: `type` cannot be updated. " +
        "This operation is atomic; if an error occurs, the category will not be updated. " +
        "All fields of the request body are optional but you must specify at least one.",
        OperationId = "updateCategory"
    )]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(404, Type = typeof(ProblemDetails))]
    [SwaggerResponse(
        409,
        "The specified slug is already in use by another category. Returns the conflicting category.",
        typeof(ConflictDetails<CategoryViewModel>)
    )]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> UpdateCategory(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateCategoryRequest request
    )
    {
        UpdateResult<Category> res = await categoryService.UpdateCategory(id, request);

        return res.Match<ActionResult>(
            conflict =>
            {
                ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                problemDetails.Extensions.Add("conflicting", CategoryViewModel.MapFrom(conflict.Conflicting));
                return Conflict(problemDetails);
            },
            notFound => NotFound(),
            success => NoContent()
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("categories/{id:long}")]
    [SwaggerOperation("Deletes a Category. This request is restricted to Administrators.", OperationId = "deleteCategory")]
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
    public async Task<ActionResult> DeleteCategory([FromRoute] long id)
    {
        DeleteResult res = await categoryService.DeleteCategory(id);

        return res.Match<ActionResult>(
            success => NoContent(),
            notFound => NotFound(),
            alreadyDeleted => Problem(
                null,
                null,
                404,
                "Already Deleted"
            )
        );
    }
}
