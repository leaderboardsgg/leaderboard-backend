using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class LeaderboardService(ApplicationContext applicationContext) : ILeaderboardService
{
    public async Task<Leaderboard?> GetLeaderboard(long id) =>
        await applicationContext.Leaderboards.FindAsync(id);

    public Task<Leaderboard?> GetLeaderboardBySlug(string slug) =>
        applicationContext.Leaderboards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug);

    // FIXME: Paginate this
    public async Task<List<Leaderboard>> GetLeaderboards(long[]? ids = null)
    {
        if (ids is null)
        {
            return await applicationContext.Leaderboards.ToListAsync();
        }
        else
        {
            return await applicationContext.Leaderboards
                .Where(leaderboard => ids.Contains(leaderboard.Id))
                .ToListAsync();
        }
    }

    public async Task CreateLeaderboard(Leaderboard leaderboard)
    {
        applicationContext.Leaderboards.Add(leaderboard);
        await applicationContext.SaveChangesAsync();
    }
}
