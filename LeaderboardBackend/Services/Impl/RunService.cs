using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class RunService : IRunService
{
	private readonly ApplicationContext ApplicationContext;

	public RunService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<Run?> GetRun(Guid id)
	{
		return await ApplicationContext.Runs.FindAsync(id);
	}

	public async Task CreateRun(Run run)
	{
		ApplicationContext.Runs.Add(run);
		await ApplicationContext.SaveChangesAsync();
	}
}
