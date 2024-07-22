using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeaderboardBackend.Services;

public class RunService : IRunService
{
    private readonly ApplicationContext _applicationContext;

    public RunService(ApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<Run?> GetRun(Guid id) =>
        await _applicationContext.Runs.Include(run => run.Category).SingleAsync(run => run.Id == id);

    public async Task CreateRun(Run run)
    {
        _applicationContext.Runs.Add(run);
        await _applicationContext.SaveChangesAsync();
        _applicationContext.Entry(run).Reference(r => r.Category).Load();
    }
}
