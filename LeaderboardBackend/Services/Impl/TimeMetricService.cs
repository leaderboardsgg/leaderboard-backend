using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public class TimeMetricService : ITimeMetricService
{
	private readonly ApplicationContext ApplicationContext;

	public TimeMetricService(ApplicationContext applicationContext)
	{
		ApplicationContext = applicationContext;
	}

	public async Task<TimeMetric?> GetTimeMetric(long id)
	{
		return await ApplicationContext.TimeMetrics.FindAsync(id);
	}

	public async Task CreateTimeMetric(TimeMetric metric)
	{
		ApplicationContext.TimeMetrics.Add(metric);
		await ApplicationContext.SaveChangesAsync();
	}
}
