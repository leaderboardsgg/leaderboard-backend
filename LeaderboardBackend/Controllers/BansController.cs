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

	/// <summary>Get bans by leaderboard ID</summary>
	/// <param name="leaderboardId">The leaderboard ID.</param>
	/// <response code="200">A list of bans. Can be an empty array.</response>
	/// <response code="404">No bans found for the Leaderboard.
	/// </response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Get))]
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
	/// <response code="404">No bans found for the User.
	/// </response>
	[AllowAnonymous]
	[ApiConventionMethod(typeof(Conventions),
							nameof(Conventions.Get))]
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
