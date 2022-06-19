using LeaderboardBackend.Authorization;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LeaderboardsController : ControllerBase
{
	private readonly ILeaderboardService LeaderboardService;

	public LeaderboardsController(ILeaderboardService leaderboardService)
	{
		LeaderboardService = leaderboardService;
	}

	/// <summary>Gets a leaderboard.</summary>
	/// <param name="id">The leaderboard's ID.</param>
	/// <response code="200">The Leaderboard.</response>
	/// <response code="404">If no Leaderboard can be found.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.GetAnon))]
	[AllowAnonymous]
	[HttpGet("{id}")]
	public async Task<ActionResult<Leaderboard>> GetLeaderboard(long id)
	{
		Leaderboard? leaderboard = await LeaderboardService.GetLeaderboard(id);
		if (leaderboard == null)
		{
			return NotFound();
		}

		return Ok(leaderboard);
	}

	/// <summary>Gets leaderboards. Can be an empty array.</summary>
	/// <param name="ids">The IDs.</param>
	/// <response code="200">An array of Leaderboards. Can be empty.</response>
	[ProducesResponseType(StatusCodes.Status200OK)]
	[AllowAnonymous]
	[HttpGet]
	public async Task<ActionResult<List<Leaderboard>>> GetLeaderboards([FromQuery] long[] ids)
	{
		return Ok(await LeaderboardService.GetLeaderboards(ids));
	}

	/// <summary>Creates a new Leaderboard. Admin-only.</summary>
	/// <param name="body">A CreateLeaderboardRequest instance.</param>
	/// <response code="201">The created Leaderboard.</response>
	/// <response code="400">If the request is malformed.</response>
	/// <response code="404">If a non-admin calls this.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.Admin)]
	[HttpPost]
	public async Task<ActionResult<Leaderboard>> CreateLeaderboard([FromBody] CreateLeaderboardRequest body)
	{
		Leaderboard leaderboard = new()
		{
			Name = body.Name,
			Slug = body.Slug
		};

		await LeaderboardService.CreateLeaderboard(leaderboard);
		return CreatedAtAction(nameof(GetLeaderboard), new { id = leaderboard.Id }, leaderboard);
	}
}
