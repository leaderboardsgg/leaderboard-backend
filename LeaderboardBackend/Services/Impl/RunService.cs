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

    public async Task<CreateRunResult> CreateRun(User user, Category category, CreateRunRequest request)
    {
        switch (user.Role)
        {
            case UserRole.Banned:
            case UserRole.Registered:
                return new BadRole();
        }

        if (request is CreateTimedRunRequest timed)
        {
            if (category.Type != RunType.Time)
            {
                return new Unprocessable(
                    "A timed run submission request was received for a non-timed category. " +
                    """Ensure "runType" is set to "Time", and a "time" field is present."""
                );
            }

            Run run = new()
            {
                Category = category,
                Info = timed.Info,
                PlayedOn = timed.PlayedOn,
                Time = timed.Time,
                User = user,
            };
            applicationContext.Add(run);
            await applicationContext.SaveChangesAsync();
            return run;
        }

        if (request is CreateScoredRunRequest scored)
        {
            if (category.Type != RunType.Score)
            {
                return new Unprocessable(
                    "A scored run submission request was received for a non-scored category. " +
                    """Ensure "runType" is set to "Score", and a "score" field is present."""
                );
            }

            Run run = new()
            {
                Category = category,
                Info = scored.Info,
                PlayedOn = scored.PlayedOn,
                TimeOrScore = scored.Score,
                User = user,
            };
            applicationContext.Add(run);
            await applicationContext.SaveChangesAsync();
            return run;
        }

        return new Unprocessable("Invalid run submission request received.");
    }
}
