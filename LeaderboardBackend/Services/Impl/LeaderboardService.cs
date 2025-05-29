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

    public async Task<ListResult<Leaderboard>> ListLeaderboards(StatusFilter statusFilter, Page page)
    {
        IQueryable<Leaderboard> query = applicationContext.Leaderboards.FilterByStatus(statusFilter);
        long count = await query.LongCountAsync();

        // Ordering by ID is necessary, otherwise pagination breaks completely because the records won't
        // be returned in a specific order.

        List<Leaderboard> items = await query.OrderBy(lb => lb.Id).Skip(page.Offset).Take(page.Limit).ToListAsync();
        return new ListResult<Leaderboard>(items, count);
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
            Leaderboard conflicting = await applicationContext.Leaderboards.SingleAsync(l => l.Slug == request.Slug && l.DeletedAt == null);
            return new Conflict<Leaderboard>(conflicting);
        }

        return lb;
    }

    public async Task<RestoreResult<Leaderboard>> RestoreLeaderboard(long id)
    {
        Leaderboard? lb = await applicationContext.Leaderboards.FindAsync(id);

        if (lb == null)
        {
            return new NotFound();
        }

        if (lb.DeletedAt == null)
        {
            return new NeverDeleted();
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
    public async Task<ListResult<Leaderboard>> SearchLeaderboards(string query, StatusFilter statusFilter, Page page)
    {
        IQueryable<Leaderboard> dbQuery = applicationContext.Leaderboards.FilterByStatus(statusFilter).Search(query);
        long count = await dbQuery.LongCountAsync();

        List<Leaderboard> items = await dbQuery
            .Rank(query)
            .Skip(page.Offset)
            .Take(page.Limit)
            .ToListAsync();

        return new ListResult<Leaderboard>(items, items.Count);
    }
}
