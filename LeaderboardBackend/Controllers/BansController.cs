using LeaderboardBackend.Authorization;
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
public class BansController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly IBanService _banService;
	private readonly ILeaderboardService _leaderboardService;
	private readonly IUserService _userService;

	public BansController(
		IAuthService authService,
		IBanService banService,
		ILeaderboardService leaderboardService,
		IUserService userService)
	{
		_authService = authService;
		_banService = banService;
		_leaderboardService = leaderboardService;
		_userService = userService;
	}

	/// <summary>
	///     Gets all Bans associated with a Leaderboard ID.
	/// </summary>
	/// <param name="leaderboardId">
	///     The ID of the `Leaderboard` whose `Ban`s should be listed.
	/// </param>
	/// <response code="200">
	///     The list of `Ban`s was retrieved successfully. The result can be an empty collection.
	/// </response>
	/// <response code="404">No `Leaderboard` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
	[HttpGet("leaderboard/{leaderboardId:long}")]
	public async Task<ActionResult<List<Ban>>> GetBansByLeaderboard(long leaderboardId)
	{
		List<Ban> bans = await _banService.GetBansByLeaderboard(leaderboardId);

		if (bans.Count == 0)
		{
			return NotFound("No bans found for this leaderboard");
		}

		return Ok(bans);
	}

	/// <summary>
	///     Gets all Bans associated with a User ID.
	/// </summary>
	/// <param name="bannedUserId">The ID of the `User` whose `Ban`s should be listed.</param>
	/// <response code="200">
	///     The list of `Ban`s was retrieved successfully. The result can be an empty collection.
	/// </response>
	/// <response code="404">No `User` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
	[HttpGet("leaderboard/{bannedUserId:Guid}")]
	public async Task<ActionResult<List<Ban>>> GetBansByUser(Guid bannedUserId)
	{
		List<Ban> bans = await _banService.GetBansByUser(bannedUserId);

		if (bans.Count == 0)
		{
			return NotFound("No bans found for this user");
		}

		return Ok(bans);
	}

	/// <summary>
	///     Gets a Ban by its ID.
	/// </summary>
	/// <param name="id">The ID of the `Ban` which should be retrieved.</param>
	/// <response code="200">The `Ban` was found and returned successfully.</response>
	/// <response code="404">No `Ban` with the requested ID could be found.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<Ban>> GetBan(long id)
	{
		Ban? ban = await _banService.GetBanById(id);

		if (ban == null)
		{
			return NotFound();
		}

		return Ok(ban);
	}

	/// <summary>
	///     Issues a site-scoped Ban.
	///     This request is restricted to Administrators.
	/// </summary>
	/// <param name="request">
	///     The `CreateSiteBanRequest` instance from which to create the `Ban`.
	/// </param>
	/// <response code="201">The `Ban` was created and returned successfully.</response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="401">
	///     The requesting `User` is unauthorized to issue site-scoped `Ban`s.
	/// </response>
	/// <response code="403">
	///     The `User` to be banned was also an Administrator. This operation is forbidden.
	/// </response>
	/// <response code="404">The `User` to be banned was not found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.ADMIN)]
	[HttpPost]
	public async Task<ActionResult<Ban>> CreateSiteBan([FromBody] CreateSiteBanRequest request)
	{
		Guid? adminId = _authService.GetUserIdFromClaims(HttpContext.User);

		if (adminId is null)
		{
			return Forbid();
		}

		User? bannedUser = await _userService.GetUserById(request.UserId);

		if (bannedUser is null)
		{
			return NotFound("User not found");
		}

		if (bannedUser.Admin)
		{
			return StatusCode(StatusCodes.Status403Forbidden, "Admin users cannot be banned.");
		}

		Ban ban = new()
		{
			Reason = request.Reason,
			BanningUserId = adminId,
			BannedUserId = bannedUser.Id,
		};

		await _banService.CreateBan(ban);

		return CreatedAtAction(nameof(GetBan), new { id = ban.Id }, ban);
	}

	/// <summary>
	///     Issues a Leaderboard-scoped Ban.
	///     This request is restricted to Moderators and Administrators.
	/// </summary>
	/// <param name="request">
	///     The `CreateLeaderboardBanRequest` instance from which to create the `Ban`.
	/// </param>
	/// <response code="201">The `Ban` was created and returned successfully.</response>
	/// <response code="400">The request was malformed.</response>
	/// <response code="401">
	///     The requesting `User` is unauthorized to issue `Leaderboard`-scoped `Ban`s.
	/// </response>
	/// <response code="403">
	///     The `User` to be banned was also an Administrator. This operation is forbidden.
	/// </response>
	/// <response code="404">The `User` to be banned was not found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.MOD)]
	[HttpPost("leaderboard")]
	public async Task<ActionResult<Ban>> CreateLeaderboardBan(
		[FromBody] CreateLeaderboardBanRequest request)
	{
		Guid? modId = _authService.GetUserIdFromClaims(HttpContext.User);

		if (modId is null)
		{
			return Forbid();
		}

		User? bannedUser = await _userService.GetUserById(request.UserId);
		Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(request.LeaderboardId);

		if (bannedUser is null)
		{
			return NotFound("User not found");
		}

		if (leaderboard is null)
		{
			return NotFound("Leaderboard not found");
		}

		if (bannedUser.Admin || bannedUser.Modships is not null)
		{
			return StatusCode(StatusCodes.Status403Forbidden, "Cannot ban users with same or higher rights.");
		}

		Ban ban = new()
		{
			Reason = request.Reason,
			BanningUserId = modId,
			BannedUserId = bannedUser.Id,
			LeaderboardId = leaderboard.Id
		};

		await _banService.CreateBan(ban);

		return CreatedAtAction(nameof(GetBan), new { id = ban.Id }, ban);
	}

	/// <summary>
	///     Lifts a Leaderboard-scoped or site-scoped Ban.
	///     This request is restricted to Administrators.
	/// </summary>
	/// <param name="id">The ID of the `Ban` to remove.</param>
	/// <response code="204">The `Ban` was removed successfully.</response>
	/// <response code="401">The requesting `User` is not logged-in.</response>
	/// <response code="403">The requesting `User` is unauthorized to lift `Ban`s.</response>
	/// <response code="404">No `Ban` with the requested ID could be found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.ADMIN)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteBan(long id)
	{
		try
		{
			await _banService.DeleteBan(id);

			return NoContent();
		}
		catch (ArgumentNullException)
		{
			return NotFound($"Ban not found: {id}");
		}
	}

	/// <summary>
	///     Lift a Leaderboard-scoped Ban.
	///     This request is restricted to Moderators and Administrators.
	/// </summary>
	/// <param name="id">The ID of the `Ban` to remove.</param>
	/// <param name="leaderboardId">The ID of the `Leaderboard` the `Ban` is scoped to.</param>
	/// <response code="204">The `Ban` was removed successfully.</response>
	/// <response code="401">The requesting `User` is not logged-in.</response>
	/// <response code="403">The requesting `User` is unauthorized to lift `Ban`s.</response>
	/// <response code="404">No `Ban` with the requested ID could be found.</response>
	[ApiConventionMethod(typeof(Conventions), nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.MOD)]
	[HttpDelete("{id}/leaderboards/{leaderboardId}")]
	public async Task<ActionResult> DeleteLeaderboardBan(long id, long leaderboardId)
	{
		try
		{
			await _banService.DeleteLeaderboardBan(id, leaderboardId);

			return NoContent();
		}
		catch (ArgumentNullException)
		{
			return NotFound($"Ban not found: {id}");
		}
	}
}
