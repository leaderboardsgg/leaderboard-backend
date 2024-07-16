using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class RunsController : ApiController
{
    private readonly IRunService _runService;
    private readonly ICategoryService _categoryService;

    public RunsController(
        IRunService runService,
        ICategoryService categoryService
    )
    {
        _runService = runService;
        _categoryService = categoryService;
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [SwaggerOperation("Gets a Run by its ID.")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<RunViewModel>> GetRun(Guid id)
    {
        Run? run = await _runService.GetRun(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(RunViewModel.MapFrom(run));
    }

    [Authorize]
    [HttpPost]
    [SwaggerOperation("Creates a new Run.")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> CreateRun([FromBody] CreateRunRequest request)
    {
        // FIXME: Should return Task<ActionResult<Run>>! - Ero
        // NOTE: Return NotFound for anything in here? - Ero

        Run run =
            new()
            {
                PlayedOn = request.PlayedOn,
                CreatedAt = request.SubmittedAt,
                CategoryId = request.CategoryId
            };

        await _runService.CreateRun(run);

        return CreatedAtAction(nameof(GetRun), new { id = run.Id }, RunViewModel.MapFrom(run));
    }

    [HttpGet("{id}/category")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<CategoryViewModel>> GetCategoryForRun(Guid id)
    {
        Run? run = await _runService.GetRun(id);

        if (run is null)
        {
            return NotFound("Run not found");
        }

        Category? category = await _categoryService.GetCategoryForRun(run);

        if (category is null)
        {
            return NotFound("Category not found");
        }

        return Ok(CategoryViewModel.MapFrom(category));
    }
}
