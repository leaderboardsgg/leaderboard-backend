using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Services;

public interface IRunService : IBaseService<Run, Guid>
{
	Task<List<Run>> GetRuns(Guid[] ids);
}
