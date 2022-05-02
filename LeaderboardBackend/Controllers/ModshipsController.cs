using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using LeaderboardBackend.Controllers.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ModshipsController : ControllerBase
{
	private readonly ILeaderboardService _leaderboardService;
	private readonly IModshipService _modshipService;
	private readonly IUserService _userService;

	public ModshipsController(
		ILeaderboardService leaderboardService,
		IModshipService modshipService,
		IUserService userService
	)
	{
		_leaderboardService = leaderboardService;
		_modshipService = modshipService;
		_userService = userService;
	}

	/// <summary>Gets a Modship.</summary>
	/// <param name="id">The mod User's ID.</param>
	/// <response code="200">The Modship.</response>
	/// <response code="404">If no Modship can be found.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Modship>> GetModship(Guid id)
	{
		Modship? modship = await _modshipService.GetModship(id);

		if (modship is null)
		{
			return NotFound();
		}

		return Ok(modship);
	}

	/// <summary>Makes a User a Mod for a Leaderboard. Admin-only.</summary>
	/// <param name="body">A CreateModshipRequest instance.</param>
	/// <response code="201">An object containing the Modship ID.</response>
	/// <response code="400">If the request is malformed.</response>
	/// <response code="404">If a non-admin calls this.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.Admin)]
	[HttpPost]
	public async Task<ActionResult> MakeMod([FromBody] CreateModshipRequest body)
	{
		User? user = await _userService.GetUserById(body.UserId);
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(body.LeaderboardId);

		if (user is null || leaderboard is null)
		{
			return NotFound();
		}

		Modship modship = new()
		{
			LeaderboardId = body.LeaderboardId,
			UserId = body.UserId,
			User = user,
			Leaderboard = leaderboard
		};

		await _modshipService.CreateModship(modship);
		return CreatedAtAction(nameof(MakeMod), new { id = modship.Id }, modship);
	}
}
