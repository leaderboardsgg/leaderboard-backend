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

        return request.Match<CreateRunResult>(
            timed =>
            {
                if (c.Type != RunType.Time)
                {
                    return new Unprocessable(
                        "A timed run request was passed to a non-timed category. " +
                        "Remove the 'time' field, and only include necessary fields."
                    );
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
            },
            scored =>
            {
                if (c.Type != RunType.Score)
                {
                    return new Unprocessable(
                        "A scored run request was passed to a non-scoring category. " +
                        "Remove the 'score' field, and only include necessary fields.");
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
        );
    }
}
