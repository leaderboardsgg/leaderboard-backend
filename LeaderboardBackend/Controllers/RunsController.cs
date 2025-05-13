using System.Net;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
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
    [HttpGet("api/run/{id}")]
    [SwaggerOperation("Gets a Run by its ID.", OperationId = "getRun")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Run with ID `id` could not be found.", typeof(ProblemDetails))]
    public async Task<ActionResult<RunViewModel>> GetRun([FromRoute] Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(RunViewModel.MapFrom(run));
    }

    [Authorize]
    [HttpPost("/category/{id:long}/runs/create")]
    [SwaggerOperation("Creates a new Run for a Category with ID `id`. This request is restricted to confirmed Users and Administrators.", OperationId = "createRun")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401, "The client is not logged in.", typeof(ProblemDetails))]
    [SwaggerResponse(400, Type = typeof(ValidationProblemDetails))]
    [SwaggerResponse(403, "The requesting User is unauthorized to create Runs.", typeof(ProblemDetails))]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.", typeof(ProblemDetails))]
    [SwaggerResponse(422, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<RunViewModel>> CreateRun(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] CreateRunRequest request
    )
    {
        GetUserResult res = await userService.GetUserFromClaims(HttpContext.User);

        if (!res.IsT0)
        {
            return Unauthorized();
        }

        Category? category = await categoryService.GetCategory(id);

        if (category is null)
        {
            return Problem(
                null,
                null,
                404,
                "Category Not Found"
            );
        }

        if (category.DeletedAt is not null)
        {
            return Problem(
                null,
                null,
                404,
                "Category Is Deleted"
            );
        }

        CreateRunResult r = await runService.CreateRun(res.AsT0, category, request);

        return r.Match<ActionResult>(
            run =>
            {
                CreatedAtActionResult result = CreatedAtAction(
                    nameof(GetRun),
                    new { id = run.Id.ToUrlSafeBase64String() },
                    RunViewModel.MapFrom(run)
                );
                return result;
            },
            badRole => Forbid(),
            badRunType => Problem(
                null,
                null,
                422,
                "The request's runType does not match the category's."
            )
        );
    }

    [AllowAnonymous]
    [HttpGet("/api/category/{id:long}/runs")]
    [Paginated]
    [SwaggerOperation("Gets the Runs for a Category.", OperationId = "getRunsForCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.", Type = typeof(ProblemDetails))]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ListView<RunViewModel>>> GetRunsForCategory(
        [FromRoute] long id,
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published
    )
    {
        GetRunsForCategoryResult result = await runService.GetRunsForCategory(id, status, page);

        return result.Match<ActionResult>(
            runs => Ok(new ListView<RunViewModel>()
            {
                Data = runs.Items.Select(RunViewModel.MapFrom).ToList(),
                Total = runs.ItemsTotal
            }),
            notFound => Problem(
                null,
                null,
                404,
                "Category Not Found"
            )
        );
    }

    [AllowAnonymous]
    [HttpGet("/api/run/{id}/category")]
    [SwaggerOperation("Gets the category a run belongs to.", OperationId = "getRunCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategoryForRun(Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return NotFound("Run Not Found");
        }

        Category? category = await categoryService.GetCategoryForRun(run);

        if (category is null)
        {
            return NotFound("Category Not Found");
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }

    // TODO: Replace UserTypes with UserRole, i.e. reconfigure authZ policy infra
    [Authorize]
    [HttpPatch("run/{id}")]
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
    [SwaggerResponse(403, "The user attempted to update another user's run, or the user is banned or not yet confirmed.", Type = typeof(ProblemDetails))]
    [SwaggerResponse(404, "The Run with ID `id` could not be found, or has been deleted. Read `title` for more information.", Type = typeof(ProblemDetails))]
    [SwaggerResponse(
        422,
        "Response can be a `ProblemDetails` for a request that doesn't match " +
        "the run type of a category, or a `ValidationProblemDetails` " +
        "otherwise.",
        Type = typeof(ProblemDetails)
    )]
    public async Task<ActionResult> UpdateRun(
        [FromRoute] Guid id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateRunRequest request
    )
    {
        GetUserResult userRes = await userService.GetUserFromClaims(HttpContext.User);

        if (!userRes.IsT0)
        {
            return Unauthorized();
        }

        UpdateRunResult res = await runService.UpdateRun(userRes.AsT0, id, request);

        return res.Match<ActionResult>(
            badRole => Forbid(),
            userDoesNotOwnRun => Problem(
                null,
                null,
                403,
                "User Does Not Own Run"
            ),
            notFound => Problem(
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

                return Problem(
                    null,
                    null,
                    404,
                    title
                );
            },
            badRunType =>
                Problem(
                    null,
                    null,
                    422,
                    "Incorrect Run Type"
                ),
            success => NoContent()
        );
    }

    // TODO: To replace UserTypes with UserRole
    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("/run/{id}")]
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
    public async Task<ActionResult> DeleteRun(Guid id)
    {
        DeleteResult res = await runService.DeleteRun(id);

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
