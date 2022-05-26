using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RunsController : ControllerBase
{
	private readonly IRunService RunService;
	private readonly IParticipationService ParticipationService;

	public RunsController(IRunService runService, IParticipationService participationService)
	{
		RunService = runService;
		ParticipationService = participationService;
	}

	/// <summary>Gets a Run.</summary>
	/// <param name="id">The Run ID. Must parse to a long for this route to be hit.</param>
	/// <response code="200">The Run with the provided ID.</response>
	/// <response code="404">If no Run is found with the provided ID.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Run>> GetRun(Guid id)
	{
		Run? run = await RunService.GetRun(id);
		if (run is null)
		{
			return NotFound();
		}

		return Ok(run);
	}

	/// <summary>Creates a Run.</summary>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	[HttpPost]
	public async Task<ActionResult> CreateRun([FromBody] CreateRunRequest request)
	{
		Run run = new()
		{
			Played = request.Played,
			Submitted = request.Submitted,
			Status = request.Status,
		};

		await RunService.CreateRun(run);
		return CreatedAtAction(nameof(GetRun), new { id = run.Id }, run);
	}

	/// <summary>Gets the Participations for a Run.</summary>
	/// <param name="id">The Run ID.</param>
	/// <response code="200">An array with all participations.</response>
	/// <response code="404">If the run is not found or the run has no participations.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	[HttpGet("participations/{id}")]
	public async Task<ActionResult<List<Participation>>> GetParticipations(Guid id)
	{
		Run? run = await RunService.GetRun(id);
		if (run is null)
		{
			return NotFound();
		}

		List<Participation> participations = await ParticipationService.GetParticipationForRun(run);

		if (participations.Count == 0)
		{
			return NotFound();
		}

		return Ok(participations);
	}
}
