using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs.Include(run => run.Category).SingleOrDefaultAsync(run => run.Id == id);

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
            case CreateTimedRunRequest timed: {
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

            case CreateScoredRunRequest scored: {
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
