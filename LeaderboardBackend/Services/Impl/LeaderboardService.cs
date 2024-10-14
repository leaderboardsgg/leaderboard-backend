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

    // FIXME: Paginate these
    public async Task<List<Leaderboard>> ListLeaderboards() =>
        await applicationContext.Leaderboards
            .Where(lb => lb.DeletedAt == null).ToListAsync();

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

    public async Task<RestoreLeaderboardResult> RestoreLeaderboard(long id)
    {
        Leaderboard? lb = await applicationContext.Leaderboards.FindAsync([id]);

        if (lb == null)
        {
            return new LeaderboardNotFound();
        }

        if (lb.DeletedAt == null)
        {
            return new LeaderboardNeverDeleted();
        }

        applicationContext.Leaderboards.Update(lb);

        lb.DeletedAt = null;

        await applicationContext.SaveChangesAsync();

        return Task.CompletedTask;
    }
}
