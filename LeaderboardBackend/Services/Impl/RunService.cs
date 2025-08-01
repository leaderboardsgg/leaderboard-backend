using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using OneOf.Types;
using Zomp.EFCore.WindowFunctions;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext, IClock clock) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs
            .Include(run => run.Category)
            .Include(run => run.User)
            .SingleOrDefaultAsync(run => run.Id == id);

    public async Task<GetRunsForCategoryResult> GetRunsForCategory(
        long id,
        StatusFilter statusFilter,
        Page page
    )
    {
        Category? cat = await applicationContext.FindAsync<Category>(id);
        if (cat is null)
        {
            return new NotFound();
        }

        IQueryable<Run> query = applicationContext.Runs
            .Include(run => run.Category)
            .Include(run => run.User)
            .FilterByStatus(statusFilter);

        long count = await query.LongCountAsync();

        if (cat.SortDirection == SortDirection.Descending)
        {
            query = query.OrderByDescending(run => run.TimeOrScore)
                .ThenBy(run => run.PlayedOn)
                .ThenBy(run => run.CreatedAt);
        }
        else
        {
            query = query.OrderBy(run => run.TimeOrScore)
                .ThenBy(run => run.PlayedOn)
                .ThenBy(run => run.CreatedAt);
        }

        List<Run> items = await query.Skip(page.Offset)
            .Take(page.Limit)
            .ToListAsync();

        return new ListResult<Run>(items, count);
    }

    public async Task<GetRecordsForCategoryResult> GetRecordsForCategory(
        long id,
        Page page
    )
    {
        Category? cat = await applicationContext.FindAsync<Category>(id);

        if (cat is null)
        {
            return new NotFound();
        }

        bool asc = cat.SortDirection == SortDirection.Ascending;

        // Query the best run from each user.

        IQueryable<RankedRun> query = applicationContext.Runs
            .Include(r => r.Category)
            .Include(r => r.User)
            .Where(r => r.CategoryId == id && r.DeletedAt == null)
            .Where(r => EF.Functions.RowNumber((
                asc ?
                EF.Functions.Over().PartitionBy(r.UserId).OrderBy(r.TimeOrScore) :
                EF.Functions.Over().PartitionBy(r.UserId).OrderByDescending(r.TimeOrScore)
            ).ThenBy(r.PlayedOn).ThenBy(r.CreatedAt)) == 1L)

        // Assign each run a rank relative to the other users' best runs and count them up.

            .Select(r => new RankedRun
            {
                Rank = EF.Functions.Rank(
                    asc ?
                    EF.Functions.Over().OrderBy(r.TimeOrScore) :
                    EF.Functions.Over().OrderByDescending(r.TimeOrScore)),
                Run = r,
                Count = EF.Functions.Count(EF.Functions.Over())
            });

        // Break ties and apply pagination.

        IOrderedQueryable<RankedRun> runs;
        if (asc)
        {
            runs = query.OrderBy(r => r.Run.TimeOrScore);
        }
        else
        {
            runs = query.OrderByDescending(r => r.Run.TimeOrScore);
        }

        List<RankedRun> records = await runs
            .ThenBy(r => r.Run.PlayedOn)
            .ThenBy(r => r.Run.CreatedAt)
            .ThenBy(r => r.Run.Id)
            .Skip(page.Offset)
            .Take(page.Limit)
            .ToListAsync();

        return new ListResult<RankedRun>(records, records.FirstOrDefault()?.Count ?? 0L);
    }

    public async Task<CreateRunResult> CreateRun(User user, Category category, CreateRunRequest request)
    {
        switch (user.Role)
        {
            case UserRole.Banned:
            case UserRole.Registered:
                return new BadRole();
        }

        Run run;

        switch (request)
        {
            case CreateTimedRunRequest timed:
            {
                if (category.Type != RunType.Time)
                {
                    return new BadRunType();
                }

                run = new()
                {
                    Category = category,
                    Info = timed.Info,
                    PlayedOn = timed.PlayedOn,
                    Time = timed.Time,
                    User = user,
                };

                break;
            }

            case CreateScoredRunRequest scored:
            {
                if (category.Type != RunType.Score)
                {
                    return new BadRunType();
                }

                run = new()
                {
                    Category = category,
                    Info = scored.Info,
                    PlayedOn = scored.PlayedOn,
                    TimeOrScore = scored.Score,
                    User = user,
                };

                break;
            }

            default:
                throw new ArgumentException("Invalid request type: not one of [CreateTimedRunRequest, CreateScoredRunRequest]", nameof(request));
        }

        applicationContext.Add(run);
        await applicationContext.SaveChangesAsync();
        return run;
    }

    public async Task<UpdateRunResult> UpdateRun(User user, Guid id, UpdateRunRequest request)
    {
        Run? run = await applicationContext.Runs
            .Include(run => run.Category)
            .ThenInclude(cat => cat.Leaderboard)
            .Where(run => run.Id == id)
            .SingleOrDefaultAsync();

        if (run is null)
        {
            return new NotFound();
        }

        switch (user.Role)
        {
            case UserRole.Confirmed:
            {
                if (request.Status != null)
                {
                    return new UserCannotChangeStatusOfRuns();
                }

                if (run.UserId != user.Id)
                {
                    return new UserDoesNotOwnRun();
                }

                if (run.DeletedAt != null)
                {
                    return new AlreadyDeleted();
                }

                if (run.Category.DeletedAt != null)
                {
                    return new AlreadyDeleted(typeof(Category));
                }

                if (run.Category.Leaderboard!.DeletedAt != null)
                {
                    return new AlreadyDeleted(typeof(Leaderboard));
                }

                break;
            }
            case UserRole.Administrator:
                break;
            default:
                return new BadRole();
        }

        if (request.Info is not null)
        {
            run.Info = request.Info;
        }

        if (request.PlayedOn is not null)
        {
            run.PlayedOn = (LocalDate)request.PlayedOn;
        }

        switch (request.Status)
        {
            case null:
                break;

            case Status.Published:
            {
                run.DeletedAt = null;
                break;
            }

            case Status.Deleted:
            {
                run.DeletedAt = clock.GetCurrentInstant();
                break;
            }

            default:
                throw new ArgumentException($"Invalid Status in request: {(int)request.Status}", nameof(request));
        }

        switch (request)
        {
            case UpdateTimedRunRequest timed:
            {
                if (run.Category.Type != RunType.Time)
                {
                    return new BadRunType();
                }

                if (timed.Time is not null)
                {
                    run.Time = (Duration)timed.Time;
                }

                break;
            }
            case UpdateScoredRunRequest scored:
            {
                if (run.Category.Type != RunType.Score)
                {
                    return new BadRunType();
                }

                if (scored.Score is not null)
                {
                    run.TimeOrScore = (long)scored.Score;
                }

                break;
            }
        }

        await applicationContext.SaveChangesAsync();
        return new Success();
    }

    public async Task<DeleteResult> DeleteRun(Guid id)
    {
        Run? run = await applicationContext.FindAsync<Run>(id);

        if (run is null)
        {
            return new NotFound();
        }

        if (run.DeletedAt is not null)
        {
            return new AlreadyDeleted();
        }

        run.DeletedAt = clock.GetCurrentInstant();
        await applicationContext.SaveChangesAsync();
        return new Success();
    }
}
