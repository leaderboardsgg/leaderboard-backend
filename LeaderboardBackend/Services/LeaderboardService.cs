using LeaderboardBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class LeaderboardService : ILeaderboardService
{
	private readonly ApplicationContext _applicationContext;
	private readonly IConfiguration _config;

	public LeaderboardService(ApplicationContext applicationContext, IConfiguration config)
	{
		_applicationContext = applicationContext;
		_config = config;
	}

	public async Task<Leaderboard?> GetLeaderboard(ulong id)
	{
		return await _applicationContext.Leaderboards.FindAsync(id);
	}

	// FIXME: Paginate this
	public async Task<List<Leaderboard>> GetLeaderboards(ulong[]? ids = null)
	{
		return ids == null
			? await _applicationContext.Leaderboards.ToListAsync()
			: await _applicationContext.Leaderboards.Where(l => ids.Contains(l.Id)).ToListAsync();
	}

	public async Task CreateLeaderboard(Leaderboard leaderboard)
	{
		_applicationContext.Leaderboards.Add(leaderboard);
		await _applicationContext.SaveChangesAsync();
	}
}