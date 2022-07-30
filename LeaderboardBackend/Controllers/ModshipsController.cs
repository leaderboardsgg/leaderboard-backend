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
public class ModshipsController : ControllerBase
{
	private readonly ILeaderboardService _leaderboardService;
	private readonly IModshipService _modshipService;
	private readonly IUserService _userService;

	public ModshipsController(
		ILeaderboardService leaderboardService,
		IModshipService modshipService,
		IUserService userService)
	{
		_leaderboardService = leaderboardService;
		_modshipService = modshipService;
		_userService = userService;
	}

	/// <summary>
	///     Gets a Modship by its ID.
	/// </summary>
	/// <param name="modshipId">
	///     The ID of the *Moderator* (`User`) which should be retrieved.
	/// </param>
	/// <response code="200">The `Modship` was found and returned successfully.</response>
	/// <response code="404">No `User` with the requested ID could be found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id}")]
	public async Task<ActionResult<Modship>> GetModship(Guid modshipId)
	{
		Modship? modship = await _modshipService.GetModship(modshipId);

		if (modship is null)
		{
			return NotFound();
		}

		return Ok(modship);
	}

	/// <summary>
	///     Promotes a User to Moderator for a Leaderboard.
	///     This request is restricted to Administrators.
	/// </summary>
	/// <param name="request">
	///     The `CreateModshipRequest` instance from which to perform the promotion.
	/// </param>
	/// <response code="201">
	///     The `User` was promoted successfully. The `Modship` is returned.
	/// </response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="404">
	///     The requesting `User` is unauthorized to promote other `User`s.
	/// </response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.ADMIN)]
	[HttpPost]
	public async Task<ActionResult> CreateModship([FromBody] CreateModshipRequest request)
	{
		User? user = await _userService.GetUserById(request.UserId);
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(request.LeaderboardId);

		if (user is null || leaderboard is null)
		{
			return NotFound();
		}

		Modship modship = new()
		{
			LeaderboardId = request.LeaderboardId,
			UserId = request.UserId,
			User = user,
			Leaderboard = leaderboard
		};

		await _modshipService.CreateModship(modship);

		return CreatedAtAction(nameof(GetModship), new { id = modship.Id }, modship);
	}

	/// <summary>
	///     Demotes a Moderator to User for a Leaderboard.
	///     This request is restricted to Administrators.
	/// </summary>
	/// <param name="request">
	///     The `RemoveModshipRequest` instance from which to perform the demotion.
	/// </param>
	/// <response code="204">The `User` was demoted successfully.</response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="404">
	///     No `User`, `Leaderboard`, or `Modship` with the requested IDs could be found, or the
	///     requesting `User` is unauthorized to demote other `User`s.
	/// </response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.ADMIN)]
	[HttpDelete]
	public async Task<ActionResult> DeleteMod([FromBody] RemoveModshipRequest request)
	{
		User? user = await _userService.GetUserById(request.UserId);
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(request.LeaderboardId);

		if (user is null || leaderboard is null)
		{
			return NotFound();
		}

		Modship? toBeDeleted = await _modshipService
			.GetModshipForLeaderboard(leaderboard.Id, user.Id);

		if (toBeDeleted is null)
		{
			return NotFound();
		}

		await _modshipService.DeleteModship(toBeDeleted);

		return NoContent();
	}
}
