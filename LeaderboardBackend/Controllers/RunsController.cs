using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
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
    [SwaggerResponse(404)]
    public async Task<ActionResult<RunViewModel>> GetRun(Guid id)
    {
        Run? run = await runService.GetRun(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(RunViewModel.MapFrom(run));
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
