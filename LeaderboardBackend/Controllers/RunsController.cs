using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class RunsController(
    IRunService runService,
    ICategoryService categoryService,
    IUserService userService
    ) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/runs/{id}")]
    [SwaggerOperation("Gets a Run by its ID.", OperationId = "getRun")]
    public async Task<Results<Ok<RunViewModel>, NotFound>> GetRun([FromRoute] Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(RunViewModel.MapFrom(run));
    }

    [Authorize]
    [HttpPost("/categories/{id:long}/runs")]
    [SwaggerOperation("Creates a new Run for a Category with ID `id`. This request is restricted to confirmed Users and Administrators.", OperationId = "createRun")]
    [SwaggerResponse(401, "The client is not logged in.", typeof(ProblemDetails))]
    [SwaggerResponse(403)]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.", typeof(ProblemDetails))]
    public async Task<Results<
        UnauthorizedHttpResult,
        BadRequest<ProblemDetails>,
        CreatedAtRoute<RunViewModel>,
        ForbidHttpResult,
        NotFound<ProblemDetails>,
        UnprocessableEntity<ProblemDetails>
    >> CreateRun(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] CreateRunRequest request
    )
    {
        GetUserResult res = await userService.GetUserFromClaims(HttpContext.User);

        if (!res.IsT0)
        {
            return TypedResults.Unauthorized();
        }

        Category? category = await categoryService.GetCategory(id);

        if (category is null)
        {
            return TypedResults.NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Category Not Found"));
        }

        if (category.DeletedAt is not null)
        {
            return TypedResults.NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Category Is Deleted"));
        }

        if (category.Leaderboard?.DeletedAt is not null)
        {
            return TypedResults.NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Leaderboard Is Deleted"));
        }

        CreateRunResult r = await runService.CreateRun(res.AsT0, category, request);

        return r.Match<Results<
            UnauthorizedHttpResult,
            BadRequest<ProblemDetails>,
            CreatedAtRoute<RunViewModel>,
            ForbidHttpResult,
            NotFound<ProblemDetails>,
            UnprocessableEntity<ProblemDetails>
        >>(
            run =>
                TypedResults.CreatedAtRoute(
                    RunViewModel.MapFrom(run),
                    nameof(GetRun),
                    new { id = run.Id.ToUrlSafeBase64String() }),
            badRole => TypedResults.Forbid(),
            badRunType => TypedResults.UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                422,
                "Mismatched Run Type",
                null,
                "The request's runType does not match the category's.")));
    }

    [AllowAnonymous]
    [HttpGet("/api/categories/{id:long}/runs")]
    [Paginated]
    [SwaggerOperation("Gets all Runs submitted by users for a Category. To get only the personal bests of every user instead, call `GetRecordsForCategory`.", OperationId = "getRunsForCategory")]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.", Type = typeof(ProblemDetails))]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<Results<
        Ok<ListView<RunViewModel>>,
        ProblemHttpResult
    >> GetRunsForCategory(
        [FromRoute] long id,
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published
    )
    {
        GetRunsForCategoryResult result = await runService.GetRunsForCategory(id, status, page);

        return result.Match<Results<
            Ok<ListView<RunViewModel>>,
            ProblemHttpResult
        >>(
            runs => TypedResults.Ok(new ListView<RunViewModel>()
            {
                Data = runs.Items.Select(RunViewModel.MapFrom).ToList(),
                Total = runs.ItemsTotal
            }),
            notFound => TypedResults.Problem(
                null,
                null,
                404,
                "Category Not Found"
            )
        );
    }

    [AllowAnonymous]
    [HttpGet("/api/categories/{id}/records")]
    [Paginated]
    [SwaggerOperation("Gets the records for a category, a.k.a. the personal bests of every user, ranked best-first.", OperationId = "getRecordsForCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<Results<
        Ok<ListView<RunViewModel>>,
        ProblemHttpResult,
        UnprocessableEntity<ValidationProblemDetails>
    >> GetRecordsForCategory(
        [FromRoute] long id,
        [FromQuery] Page page
    )
    {
        GetRecordsForCategoryResult result = await runService.GetRecordsForCategory(id, page);

        return result.Match<Results<
            Ok<ListView<RunViewModel>>,
            ProblemHttpResult,
            UnprocessableEntity<ValidationProblemDetails>
        >>(
            runs => TypedResults.Ok(new ListView<RunViewModel>()
            {
                Data = runs.Items.Select(RunViewModel.MapFrom).ToList(),
                Total = runs.ItemsTotal
            }),
            notFound => TypedResults.Problem(
                null,
                null,
                404,
                "Category Not Found"
            )
        );
    }

    [AllowAnonymous]
    [HttpGet("/api/runs/{id}/category")]
    [SwaggerOperation("Gets the category a run belongs to.", OperationId = "getRunCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<Results<
        Ok<CategoryViewModel>,
        NotFound<string>
    >> GetCategoryForRun(Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return TypedResults.NotFound("Run Not Found");
        }

        Category? category = await categoryService.GetCategoryForRun(run);

        if (category is null)
        {
            return TypedResults.NotFound("Category Not Found");
        }

        return TypedResults.Ok(CategoryViewModel.MapFrom(category));
    }

    // TODO: Replace UserTypes with UserRole, i.e. reconfigure authZ policy infra
    [Authorize]
    [HttpPatch("runs/{id}")]
    [SwaggerOperation(
        "Updates a run with the specified new fields. This request is restricted to administrators " +
        "or users updating their own runs. " +
        "Note: `runType` cannot be updated. " +
        "This operation is atomic; if an error occurs, the run will not be updated. " +
        "All fields of the request body are optional but you must specify at least one.",
        OperationId = "updateRun"
    )]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(
        403,
        "The user attempted to update another user's run, " +
        "the user is banned or not yet confirmed, " +
        "or the user attempted to change the status of a run.",
        Type = typeof(ProblemDetails)
    )]
    [SwaggerResponse(404, "The Run with ID `id` could not be found, or has been deleted. Read `title` for more information.", Type = typeof(ProblemDetails))]
    [SwaggerResponse(
        422,
        "Response can be a `ProblemDetails` for a request that doesn't match " +
        "the run type of a category, or a `ValidationProblemDetails` " +
        "otherwise.",
        Type = typeof(ProblemDetails)
    )]
    public async Task<Results<
        NoContent,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        ProblemHttpResult
    >> UpdateRun(
        [FromRoute] Guid id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateRunRequest request
    )
    {
        GetUserResult userRes = await userService.GetUserFromClaims(HttpContext.User);

        if (!userRes.IsT0)
        {
            return TypedResults.Unauthorized();
        }

        UpdateRunResult res = await runService.UpdateRun(userRes.AsT0, id, request);

        return res.Match<Results<
            NoContent,
            UnauthorizedHttpResult,
            ForbidHttpResult,
            ProblemHttpResult
        >>(
            badRole => TypedResults.Forbid(),
            userDoesNotOwnRun => TypedResults.Problem(
                null,
                null,
                403,
                "User Does Not Own Run"
            ),
            userCannotChangeStatusOfRuns => TypedResults.Problem(
                null,
                null,
                403,
                "User Cannot Change Status of Runs"
            ),
            notFound => TypedResults.Problem(
                null,
                null,
                404,
                "Run Not Found"
            ),
            alreadyDeleted =>
            {
                string title;

                if (alreadyDeleted.DeletedEntity == typeof(Category))
                {
                    title = "Category Is Deleted";
                }

                else if (alreadyDeleted.DeletedEntity == typeof(Leaderboard))
                {
                    title = "Leaderboard Is Deleted";
                }

                else
                {
                    title = "Run Is Deleted";
                }

                return TypedResults.Problem(
                    null,
                    null,
                    404,
                    title
                );
            },
            badRunType =>
                TypedResults.Problem(
                    null,
                    null,
                    422,
                    "Incorrect Run Type"
                ),
            success => TypedResults.NoContent()
        );
    }

    // TODO: To replace UserTypes with UserRole
    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("/runs/{id}")]
    [SwaggerOperation("Deletes a Run.", OperationId = "deleteRun")]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(
        404,
        """
        The run does not exist (Not Found) or was already deleted (Already Deleted).
        Use the `title` field of the response to differentiate between the two cases if necessary.
        """,
        typeof(ProblemDetails)
    )]
    public async Task<Results<
        NoContent,
        NotFound,
        ProblemHttpResult
    >> DeleteRun(Guid id)
    {
        DeleteResult res = await runService.DeleteRun(id);

        return res.Match<Results<
            NoContent,
            NotFound,
            ProblemHttpResult
        >>(
            success => TypedResults.NoContent(),
            notFound => TypedResults.NotFound(),
            alreadyDeleted => TypedResults.Problem(
                null,
                null,
                404,
                "Already Deleted"
            )
        );
    }
}
