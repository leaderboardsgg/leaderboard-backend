using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Services;

public class RunService : IRunService
{
	private readonly ApplicationContext _applicationContext;

	public RunService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<List<Run>> GetRuns(Guid[] ids)
	{
		return await _applicationContext.Runs.Where(run => ids.Contains(run.Id)).ToListAsync();
	}

	public async Task<Run> Get(Guid id)
	{
		return await _applicationContext.Runs
			.SingleAsync(run => run.Id == id);
	}

	public async Task Create(Run run)
	{
		_applicationContext.Runs.Add(run);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task Delete(Run run)
	{
		run.DeletedAt = SystemClock.Instance.GetCurrentInstant();
		await _applicationContext.SaveChangesAsync();
	}
}
