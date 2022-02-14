using LeaderboardBackend.Models;

namespace LeaderboardBackend.Services
{
	public class RunService : IRunService
	{
		private ApplicationContext _applicationContext;
		public RunService(ApplicationContext applicationContext, IConfiguration config)
		{
			_applicationContext = applicationContext;
		}

		public async Task<Run?> GetRun(long id)
		{
			return await _applicationContext.Runs.FindAsync(id);
		}
	}
}
