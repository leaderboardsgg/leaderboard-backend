using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class LeaderboardService : ILeaderboardService
{
	private readonly ApplicationContext ApplicationContext;

	public LeaderboardService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Leaderboard?> GetLeaderboard(long id)
	{
		return await ApplicationContext.Leaderboards.FindAsync(id);
	}

	// FIXME: Paginate this
	public async Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null)
	{
		if (ids is null)
		{
			return await ApplicationContext.Leaderboards.ToListAsync();
		}
		else
		{
			return await ApplicationContext.Leaderboards.Where(l => ids.Contains(l.Id)).ToListAsync();
		}
	}

	public async Task CreateLeaderboard(Leaderboard leaderboard)
	{
		ApplicationContext.Leaderboards.Add(leaderboard);
		await ApplicationContext.SaveChangesAsync();
	}
}
