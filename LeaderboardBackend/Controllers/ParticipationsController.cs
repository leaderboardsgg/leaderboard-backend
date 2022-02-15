using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using LeaderboardBackend.Controllers.Requests;
using Microsoft.AspNetCore.Authorization;

namespace LeaderboardBackend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ParticipationsController : ControllerBase
	{
		private readonly IParticipationService _participationService;
		private readonly IRunService _runService;
		private readonly IUserService _userService;

		public ParticipationsController(
			IParticipationService participationService,
			IRunService runService,
			IUserService userService
		)
		{
			_participationService = participationService;
			_runService = runService;
			_userService = userService;
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

		[HttpPost]
		public async Task<ActionResult> CreateParticipation([FromBody] CreateParticipationRequest request)
		{
			// TODO: Maybe create validation middleware
			if (request.IsRunner && (request.Comment == null || request.Vod == null))
			{
				return BadRequest("You must provide a comment and a VoD link, as you're the submitter.");
			}

			var runner = await _userService.GetUser(request.RunnerId);
			var run = await _runService.GetRun(request.RunId);

			if (runner == null)
			{
				if (request.IsRunner)
				{
					return BadRequest("We can't add your participation because we can't.. find.. your ID? On our end???");
				}
				return BadRequest("We can't find the user with the provided ID on our end.");
			}

			if (run == null)
			{
				return BadRequest("We can't find the associated run on our end.");
			}

			Participation participation = new Participation
			{
				Comment = request.Comment,
				RunId = request.RunId,
				Run = run,
				RunnerId = request.RunnerId,
				Runner = runner,
				Vod = request.Vod
			};

			await _participationService.CreateParticipation(participation);
			return CreatedAtAction(nameof(CreateParticipation), new { id = participation.Id });
		}

		[Authorize]
		[HttpPut]
		public async Task<ActionResult> UpdateParticipation([FromBody] UpdateParticipationRequest request)
		{
			User? user = await _userService.GetUserFromClaims(HttpContext.User);
			if (user == null)
			{
				return Forbid();
			}
			Participation? participation = await _participationService.GetParticipationForUser(user);
			if (participation == null)
			{
				return NotFound();
			}

			participation.Comment = request.Comment;
			participation.Vod = request.Vod;

			await _participationService.UpdateParticipation(participation);

			return Ok();
		}
	}
}
