using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class RunService : IRunService
{
	private readonly ApplicationContext _applicationContext;

	public RunService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<Run?> GetRun(Guid id)
	{
		return await _applicationContext.Runs.FindAsync(id);
	}

	public async Task CreateRun(Run run)
	{
		_applicationContext.Runs.Add(run);
		await _applicationContext.SaveChangesAsync();
	}
}
