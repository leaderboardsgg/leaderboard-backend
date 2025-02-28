using System.Net.Mime;
using System.Text.Json;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
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
    // We expect an encoded GUID and not the GUID itself. Refer to GuidExtensions.cs.
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
    [HttpPost("runs/create")]
    [SwaggerOperation("Creates a new Run.", OperationId = "createRun")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> CreateRun([FromBody] CreateRunRequest request)
    {
        // FIXME: Should return Task<ActionResult<Run>>! - Ero
        // NOTE: Return NotFound for anything in here? - Ero

        GetUserResult res = await userService.GetUserFromClaims(HttpContext.User);

        if (res.TryPickT0(out User user, out OneOf<BadCredentials, UserNotFound> _))
        {
            Run run = new()
            {
                PlayedOn = request.PlayedOn,
                CategoryId = request.CategoryId,
                User = user,
            };

            await runService.CreateRun(run);
            return CreatedAtAction(nameof(GetRun), new { id = run.Id }, RunViewModel.MapFrom(run));
        }

        return Unauthorized();
    }

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
