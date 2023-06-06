using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RunsController : ControllerBase
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

    /// <summary>
    ///     Gets a Run by its ID.
    /// </summary>
    /// <param name="id">
    ///     The ID of the `Run` which should be retrieved.<br/>
    ///     It must be possible to parse this to `long` for this request to complete.
    /// </param>
    /// <response code="200">The `Run` was found and returned successfully.</response>
    /// <response code="404">No `Run` with the requested ID could be found.</response>
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
    [HttpGet("{id}")]
    public async Task<ActionResult<RunViewModel>> GetRun(Guid id)
    {
        // NOTE: Should this use [AllowAnonymous]? - Ero

        Run? run = await _runService.GetRun(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(RunViewModel.MapFrom(run));
    }

    /// <summary>
    ///     Creates a new Run.
    /// </summary>
    /// <param name="request">
    ///     The `CreateRunRequest` instance from which to create the `Run`.
    /// </param>
    /// <response code="201">The `Run` was created and returned successfully.</response>
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
    [HttpPost]
    public async Task<ActionResult> CreateRun([FromBody] CreateRunRequest request)
    {
        // FIXME: Should return Task<ActionResult<Run>>! - Ero
        // NOTE: Return NotFound for anything in here? - Ero

        Run run =
            new()
            {
                PlayedOn = request.PlayedOn,
                SubmittedAt = request.SubmittedAt,
                CategoryId = request.CategoryId
            };

        await _runService.CreateRun(run);

        return CreatedAtAction(nameof(GetRun), new { id = run.Id }, RunViewModel.MapFrom(run));
    }

    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
    [HttpGet("{id}/category")]
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
