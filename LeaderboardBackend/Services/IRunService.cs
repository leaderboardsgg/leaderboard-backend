using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public interface IRunService
	{
		Task<Run?> GetRun(long id);
	}
}
