using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RunsController : ControllerBase
	{
		private readonly IRunService _runService;
		public RunsController(
			IRunService runService
		)
		{
			_runService = runService;
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Run>> GetRun(long id)
		{
			Run? run = await _runService.GetRun(id);
			if (run == null)
			{
				return NotFound();
			}

			return run;
		}
	}
}
