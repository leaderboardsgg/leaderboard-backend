using System.Text.Json;
using LeaderboardBackend.Models;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneOf.Types;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext, IOptions<JsonOptions> jsonOptions) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs.Include(run => run.Category).SingleOrDefaultAsync(run => run.Id == id);

    public async Task<CreateRunResult> CreateRun(User user, long categoryId, JsonDocument request)
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

        if (c.Type == RunType.Time)
        {
            try
            {
                CreateTimedRunRequest? timed = request.Deserialize<CreateTimedRunRequest>(jsonOptions.Value.JsonSerializerOptions);

                if (timed == null)
                {
                    return new Unprocessable("Incorrect Request Body");
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
            catch (JsonException)
            {
                return new Unprocessable("Incorrect Request Body");
            }
        }

        try
        {
            CreateScoredRunRequest? scored = request.Deserialize<CreateScoredRunRequest>(jsonOptions.Value.JsonSerializerOptions);

            if (scored == null)
            {
                return new Unprocessable("Incorrect Request Body");
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
        catch (JsonException)
        {
            return new Unprocessable("Incorrect Request Body");
        }
    }
}
