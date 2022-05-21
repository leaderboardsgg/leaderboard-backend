using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Http;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Requests;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RunsController : ControllerBase
{
	private readonly IRunService RunService;

	public RunsController(IRunService runService)
	{
		RunService = runService;
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
}
