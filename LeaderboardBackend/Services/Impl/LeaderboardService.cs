using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Npgsql;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class LeaderboardService(ApplicationContext applicationContext, IClock clock) : ILeaderboardService
{
    public async Task<Leaderboard?> GetLeaderboard(long id) =>
        await applicationContext.Leaderboards.FindAsync(id);

    public async Task<Leaderboard?> GetLeaderboardBySlug(string slug) =>
        await applicationContext.Leaderboards
            .FirstOrDefaultAsync(b => b.Slug == slug && b.DeletedAt == null);

    // FIXME: Paginate these
    public async Task<List<Leaderboard>> ListLeaderboards(bool includeDeleted)
    {
        IQueryable<Leaderboard> lbs = applicationContext.Leaderboards;
        return await (includeDeleted ? lbs : lbs.Where(lb => lb.DeletedAt == null)).ToListAsync();
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
            return new Conflict<Leaderboard>();
        }

        return lb;
    }

    public async Task<RestoreLeaderboardResult> RestoreLeaderboard(long id)
    {
        Leaderboard? lb = await applicationContext.Leaderboards.FindAsync(id);

        if (lb == null)
        {
            return new NotFound();
        }

        if (lb.DeletedAt == null)
        {
            return new LeaderboardNeverDeleted();
        }

        lb.DeletedAt = null;

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            Leaderboard conflict = await applicationContext.Leaderboards.SingleAsync(c => c.Slug == lb.Slug && c.DeletedAt == null);
            return new Conflict<Leaderboard>(conflict);
        }

        return lb;
    }

    public async Task<DeleteResult> DeleteLeaderboard(long id)
    {
        Leaderboard? lb = await applicationContext.Leaderboards.FindAsync(id);

        if (lb is null)
        {
            return new NotFound();
        }

        if (lb.DeletedAt is not null)
        {
            return new AlreadyDeleted();
        }

        lb.DeletedAt = clock.GetCurrentInstant();
        await applicationContext.SaveChangesAsync();
        return new Success();
    }

    public async Task<UpdateResult<Leaderboard>> UpdateLeaderboard(long id, UpdateLeaderboardRequest request)
    {
        Leaderboard? lb = await applicationContext.Leaderboards.FindAsync(id);

        if (lb is null)
        {
            return new NotFound();
        }

        if (request.Info is not null)
        {
            lb.Info = request.Info;
        }

        if (request.Name is not null)
        {
            lb.Name = request.Name;
        }

        if (request.Slug is not null)
        {
            lb.Slug = request.Slug;
        }

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            Leaderboard conflict = await applicationContext.Leaderboards.SingleAsync(c => c.Slug == lb.Slug && c.DeletedAt == null);
            return new Conflict<Leaderboard>(conflict);
        }

        return new Success();
    }
}
