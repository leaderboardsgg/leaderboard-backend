using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs.Include(run => run.Category).SingleOrDefaultAsync(run => run.Id == id);

    public async Task<GetRunsForCategoryResult> GetRunsForCategory(
        long id,
        Page page,
        bool includeDeleted = false
    )
    {
        Category? cat = await applicationContext.FindAsync<Category>(id);
        if (cat is null)
        {
            return new NotFound();
        }

        if (cat.DeletedAt is not null)
        {
            return new AlreadyDeleted();
        }

        IQueryable<Run> query = applicationContext.Runs
            .Include(run => run.Category)
            .Where(run =>
                run.CategoryId == id && (includeDeleted || run.DeletedAt == null)
            );

        long count = await query.LongCountAsync();

        if (cat.SortDirection == SortDirection.Descending)
        {
            query.OrderByDescending(run => run.TimeOrScore);
        }
        else
        {
            query.OrderBy(run => run.TimeOrScore);
        }

        List<Run> items = await query.OrderBy(run => run.CreatedAt)
            .Skip(page.Offset)
            .Take(page.Limit)
            .ToListAsync();

        return new ListResult<Run>(items, count);
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
}
