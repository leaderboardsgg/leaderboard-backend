using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs.Include(run => run.Category).SingleOrDefaultAsync(run => run.Id == id);

    public async Task<CreateRunResult> CreateRun(User user, long categoryId, CreateRunRequest request)
    {
        switch (user.Role)
        {
            case UserRole.Banned:
            case UserRole.Registered:
                return new BadRole();
        }

        Category? c = await applicationContext.FindAsync<Category>(categoryId);

        if (c == null)
        {
            return new NotFound();
        }

        switch (request)
        {
            case CreateTimedRunRequest timed:
            {
                if (c.Type != RunType.Time)
                {
                    return new Unprocessable();
                }

                Run run = new()
                {
                    Category = c,
                    Info = timed.Info ?? "",
                    PlayedOn = timed.PlayedOn,
                    Time = timed.Time,
                    User = user,
                };
                applicationContext.Add(run);
                applicationContext.SaveChanges();
                return run;
            }
            case CreateScoredRunRequest scored:
            {
                if (c.Type != RunType.Score)
                {
                    return new Unprocessable();
                }

                Run run = new()
                {
                    Category = c,
                    Info = scored.Info ?? "",
                    PlayedOn = scored.PlayedOn,
                    TimeOrScore = scored.Score,
                    User = user,
                };
                applicationContext.Add(run);
                applicationContext.SaveChanges();
                return run;
            }
            default:
            {
                Console.WriteLine("It's here isn't it");
                return new Unprocessable();
            }
        }
    }
}
