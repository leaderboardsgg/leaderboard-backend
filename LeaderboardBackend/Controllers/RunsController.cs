using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RunsController : ControllerBase
{
	private readonly IParticipationService _participationService;
	private readonly IRunService _runService;

	public RunsController(IParticipationService participationService, IRunService runService)
	{
		_participationService = participationService;
		_runService = runService;
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
	public async Task<ActionResult<Run>> GetRun(Guid id)
	{
		// NOTE: Should this use [AllowAnonymous]? - Ero

		Run? run = await _runService.GetRun(id);

		if (run is null)
		{
			return NotFound();
		}

		return Ok(run);
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

		Run run = new()
		{
			PlayedOn = request.PlayedOn,
			SubmittedAt = request.SubmittedAt,
			Status = request.Status,
		};

		await _runService.CreateRun(run);

		return CreatedAtAction(nameof(GetRun), new { id = run.Id }, run);
	}

	/// <summary>
	///     Gets all Participations associated with a Run ID.
	/// </summary>
	/// <param name="id">
	///     The ID of the `Run` whose `Participation`s should be retrieved.<br/>
	///     It must be possible to parse this to `long` for this request to complete.
	/// </param>
	/// <response code="200">The list of `Participation`s was retrieved successfully.</response>
	/// <response code="404">
	///     No `Run` with the requested ID could be found or the `Run` does not contain any
	///     `Participation`s.
	/// </response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id}/participations")]
	public async Task<ActionResult<List<Participation>>> GetParticipations(Guid id)
	{
		// NOTE: Should this use [AllowAnonymous]? - Ero

		Run? run = await _runService.GetRun(id);

		if (run is null)
		{
			return NotFound("Run not found");
		}

		List<Participation> participations = await _participationService
			.GetParticipationsForRun(run);

		// NOTE: If a Run happens to have 0 Participations, there's something else severely wrong.
		// Should perhaps return something much more critical. - Ero
		if (participations.Count == 0)
		{
			return NotFound("No participations for this run were found");
		}

		return Ok(participations);
	}
}
