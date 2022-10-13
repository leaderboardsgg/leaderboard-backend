using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Services;

public class NumericalMetricService : INumericalMetricService
{
	private readonly ApplicationContext _applicationContext;
	public NumericalMetricService(ApplicationContext applicationContext)
	{
		_applicationContext = applicationContext;
	}

	public async Task<NumericalMetric> Get(long id)
	{
		NumericalMetric metric = await _applicationContext.NumericalMetrics
			.FirstAsync(metric => metric.Id == id);

		await _applicationContext.Entry(metric)
			.Collection(m => m.Categories)
			.LoadAsync();

		await _applicationContext.Entry(metric)
			.Collection(m => m.Runs)
			.LoadAsync();

		return metric;
	}

	public async Task Create(NumericalMetric numericalMetric)
	{
		_applicationContext.NumericalMetrics.Add(numericalMetric);
		await _applicationContext.SaveChangesAsync();
	}

	public async Task Delete(NumericalMetric metric)
	{
		metric.DeletedAt = SystemClock.Instance.GetCurrentInstant();
		await _applicationContext.SaveChangesAsync();
	}
}
