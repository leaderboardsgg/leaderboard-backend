using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests.Participations;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers;

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
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesDefaultResponseType]
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
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
	[ProducesDefaultResponseType]
	public async Task<ActionResult> CreateParticipation([FromBody] CreateParticipationRequest request)
	{
		// TODO: Maybe create validation middleware
		if (request.IsSubmitter && (request.Comment == null || request.Vod == null))
		{
			return BadRequest();
		}

		User? runner = await _userService.GetUserById(request.RunnerId);
		Run? run = await _runService.GetRun(request.RunId);

		if (runner == null)
		{
			if (request.IsSubmitter)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, "We can't add your participation because we can't.. find.. your ID? On our end???");
			}
			return StatusCode(StatusCodes.Status500InternalServerError, "We can't find the user with the provided ID on our end.");
		}

		if (run == null)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, "We can't find the associated run on our end.");
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
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status200OK)]
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
