using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ParticipationsController : ControllerBase
{
	private readonly IParticipationService ParticipationService;
	private readonly IRunService RunService;
	private readonly IUserService UserService;
	private readonly IAuthService AuthService;

	public ParticipationsController(
		IParticipationService participationService,
		IRunService runService,
		IUserService userService,
		IAuthService authService
	)
	{
		ParticipationService = participationService;
		RunService = runService;
		UserService = userService;
		AuthService = authService;
	}

	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.GetAnon))]
	[AllowAnonymous]
	[HttpGet("{id}")]
	public async Task<ActionResult<Participation>> GetParticipation(long id)
	{
		Participation? participation = await ParticipationService.GetParticipation(id);
		if (participation == null)
		{
			return NotFound();
		}

		return Ok(participation);
	}

	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Post))]
	[Authorize]
	[HttpPost]
	public async Task<ActionResult> CreateParticipation([FromBody] CreateParticipationRequest request)
	{
		// TODO: Maybe create validation middleware
		if (request.IsSubmitter && (request.Comment is null || request.Vod is null))
		{
			return BadRequest();
		}

		User? runner = await UserService.GetUserById(request.RunnerId);
		Run? run = await RunService.GetRun(request.RunId);

		if (runner is null || run is null)
		{
			return NotFound();
		}

		Participation participation = new()
		{
			Comment = request.Comment,
			RunId = request.RunId,
			Run = run,
			RunnerId = request.RunnerId,
			Runner = runner,
			Vod = request.Vod
		};

		await ParticipationService.CreateParticipation(participation);
		return CreatedAtAction(nameof(GetParticipation), new { id = participation.Id }, participation);
	}

	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Update))]
	[Authorize]
	[HttpPut]
	public async Task<ActionResult> UpdateParticipation([FromBody] UpdateParticipationRequest request)
	{
		Guid? userId = AuthService.GetUserIdFromClaims(HttpContext.User);
		if (userId is null)
		{
			return Forbid();
		}
		Participation? participation = await ParticipationService.GetParticipationForUser(userId.Value);
		if (participation is null)
		{
			return NotFound();
		}

		participation.Comment = request.Comment;
		participation.Vod = request.Vod;

		await ParticipationService.UpdateParticipation(participation);

		return Ok();
	}
}
