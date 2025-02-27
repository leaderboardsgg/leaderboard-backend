using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class RunService(ApplicationContext applicationContext) : IRunService
{
    public async Task<Run?> GetRun(Guid id) =>
        await applicationContext.Runs.Include(run => run.Category).SingleOrDefaultAsync(run => run.Id == id);

    public async Task CreateRun(Run run)
    {
        applicationContext.Runs.Add(run);
        await applicationContext.SaveChangesAsync();
        applicationContext.Entry(run).Reference(r => r.Category).Load();
    }
}
