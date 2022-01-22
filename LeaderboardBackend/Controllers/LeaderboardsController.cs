using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using LeaderboardBackend.Controllers.Requests;

namespace LeaderboardBackend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LeaderboardsController : ControllerBase
	{
		private readonly ILeaderboardService _leaderboardService;
		public LeaderboardsController(
			ILeaderboardService leaderboardService
		)
		{
			_leaderboardService = leaderboardService;
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Leaderboard>> GetLeaderboard(long id)
		{
			Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(id);
			if (leaderboard == null)
			{
				return NotFound();
			}

			return leaderboard;
		}

		[HttpGet]
		public async Task<ActionResult<List<Leaderboard>>> GetLeaderboards([FromQuery] long[] ids)
		{
			return await _leaderboardService.GetLeaderboards(ids);
		}

		// FIXME: Only allow admins to call this route
		[HttpPost]
		public async Task<ActionResult<Leaderboard>> CreateLeaderboard([FromBody] CreateLeaderboardRequest body)
		{
			Leaderboard leaderboard = new Leaderboard
			{
				Name = body.Name,
				Slug = body.Slug,
			};
			await _leaderboardService.CreateLeaderboard(leaderboard);
			return CreatedAtAction(nameof(GetLeaderboard), new { id = leaderboard.Id }, leaderboard);
		}
	}
}
