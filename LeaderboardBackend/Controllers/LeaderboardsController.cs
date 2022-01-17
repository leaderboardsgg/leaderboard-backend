using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardsController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;
        public LeaderboardsController(
            ILeaderboardService leaderboardService
        ) {
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

        public async Task<List<Leaderboard[]>> GetLeaderboards([FromQuery]long[] ?ids)
        {
            return await _leaderboardService.GetLeaderboards(ids);
        }
    }
}
