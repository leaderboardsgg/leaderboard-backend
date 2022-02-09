using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services
{
	public class LeaderboardService : ILeaderboardService
	{
		private ApplicationContext _applicationContext;
		private IConfiguration _config;
		public LeaderboardService(ApplicationContext applicationContext, IConfiguration config)
		{
			_applicationContext = applicationContext;
			_config = config;
		}

		public async Task<Leaderboard?> GetLeaderboard(long id)
		{
			Leaderboard? leaderboard = await _applicationContext.Leaderboards.FindAsync(id);
			return leaderboard;
		}

		// FIXME: Paginate this
		public async Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null)
		{
			if (ids == null)
			{
				return await _applicationContext.Leaderboards.ToListAsync();
			}
			else
			{
				return await _applicationContext.Leaderboards.Where(l => ids.Contains(l.Id)).ToListAsync();
			}
		}

		public async Task CreateLeaderboard(Leaderboard leaderboard)
		{
			_applicationContext.Leaderboards.Add(leaderboard);
			await _applicationContext.SaveChangesAsync();
		}
	}
}
