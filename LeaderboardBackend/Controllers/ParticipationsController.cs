using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ParticipationsController : ControllerBase
	{
		private readonly IParticipationService _participationService;

		public ParticipationsController(IParticipationService participationService)
		{
			_participationService = participationService;
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Participation>> GetParticipation(long id)
		{
			Participation? participation = await _participationService.GetParticipation(id);
			if (participation == null)
			{
				return NotFound();
			}

			return Ok(participation);
		}
	}
}
