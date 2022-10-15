using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Services;

public class IntegerMetricService : IIntegerMetricService
{
	private readonly ApplicationContext _applicationContext;
	public IntegerMetricService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<IntegerMetric> Get(long id)
	{
		IntegerMetric metric = await _applicationContext.IntegerMetrics
			.FirstAsync(metric => metric.Id == id);

		await _applicationContext.Entry(metric)
			.Collection(m => m.Categories)
			.LoadAsync();

		await _applicationContext.Entry(metric)
			.Collection(m => m.Runs)
			.LoadAsync();

		return metric;
	}

	public async Task Create(IntegerMetric integerMetric)
	{
		_applicationContext.IntegerMetrics.Add(integerMetric);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task Delete(IntegerMetric metric)
	{
		metric.DeletedAt = SystemClock.Instance.GetCurrentInstant();
		await _applicationContext.SaveChangesAsync();
	}
}
