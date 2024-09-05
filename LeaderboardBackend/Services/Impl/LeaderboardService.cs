using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class LeaderboardService(ApplicationContext applicationContext) : ILeaderboardService
{
    private readonly ApplicationContext _applicationContext = applicationContext;

    public ValueTask<Leaderboard?> GetLeaderboard(long id) =>
        _applicationContext.Leaderboards
            .FindAsync([id]);

    public Task<Leaderboard?> GetLeaderboardBySlug(string slug) =>
        _applicationContext.Leaderboards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug);

    // FIXME: Paginate this
    public async Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null)
    {
        if (ids is null)
        {
            return await _applicationContext.Leaderboards.ToListAsync();
        }
        else
        {
            return await _applicationContext.Leaderboards
                .Where(leaderboard => ids.Contains(leaderboard.Id))
                .ToListAsync();
        }
    }

    public async Task CreateLeaderboard(Leaderboard leaderboard)
    {
        _applicationContext.Leaderboards.Add(leaderboard);
        await _applicationContext.SaveChangesAsync();
    }
}
