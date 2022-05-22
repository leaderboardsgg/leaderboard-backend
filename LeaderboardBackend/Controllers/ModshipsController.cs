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
public class ModshipsController : ControllerBase
{
	private readonly ILeaderboardService LeaderboardService;
	private readonly IModshipService ModshipService;
	private readonly IUserService UserService;

	public ModshipsController(
		ILeaderboardService leaderboardService,
		IModshipService modshipService,
		IUserService userService
	)
	{
		LeaderboardService = leaderboardService;
		ModshipService = modshipService;
		UserService = userService;
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
		Modship? modship = await ModshipService.GetModship(id);

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
	public async Task<ActionResult> CreateModship([FromBody] CreateModshipRequest body)
	{
		User? user = await UserService.GetUserById(body.UserId);
		Leaderboard? leaderboard = await LeaderboardService.GetLeaderboard(body.LeaderboardId);

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

		await ModshipService.CreateModship(modship);
		return CreatedAtAction(nameof(GetModship), new { id = modship.Id }, modship);
	}

	/// <summary>Removes a User as a Mod from a Leaderboard. Admin-only.</summary>
	/// <param name="body">A RemoveModshipRequest</param>
	/// <response code="204">Request was successfull.</response>
	/// <response code="400">If the request is malformed.</response>
	/// <response code="404">The User, Leaderboard or Modship was not found.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.Admin)]
	[HttpDelete]
	public async Task<ActionResult> DeleteMod([FromBody] RemoveModshipRequest body)
	{
		User? user = await UserService.GetUserById(body.UserId);
		Leaderboard? leaderboard = await LeaderboardService.GetLeaderboard(body.LeaderboardId);

		if (user is null || leaderboard is null)
		{
			return NotFound();
		}

		Modship? toBeDeleted = await ModshipService.GetModshipForLeaderboard(leaderboard.Id, user.Id);

		if (toBeDeleted is null)
		{
			return NotFound();
		}

		await ModshipService.DeleteModship(toBeDeleted);

		return NoContent();
	}
}
