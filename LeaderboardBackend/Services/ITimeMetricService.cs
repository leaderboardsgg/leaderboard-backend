using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface ITimeMetricService
{
	Task<TimeMetric?> GetTimeMetric(long id);

	Task CreateTimeMetric(TimeMetric metric);
}
