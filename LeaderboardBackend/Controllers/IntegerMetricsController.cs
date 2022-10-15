using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class IntegerMetricsController : ControllerBase
{
	private readonly IIntegerMetricService _integerMetricService;
	private readonly ICategoryService _categoryService;
	private readonly IRunService _runService;

	public IntegerMetricsController(
		IIntegerMetricService integerMetricService,
		ICategoryService categoryService,
		IRunService runService
	)
	{
		_integerMetricService = integerMetricService;
		_categoryService = categoryService;
		_runService = runService;
	}

	/// <summary>
	///     Gets a IntegerMetric by its ID.
	/// </summary>
	/// <param name="id">The ID of the `IntegerMetric` which should be retrieved.</param>
	/// <response code="200">The `IntegerMetric` was found and returned successfully.</response>
	/// <response code="404">No `IntegerMetric` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<IntegerMetric>> GetIntegerMetric(long id)
	{
		try
		{
			IntegerMetric metric = await _integerMetricService.Get(id);
			return Ok(metric);
		}
		catch (System.Exception)
		{
			return NotFound();
		}
	}

	/// <summary>
	///     Creates a new IntegerMetric.
	/// </summary>
	/// <param name="request">
	///     The `CreateIntegerMetricRequest` instance from which to create the `IntegerMetric`.
	/// </param>
	/// <response code="201">The `IntegerMetric` was created and returned successfully.</response>
	/// <response code="400">The `Category` or `Run` IDs passed to the request do not exist.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[HttpPost]
	public async Task<ActionResult<IntegerMetric>> CreateIntegerMetric(CreateIntegerMetricRequest request)
	{
		// TODO: Check for existing IntegerMetrics
		ParsedCreateIntegerMetricRequest parsed = new(request);

		try
		{
			List<Category> categories = await _categoryService.GetCategories(parsed.CategoryIds);
			// TODO: Error on categories.Count == 0
			IntegerMetric metric = new()
			{
				Categories = categories,
				Max = parsed.Max,
				Min = parsed.Min,
				Name = parsed.Name
			};

			if (parsed.RunIds.Length > 0)
			{
				List<Run> runs = await _runService.GetRuns(parsed.RunIds);
				// TODO: Error on runs.Count == 0
				metric.Runs = runs;
			}

			await _integerMetricService.Create(metric);

			return CreatedAtAction(nameof(GetIntegerMetric), new { id = metric.Id }, metric);
		}
		catch (System.Exception)
		{
			return BadRequest();
		}
	}

	/// <summary>
	///     Deletes a IntegerMetric.
	/// </summary>
	/// <param name="id">
	///     The id of the `IntegerMetric` to be deleted.
	/// </param>
	/// <response code="201">The `IntegerMetric` was created and returned successfully.</response>
	/// <response code="400">The `Category` or `Run` IDs passed to the request do not exist.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Delete))]
	[HttpDelete]
	public async Task<ActionResult> DeleteIntegerMetric(long id)
	{
		try
		{
			IntegerMetric metric = await _integerMetricService.Get(id);
			await _integerMetricService.Delete(metric);
			return NoContent();
		}
		catch (System.Exception)
		{
			return NotFound(id);
		}
	}
}
