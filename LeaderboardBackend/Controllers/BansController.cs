using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
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

	public BansController(
		IUserService userService,
		IAuthService authService,
		IBanService banService
	)
	{
		UserService = userService;
		AuthService = authService;
		BanService = banService;
	}

	/// <summary>Get all bans, optionally filtered by a Leaderboard or User.</summary>
	/// <remarks>
	/// Simply calling this without any query parameters will get all present bans. Else:
	/// <ul>
	///   <li>passing <code>leaderboardId</code> will return all bans a Leaderboard has</li>
	///   <li>passing <code>bannedUserId</code> will return all bans a User has</li>
	/// </ul>
	/// Don't specify both a Leaderboard ID and a User ID. Only specify one.
	/// </remarks>
	/// <param name="leaderboardId">Optional. Gets Bans a Leaderboard has.</param>
	/// <param name="bannedUserId">Optional. Gets Bans a User has.</param>
	/// <response code="200">A list of bans. Can be an empty array.</response>
	/// <response code="400">
	/// If both <code>leaderboardId</code> and <code>bannedUserId</code> are given.
	/// </response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions),
							 nameof(Conventions.Get))]
	[HttpGet]
	public async Task<ActionResult<List<Ban>>> GetBans([FromQuery] long? leaderboardId, [FromQuery] Guid? bannedUserId)
	{
		if (leaderboardId != null && bannedUserId != null)
		{
			return BadRequest("Specify only either a leaderboard ID, or a user ID. Don't specify both.");
		}
		if (leaderboardId != null)
		{
			return Ok(await BanService.GetBans(leaderboardId));
		}
		if (bannedUserId != null)
		{
			return Ok(await BanService.GetBans(bannedUserId));
		}
		return Ok(await BanService.GetBans());
	}

	/// <summary>Get a Ban from its ID.</summary>
	/// <param name="id">The Ban ID.</param>
	/// <response code="200">The found Ban.</response>
	/// <response code="404">If no Ban can be found.</response>
	[AllowAnonymous]

	[ApiConventionMethod(typeof(Conventions),
							 nameof(Conventions.Get))]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<Ban>> GetBan(ulong id)
	{
		Ban? ban = await BanService.GetBanById(id);
		if (ban == null)
		{
			return NotFound();
		}
		return Ok(ban);
	}
}
