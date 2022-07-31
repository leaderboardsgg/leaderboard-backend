using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IRunService
{
	Task<Run?> GetRun(Guid id);
	Task CreateRun(Run run);
}
