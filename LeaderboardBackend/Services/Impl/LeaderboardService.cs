using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LeaderboardBackend.Services;

public class LeaderboardService(ApplicationContext applicationContext) : ILeaderboardService
{
    public async Task<Leaderboard?> GetLeaderboard(long id) =>
        await applicationContext.Leaderboards.FindAsync(id);

    public async Task<Leaderboard?> GetLeaderboardBySlug(string slug) =>
        await applicationContext.Leaderboards
            .FirstOrDefaultAsync(b => b.Slug == slug && b.DeletedAt == null);

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

    public async Task<CreateLeaderboardResult> CreateLeaderboard(CreateLeaderboardRequest request)
    {
        Leaderboard lb = new()
        {
            Name = request.Name,
            Slug = request.Slug,
            Info = request.Info
        };

        applicationContext.Leaderboards.Add(lb);

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return new CreateLeaderboardConflict();
        }

        return lb;
    }
}
