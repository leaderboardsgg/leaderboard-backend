using LeaderboardBackend.Authorization;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardsController : ControllerBase
{
	private readonly ILeaderboardService _leaderboardService;

	public LeaderboardsController(ILeaderboardService leaderboardService)
	{
		_leaderboardService = leaderboardService;
	}

	/// <summary>
	///     Gets a Leaderboard by its ID.
	/// </summary>
	/// <param name="id">The ID of the `Leaderboard` which should be retrieved.</param>
	/// <response code="200">The `Leaderboard` was found and returned successfully.</response>
	/// <response code="404">No `Leaderboard` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Leaderboard>> GetLeaderboard(long id)
	{
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(id);

		if (leaderboard == null)
		{
			return NotFound();
		}

		return Ok(leaderboard);
	}

	/// <summary>
	///     Gets Leaderboards by their IDs.
	/// </summary>
	/// <param name="ids">The IDs of the `Leaderboard`s which should be retrieved.</param>
	/// <response code="200">
	///     The list of `Leaderboard`s was retrieved successfully. The result can be an empty
	///     collection.
	/// </response>
	[AllowAnonymous]
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<List<Leaderboard>>> GetLeaderboards(
		[FromQuery] long[] ids)
	{
		return Ok(await _leaderboardService.GetLeaderboards(ids));
	}

	/// <summary>
	///     Creates a new Leaderboard.
	///     This request is restricted to Administrators.
	/// </summary>
	/// <param name="request">
	///     The `CreateLeaderboardRequest` instance from which to create the `Leaderboard`.
	/// </param>
	/// <response code="201">The `Leaderboard` was created and returned successfully.</response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="404">
	///     The requesting `User` is unauthorized to create `Leaderboard`s.
	/// </response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.ADMIN)]
	[HttpPost]
	public async Task<ActionResult<Leaderboard>> CreateLeaderboard(
		[FromBody] CreateLeaderboardRequest request)
	{
		Leaderboard leaderboard = new()
		{
			Name = request.Name,
			Slug = request.Slug
		};

		await _leaderboardService.CreateLeaderboard(leaderboard);

		return CreatedAtAction(nameof(GetLeaderboard), new { id = leaderboard.Id }, leaderboard);
	}
}
