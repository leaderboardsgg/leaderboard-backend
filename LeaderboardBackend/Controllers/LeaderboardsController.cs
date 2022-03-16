using LeaderboardBackend.Controllers.Requests;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderboardsController : ControllerBase
{
	private readonly ILeaderboardService _leaderboardService;

	public LeaderboardsController(ILeaderboardService leaderboardService)
	{
		_leaderboardService = leaderboardService;
	}

	/// <summary>Gets a leaderboard.</summary>
	/// <param name="id">The leaderboard's ID.</param>
	/// <response code="200">The Leaderboard.</response>
	/// <response code="404">If no Leaderboard can be found.</response>
	[HttpGet("{id}")]
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	public async Task<ActionResult<Leaderboard>> GetLeaderboard(ulong id)
	{
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(id);
		if (leaderboard == null)
		{
			return NotFound();
		}

		return Ok(leaderboard);
	}

	/// <summary>Gets leaderboards. Can be an empty array.</summary>
	/// <param name="ids">The IDs.</param>
	/// <response code="200">An array of Leaderboards. Can be empty.</response>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<Leaderboard>>> GetLeaderboards([FromQuery] ulong[] ids)
	{
		return Ok(await _leaderboardService.GetLeaderboards(ids));
	}

	// FIXME: Only allow admins to call this route
	/// <summary>Creates a new Leaderboard. Admin-only.</summary>
	/// <param name="body">A CreateLeaderboardRequest instance.</param>
	/// <response code="201">The created Leaderboard.</response>
	/// <response code="404">If a non-admin calls this.</response>
	[HttpPost]
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	public async Task<ActionResult<Leaderboard>> CreateLeaderboard([FromBody] CreateLeaderboardRequest body)
	{
		Leaderboard leaderboard = new()
		{
			Name = body.Name,
			Slug = body.Slug
		};

		await _leaderboardService.CreateLeaderboard(leaderboard);
		return CreatedAtAction(nameof(GetLeaderboard), new { id = leaderboard.Id }, leaderboard);
	}
}
