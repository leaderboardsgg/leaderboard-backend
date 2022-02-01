using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Controllers.Requests;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class BansController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly IAuthService _authService;
	private readonly IBanService _banService;

	public BansController(
		IUserService userService,
		IAuthService authService,
		IBanService banService
	)
	{
		_userService = userService;
		_authService = authService;
		_banService = banService;
	}

	// [Authorize]
	// public async Task<ActionResult<Ban>>CreateBanSite()
	// {
	// 	// TODO: Implement
	// }

	// [Authorize]
	// public async Task<ActionResult<Ban>>RemoveBanSite()
	// {
	// 	// TODO: Implement
	// }

	// [Authorize]
	// public async Task<ActionResult<Ban>>CreateBanMod()
	// {
	// 	// TODO: Implement
	// }

	// [Authorize]
	// public async Task<ActionResult<Ban>>RemoveBanMod()
	// {
	// 	// TODO: Implement
	// }

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
	[HttpGet]
	public async Task<ActionResult<List<Ban>>>GetBans([FromQuery]long? leaderboardId, [FromQuery]Guid? bannedUserId)
	{
		if (leaderboardId != null && bannedUserId != null)
		{
			return BadRequest("Specify only either a leaderboard ID, or a user ID. Don't specify both.");
		}
		if (leaderboardId != null)
		{
			return Ok(await _banService.GetBans(leaderboardId));
		}
		if (bannedUserId != null)
		{
			return Ok(await _banService.GetBans(bannedUserId));
		}
		return Ok(await _banService.GetBans());
	}

	/// <summary>Get a Ban from its ID.</summary>
	/// <param name="id">The Ban ID.</param>
	/// <response code="200">The found Ban.</response>
	/// <response code="404">If no Ban can be found.</response>
	[AllowAnonymous]
	[HttpGet("{id:long}")]
	public async Task<ActionResult<Ban>>GetBan(ulong id)
	{
		var ban = await _banService.GetBanById(id);
		if (ban == null)
		{
			return NotFound();
		}
		return Ok(ban);
	}
}
