using System.Net;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
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
            return NotFound(
                ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    404,
                    "Category Not Found"
                )
            );
        }

        if (category.DeletedAt is not null)
        {
            return NotFound(
                ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    404,
                    "Category Is Deleted"
                )
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
            badRunType => UnprocessableEntity(
                ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    422,
                    null,
                    null,
                    "The request's runType does not match the category's."
                )
            )
        );
    }

    [AllowAnonymous]
    [HttpGet("/api/category/{id:long}/runs")]
    [Paginated]
    [SwaggerOperation("Gets the Runs for a Category.", OperationId = "getRunsForCategory")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404, "The Category with ID `id` could not be found, or has been deleted. Read `title` for more information.")]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ListView<RunViewModel>>> GetRunsForCategory(
        [FromRoute] long id,
        [FromQuery] Page page,
        [FromQuery, SwaggerParameter(Required = false, Description = "Whether to include deleted runs. Defaults false.")] bool includeDeleted = false
    )
    {
        GetRunsForCategoryResult result = await runService.GetRunsForCategory(id, page, includeDeleted);

        return result.Match<ActionResult>(
            runs => Ok(new ListView<RunViewModel>()
            {
                Data = runs.Items.Select(RunViewModel.MapFrom).ToList(),
                Total = runs.ItemsTotal
            }),
            notFound => NotFound(
                ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    404,
                    "Category Not Found"
                )
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
    [SwaggerResponse(403, "The user attempted to update another user's run, or the user isn't an admin.")]
    [SwaggerResponse(404, Type = typeof(ProblemDetails))]
    [SwaggerResponse(422, Type = typeof(ProblemDetails))]
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
            notFound => NotFound(),
            badRunType =>
                UnprocessableEntity(
                    ProblemDetailsFactory.CreateProblemDetails(
                        HttpContext,
                        422,
                        null,
                        null,
                        "The request's runType does not match the category's."
                    )
                ),
            success => NoContent()
        );
    }
}
