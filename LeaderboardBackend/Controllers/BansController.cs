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
[Produces("application/json")]
public class BansController : ControllerBase
{
	private readonly IUserService UserService;
	private readonly IAuthService AuthService;
	private readonly IBanService BanService;
	private readonly ILeaderboardService LeaderboardService;

	public BansController(
		IUserService userService,
		IAuthService authService,
		IBanService banService,
		ILeaderboardService leaderboardService
	)
	{
		UserService = userService;
		AuthService = authService;
		BanService = banService;
		LeaderboardService = leaderboardService;
	}

	/// <summary>Get bans by leaderboard ID</summary>
	/// <param name="leaderboardId">The leaderboard ID.</param>
	/// <response code="200">A list of bans. Can be an empty array.</response>
	/// <response code="404">No bans found for the Leaderboard.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.GetAnon))]
	[HttpGet("leaderboard/{leaderboardId:long}")]
	public async Task<ActionResult<List<Ban>>> GetBansByLeaderboard(long leaderboardId)
	{
		List<Ban> bans = await BanService.GetBansByLeaderboard(leaderboardId);

		if (bans.Count == 0)
		{
			return NotFound("No bans found for this leaderboard");
		}

		return Ok(bans);
	}

	/// <summary>Get bans by user ID.</summary>
	/// <param name="bannedUserId">The user ID.</param>
	/// <response code="200">A list of bans. Can be an empty array.</response>
	/// <response code="404">No bans found for the User.</response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.GetAnon))]
	[HttpGet("leaderboard/{bannedUserId:Guid}")]
	public async Task<ActionResult<List<Ban>>> GetBansByUser(Guid bannedUserId)
	{
		List<Ban> bans = await BanService.GetBansByUser(bannedUserId);

		if (bans.Count == 0)
		{
			return NotFound("No bans found for this user");
		}

		return Ok(bans);
	}

	/// <summary>Get a Ban from its ID.</summary>
	/// <param name="id">The Ban ID.</param>
	/// <response code="200">The found Ban.</response>
	/// <response code="404">No Ban can be found.</response>
	[AllowAnonymous]

	[ApiConventionMethod(typeof(Conventions),
							 nameof(Conventions.Get))]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<Ban>> GetBan(long id)
	{
		Ban? ban = await BanService.GetBanById(id);
		if (ban == null)
		{
			return NotFound();
		}
		return Ok(ban);
	}

	/// <summary>Creates a side-wide ban. Admin-only.</summary>
	/// <param name="body">A CreateSiteBanRequest instance.</param>
	/// <response code="201">The created Ban.</response>
	/// <response code="400">The request is malformed.</response>
	/// <response code="401">A non-admin calls this.</response>
	/// <response code="403">The banned user is also an admin.</response>
	/// <response code="404">The banned user is not found.</response>
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.Admin)]
	[HttpPost]
	public async Task<ActionResult<Ban>> CreateSiteBan([FromBody] CreateSiteBanRequest body)
	{
		Guid? adminId = AuthService.GetUserIdFromClaims(HttpContext.User);

		if (adminId is null)
		{
			return Forbid();
		}

		User? bannedUser = await UserService.GetUserById(body.UserId);

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
			Reason = body.Reason,
			BanningUserId = adminId,
			BannedUserId = bannedUser.Id,
		};

		await BanService.CreateBan(ban);
		return CreatedAtAction(nameof(GetBan), new { id = ban.Id }, ban);
	}

	/// <summary>Creates a leaderboard-wide ban. Mod-only.</summary>
	/// <param name="body">A CreateLeaderboardBanRequest instance.</param>
	/// <response code="201">The created Ban.</response>
	/// <response code="400">The request is malformed.</response>
	/// <response code="401">A non-admin or mod calls this.</response>
	/// <response code="403">The banned user is an admin or a mod.</response>
	/// <response code="404">The banned user is not found.</response>
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Post))]
	[Authorize(Policy = UserTypes.Mod)]
	[HttpPost("leaderboard")]
	public async Task<ActionResult<Ban>> CreateLeaderboardBan([FromBody] CreateLeaderboardBanRequest body)
	{
		Guid? modId = AuthService.GetUserIdFromClaims(HttpContext.User);

		if (modId is null)
		{
			return Forbid();
		}

		User? bannedUser = await UserService.GetUserById(body.UserId);
		Leaderboard? leaderboard = await LeaderboardService.GetLeaderboard(body.LeaderboardId);

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
			Reason = body.Reason,
			BanningUserId = modId,
			BannedUserId = bannedUser.Id,
			LeaderboardId = leaderboard.Id
		};

		await BanService.CreateBan(ban);
		return CreatedAtAction(nameof(GetBan), new { id = ban.Id }, ban);
	}

	/// <summary>Removes a ban, including site-wide bans. Admin-only.</summary>
	/// <param name="id">The ban ID.</param>
	/// <response code="204">The ban was successfully deleted.</response>
	/// <response code="401">The user isn't logged in.</response>
	/// <response code="403">The user is a non-admin.</response>
	/// <response code="404">The ban could not be found.</response>
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.Admin)]
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteBan(long id)
	{
		try
		{
			await BanService.DeleteBan(id);
			return NoContent();
		} catch (ArgumentNullException)
		{
			return NotFound($"Ban not found: {id}");
		}
	}

	/// <summary>Removes a leaderboard-wide ban. Mod-only.</summary>
	/// <param name="id">The ban ID.</param>
	/// <param name="leaderboardId">The leaderboard ID.</param>
	/// <response code="204">The ban was successfully deleted.</response>
	/// <response code="401">The user isn't logged in.</response>
	/// <response code="403">The user is a non-admin, or the ban is site-wide.</response>
	/// <response code="404">The ban could not be found.</response>
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Delete))]
	[Authorize(Policy = UserTypes.Mod)]
	[HttpDelete("{id}/leaderboards/{leaderboardId}")]
	public async Task<ActionResult> DeleteLeaderboardBan(long id, long leaderboardId)
	{
		try
		{
			await BanService.DeleteLeaderboardBan(id, leaderboardId);
			return NoContent();
		} catch (ArgumentNullException)
		{
			return NotFound($"Ban not found: {id}");
		}
	}
}
