using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class LeaderboardService : ILeaderboardService
{
	private readonly ApplicationContext _applicationContext;

	public LeaderboardService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Leaderboard?> GetLeaderboard(long id)
	{
		return await _applicationContext.Leaderboards
			.FindAsync(id);
	}

	// FIXME: Paginate this
	public async Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null)
	{
		if (ids is null)
		{
			return await _applicationContext.Leaderboards
				.ToListAsync();
		}
		else
		{
			return await _applicationContext.Leaderboards
				.Where(l => ids.Contains(l.Id))
				.ToListAsync();
		}
	}

	public async Task CreateLeaderboard(Leaderboard leaderboard)
	{
		_applicationContext.Leaderboards.Add(leaderboard);
		await _applicationContext.SaveChangesAsync();
	}
}
