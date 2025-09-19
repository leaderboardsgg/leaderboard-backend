using System.ComponentModel;
using LeaderboardBackend.Models;
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
    public async Task<LeaderboardWithStats?> GetLeaderboard(long id) =>
        await applicationContext.Leaderboards.WithStats().FirstOrDefaultAsync(lb => lb.Leaderboard.Id == id);

    public async Task<LeaderboardWithStats?> GetLeaderboardBySlug(string slug) =>
        await applicationContext.Leaderboards
            .WithStats()
            .FirstOrDefaultAsync(b => b.Leaderboard.Slug == slug && b.Leaderboard.DeletedAt == null);

    public async Task<ListResult<LeaderboardWithStats>> ListLeaderboards(StatusFilter statusFilter, Page page, SortLeaderboardsBy sortBy)
    {
        IQueryable<LeaderboardWithStats> query = applicationContext.Leaderboards.FilterByStatus(statusFilter).WithStatsAndCount();

        query = sortBy switch
        {
            SortLeaderboardsBy.Name_Asc => query.OrderBy(lb => lb.Leaderboard.Name),
            SortLeaderboardsBy.Name_Desc => query.OrderByDescending(lb => lb.Leaderboard.Name),
            SortLeaderboardsBy.CreatedAt_Asc => query.OrderBy(lb => lb.Leaderboard.CreatedAt),
            SortLeaderboardsBy.CreatedAt_Desc => query.OrderByDescending(lb => lb.Leaderboard.CreatedAt),
            _ => throw new InvalidEnumArgumentException(nameof(SortLeaderboardsBy), (int)sortBy, typeof(SortLeaderboardsBy)),
        };

        return await query
            .Skip(page.Offset)
            .Take(page.Limit)
            .ToListResult();
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

    public async Task<RestoreResult<LeaderboardWithStats>> RestoreLeaderboard(long id)
    {
        LeaderboardWithStats? lb = await applicationContext.Leaderboards.WithStats().FirstOrDefaultAsync(lb => lb.Leaderboard.Id == id);

        if (lb == null)
        {
            return new NotFound();
        }

        if (lb.Leaderboard.DeletedAt == null)
        {
            return new NeverDeleted();
        }

        lb.Leaderboard.DeletedAt = null;

        try
        {
            await applicationContext.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            LeaderboardWithStats conflict = await applicationContext.Leaderboards.WithStats().SingleAsync(
                c => c.Leaderboard.Slug == lb.Leaderboard.Slug && c.Leaderboard.DeletedAt == null
            );

            return new Conflict<LeaderboardWithStats>(conflict);
        }

        return lb;
    }

    public async Task<DeleteResult> DeleteLeaderboard(long id)
    {
        // TODO: Use ExecuteUpdate instead of fetching and saving, possibly using
        // RETURNING to check old value.
        // - Ted W

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

        switch (request.Status)
        {
            case null:
                break;

            case Status.Published:
            {
                lb.DeletedAt = null;
                break;
            }

            case Status.Deleted:
            {
                lb.DeletedAt = clock.GetCurrentInstant();
                break;
            }

            default:
            {
                throw new ArgumentException($"Invalid Status in request: {(int)request.Status}", nameof(request));
            }
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

    public async Task<ListResult<LeaderboardWithStats>> SearchLeaderboards(string query, StatusFilter statusFilter, Page page) =>
        await applicationContext.Leaderboards
            .FilterByStatus(statusFilter)
            .Search(query)
            .Rank(query)
            .Skip(page.Offset)
            .Take(page.Limit)
            .WithStatsAndCount()
            .ToListResult();
}
