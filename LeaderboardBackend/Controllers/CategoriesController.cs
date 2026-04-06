using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class CategoriesController(ICategoryService categoryService) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/categories/{id:long}")]
    [SwaggerOperation("Gets a Category by its ID.", OperationId = "getCategory")]
    public async Task<Results<Ok<CategoryViewModel>, NotFound>> GetCategory([FromRoute] long id)
    {
        Category? category = await categoryService.GetCategory(id);

        if (category == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(CategoryViewModel.MapFrom(category));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards/{id:long}/categories/{slug}")]
    [SwaggerOperation("Gets a Category of Leaderboard `id` by its slug. Will not return deleted Categories.", OperationId = "getCategoryBySlug")]
    public async Task<Results<Ok<CategoryViewModel>, NotFound>> GetCategoryBySlug(
        [FromRoute] long id,
        [FromRoute] string slug
    )
    {
        Category? category = await categoryService.GetCategoryBySlug(id, slug);

        if (category == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(CategoryViewModel.MapFrom(category));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards/{id:long}/categories")]
    [Paginated]
    [SwaggerOperation("Gets all Categories of Leaderboard `id`.", OperationId = "getCategoriesForLeaderboard")]
    public async Task<Results<Ok<ListView<CategoryViewModel>>, NotFound, UnprocessableEntity<ValidationProblemDetails>>> GetCategoriesForLeaderboard(
        [FromRoute] long id,
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published
    )
    {
        GetCategoriesForLeaderboardResult r = await categoryService.GetCategoriesForLeaderboard(id, status, page);

        return r.Match<Results<Ok<ListView<CategoryViewModel>>, NotFound, UnprocessableEntity<ValidationProblemDetails>>>(
            categories => TypedResults.Ok(new ListView<CategoryViewModel>
            {
                Data = [.. categories.Items.Select(CategoryViewModel.MapFrom)],
                Total = categories.ItemsTotal
            }),
            notFound => TypedResults.NotFound()
        );
    }

    // This endpoint must omit one of its possible result types because it only goes up to 6 generic type args.
    // Hopefully this gets fixed soon. PR to address this issue:
    // https://github.com/dotnet/aspnetcore/pull/66092
    // —Ted W
    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboards/{id:long}/categories")]
    [SwaggerOperation("Creates a new Category for a Leaderboard with ID `id`. This request is restricted to Administrators.", OperationId = "createCategory")]
    [SwaggerResponse(400, Type = typeof(ProblemDetails))]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(404, "The Leaderboard with ID `id` could not be found.")]
    [SwaggerResponse(409, "A Category with the specified slug already exists.", typeof(ConflictDetails<CategoryViewModel>))]
    [SwaggerResponse(422, $"The request contains errors. The following errors can occur: NotEmptyValidator, {SlugRule.SLUG_FORMAT}", typeof(ValidationProblemDetails))]
    public async Task<Results<
        CreatedAtRoute<CategoryViewModel>,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        NotFound,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<ConflictDetails<CategoryViewModel>>,
        UnprocessableEntity<ValidationProblemDetails>
    >> CreateCategory(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] CreateCategoryRequest request
    )
    {
        CreateCategoryResult r = await categoryService.CreateCategory(id, request);

        return r.Match<Results<
            CreatedAtRoute<CategoryViewModel>,
            UnauthorizedHttpResult,
            ForbidHttpResult,
            NotFound,
            Microsoft.AspNetCore.Http.HttpResults.Conflict<ConflictDetails<CategoryViewModel>>,
            UnprocessableEntity<ValidationProblemDetails>
        >>(
            category => TypedResults.CreatedAtRoute(
                CategoryViewModel.MapFrom(category),
                nameof(GetCategory),
                new { id = category.Id }),
            conflict =>
                TypedResults.Conflict(CreateConflictDetails(CategoryViewModel.MapFrom(conflict.Conflicting))),
            notFound => TypedResults.NotFound()
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
    [SwaggerResponse(400, Type = typeof(ProblemDetails))]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(
        409,
        "The specified slug is already in use by another category. Returns the conflicting category.",
        typeof(ConflictDetails<CategoryViewModel>)
    )]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<Results<
        NoContent,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        NotFound,
        Microsoft.AspNetCore.Http.HttpResults.Conflict<ConflictDetails<CategoryViewModel>>,
        UnprocessableEntity<ValidationProblemDetails>
    >> UpdateCategory(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateCategoryRequest request
    )
    {
        UpdateResult<Category> res = await categoryService.UpdateCategory(id, request);

        return res.Match<Results<
            NoContent,
            UnauthorizedHttpResult,
            ForbidHttpResult,
            NotFound,
            Microsoft.AspNetCore.Http.HttpResults.Conflict<ConflictDetails<CategoryViewModel>>,
            UnprocessableEntity<ValidationProblemDetails>
        >>(
            conflict =>
                TypedResults.Conflict(CreateConflictDetails(CategoryViewModel.MapFrom(conflict.Conflicting))),
            notFound => TypedResults.NotFound(),
            success => TypedResults.NoContent()
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("categories/{id:long}")]
    [SwaggerOperation("Deletes a Category. This request is restricted to Administrators.", OperationId = "deleteCategory")]
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
    public async Task<Results<
        NoContent,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        NotFound<ProblemDetails>
    >> DeleteCategory([FromRoute] long id)
    {
        DeleteResult res = await categoryService.DeleteCategory(id);

        return res.Match<Results<
            NoContent,
            UnauthorizedHttpResult,
            ForbidHttpResult,
            NotFound<ProblemDetails>
        >>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                404)),
            alreadyDeleted => TypedResults.NotFound(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                404,
                "Already Deleted")));
    }
}
