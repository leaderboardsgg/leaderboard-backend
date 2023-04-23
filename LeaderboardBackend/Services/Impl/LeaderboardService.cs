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
	public async Task<List<Leaderboard>> GetLeaderboards(GetLeaderboardsQuery query)
	{
		IQueryable<Leaderboard> queryable = _applicationContext.Leaderboards.AsNoTracking();

		if (query.Ids is not null && query.Ids.Length > 0)
		{
			queryable = queryable.Where(leaderboard => query.Ids.Contains(leaderboard.Id));
		}

		if (query.Slug is not null)
		{
			queryable = queryable.Where(leaderboard => leaderboard.Slug == query.Slug);
		}

		return await queryable.ToListAsync();
	}

	public async Task CreateLeaderboard(Leaderboard leaderboard)
	{
		_applicationContext.Leaderboards.Add(leaderboard);
		await _applicationContext.SaveChangesAsync();
	}
}
