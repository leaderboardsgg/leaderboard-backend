using LeaderboardBackend.Authorization;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using LeaderboardBackend.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class JudgementsController : ControllerBase
{
	private readonly ILogger _logger;
	private readonly IJudgementService _judgementService;
	private readonly IRunService _runService;
	private readonly IUserService _userService;

	public JudgementsController(
		ILogger<JudgementsController> logger,
		IJudgementService judgementService,
		IRunService runService,
		IUserService userService
	)
	{
		_logger = logger;
		_judgementService = judgementService;
		_runService = runService;
		_userService = userService;
	}

	/// <summary>Gets a Judgement from its ID.</summary>
	/// <response code="200">The Judgement with the provided ID.</response>
	/// <response code="404">If no Judgement can be found.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	[AllowAnonymous]
	[HttpGet("{id}")]
	public async Task<ActionResult<JudgementViewModel>> GetJudgement(long id)
	{
		Judgement? judgement = await _judgementService.GetJudgement(id);
		if (judgement is null)
		{
			return NotFound();
		}

		return Ok(new JudgementViewModel(judgement));
	}

	/// <summary>Creates a judgement for a run.</summary>
	/// <response code="201">The created judgement.</response>
	/// <response code="400">The request body is malformed.</response>
	/// <response code="403">The run has pending participations.</response>
	/// <response code="404">For an invalid judgement.</response>
	[ApiConventionMethod(typeof(Conventions),
						nameof(Conventions.Post))]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[Authorize(Policy = UserTypes.Mod)]
	[HttpPost]
	public async Task<ActionResult<JudgementViewModel>> CreateJudgement([FromBody] CreateJudgementRequest body)
	{
		User? mod = await _userService.GetUserFromClaims(HttpContext.User);
		Run? run = await _runService.GetRun(body.RunId);

		if (run is null)
		{
			_logger.LogError($"CreateJudgement: run is null. ID = {body.RunId}");
			return NotFound($"Run not found for ID = {body.RunId}");
		}

		if (run.Status == RunStatus.CREATED)
		{
			_logger.LogError($"CreateJudgement: run has pending participations (i.e. run status == CREATED). ID = {body.RunId}");
			return Forbid($"Run has pending Participations. ID = {body.RunId}");
		}

		// TODO: Update run status on body.Approved's value
		Judgement judgement = new()
		{
			Approved = body.Approved,
			Mod = mod!,
			ModId = mod!.Id,
			Note = body.Note,
			Run = run,
			RunId = run.Id,
		};

		await _judgementService.CreateJudgement(judgement);

		return CreatedAtAction(nameof(GetJudgement), new JudgementViewModel(judgement));
	}
}
