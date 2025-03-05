using System.Net.Mime;
using System.Text.Json;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneOf;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class RunsController(
    IRunService runService,
    ICategoryService categoryService,
    IUserService userService,
    IOptions<JsonOptions> options
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

        // This is needed because of what we think is a bug with serialisation. The "$type" field
        // doesn't show up in the response if we simply return Ok(RunViewModel.MapFrom(run)).
        // And we want that field present for consumers to better discriminate what they're getting.
        return Content(
            JsonSerializer.Serialize(RunViewModel.MapFrom(run), options.Value.JsonSerializerOptions),
            MediaTypeNames.Application.Json
        );
    }

    [Authorize]
    [HttpPost("/category/{id:long}/runs/create")]
    [SwaggerOperation("Creates a new Run for a Category with ID `id`. This request is restricted to confirmed Users and Administrators.", OperationId = "createRun")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401, "The client is not logged in.", typeof(ProblemDetails))]
    [SwaggerResponse(403, "The requesting User is unauthorized to create Runs.", typeof(ProblemDetails))]
    [SwaggerResponse(404, "The Category with ID `id` could not be found.", typeof(ProblemDetails))]
    [SwaggerResponse(422, "The request body is incorrect in some way. Read the `title` field to get more information.", Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<RunViewModel>> CreateRun([FromRoute] long id, [FromBody, SwaggerRequestBody(Required = true)] JsonDocument request)
    {
        GetUserResult res = await userService.GetUserFromClaims(HttpContext.User);

        if (res.TryPickT0(out User user, out OneOf<BadCredentials, UserNotFound> _))
        {
            CreateRunResult r = await runService.CreateRun(user, id, request);
            return r.Match<ActionResult>(
                run =>
                {
                    CreatedAtActionResult result = CreatedAtAction(nameof(GetRun), new { id = run.Id.ToUrlSafeBase64String() }, RunViewModel.MapFrom(run));
                    return result;
                },
                badRole => Forbid(),
                notFound => NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Category Not Found")),
                unprocessable => UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 422, unprocessable.Title))
            );
        }

        return Unauthorized();
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
            return NotFound("Run not found");
        }

        Category? category = await categoryService.GetCategoryForRun(run);

        if (category is null)
        {
            return NotFound("Category not found");
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }
}
