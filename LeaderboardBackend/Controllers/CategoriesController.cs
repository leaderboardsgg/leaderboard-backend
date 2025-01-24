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
    [HttpPatch("category/{id:long}")]
    [SwaggerOperation(
        "Updates a category with the specified new fields. This request is restricted to administrators. " +
        "Note: `type` cannot be updated." +
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
        typeof(ConflictDetails<Category>)
    )]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> UpdateCategory(
        [FromRoute, SwaggerParameter(Required = true)] long id,
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
    [HttpDelete("category/{id:long}")]
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
            alreadyDeleted => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Already Deleted"))
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPut("category/{id:long}/restore")]
    [SwaggerOperation("Restores a deleted Category.", OperationId = "restoreCategory")]
    [SwaggerResponse(200, "The restored `Category`s view model.", typeof(CategoryViewModel))]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to restore `Category`s.")]
    [SwaggerResponse(404, "The `Category` was not found, or it wasn't deleted in the first place. Includes a field, `title`, which will be \"Not Found\" in the former case, and \"Not Deleted\" in the latter.", typeof(ProblemDetails))]
    [SwaggerResponse(409, "Another `Category` with the same slug has been created since, and therefore can't be restored. Said `Category` will be returned in the `conflicting` field in the response.", typeof(ConflictDetails<CategoryViewModel>))]
    public async Task<ActionResult<CategoryViewModel>> RestoreCategory(
        long id
    )
    {
        RestoreResult<Category> r = await categoryService.RestoreCategory(id);

        return r.Match<ActionResult<CategoryViewModel>>(
            category => Ok(CategoryViewModel.MapFrom(category)),
            notFound => NotFound(),
            neverDeleted =>
                NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Not Deleted")),
            conflict =>
            {
                ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                problemDetails.Extensions.Add("conflicting", CategoryViewModel.MapFrom(conflict.Conflicting));
                return Conflict(problemDetails);
            }
        );
    }
}
