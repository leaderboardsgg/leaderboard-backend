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
public class NumericalMetricsController : ControllerBase
{
	private INumericalMetricService _numericalMetricService;
	private ICategoryService _categoryService;
	private IRunService _runService;

	public NumericalMetricsController(
		INumericalMetricService numericalMetricService,
		ICategoryService categoryService,
		IRunService runService
	)
	{
		_numericalMetricService = numericalMetricService;
		_categoryService = categoryService;
		_runService = runService;
	}

	/// <summary>
	///     Gets a NumericalMetric by its ID.
	/// </summary>
	/// <param name="id">The ID of the `NumericalMetric` which should be retrieved.</param>
	/// <response code="200">The `NumericalMetric` was found and returned successfully.</response>
	/// <response code="404">No `NumericalMetric` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<NumericalMetric>> GetNumericalMetric(long id)
	{
		try
		{
			NumericalMetric metric = await _numericalMetricService.Get(id);
			return Ok(metric);
		}
		catch (System.Exception)
		{
			return NotFound();
		}
	}

	/// <summary>
	///     Creates a new NumericalMetric.
	/// </summary>
	/// <param name="request">
	///     The `CreateNumericalMetricRequest` instance from which to create the `NumericalMetric`.
	/// </param>
	/// <response code="201">The `NumericalMetric` was created and returned successfully.</response>
	/// <response code="400">The `Category` or `Run` IDs passed to the request do not exist.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[HttpPost]
	public async Task<ActionResult<NumericalMetric>> CreateNumericalMetric(CreateNumericalMetricRequest request)
	{
		// TODO: Check for existing NumericalMetrics
		ParsedCreateNumericalMetricRequest parsed = ParsedCreateNumericalMetricRequest.Parse(request);

		try
		{
			List<Category> categories = await _categoryService.GetCategories(parsed.CategoryIds);
			// TODO: Error on categories.Count == 0
			NumericalMetric metric = new()
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

			await _numericalMetricService.Create(metric);

			return CreatedAtAction(nameof(GetNumericalMetric), new { id = metric.Id }, metric);
		}
		catch (System.Exception)
		{
			return BadRequest();
		}
	}

	/// <summary>
	///     Deletes a NumericalMetric.
	/// </summary>
	/// <param name="id">
	///     The id of the `NumericalMetric` to be deleted.
	/// </param>
	/// <response code="201">The `NumericalMetric` was created and returned successfully.</response>
	/// <response code="400">The `Category` or `Run` IDs passed to the request do not exist.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Delete))]
	[HttpDelete]
	public async Task<ActionResult> DeleteNumericalMetric(long id)
	{
		try
		{
			NumericalMetric metric = await _numericalMetricService.Get(id);
			await _numericalMetricService.Delete(metric);
			return NoContent();
		}
		catch (System.Exception)
		{
			return NotFound(id);
		}
	}
}
