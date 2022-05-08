using LeaderboardBackend.Authorization;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace LeaderboardBackend.Controllers;

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
	[HttpGet("{id}")]
	public async Task<ActionResult<Judgement>> GetJudgement(long id)
	{
		Judgement? judgement = await _judgementService.GetJudgement(id);
		if (judgement is null)
		{
			return NotFound();
		}

		return Ok(judgement);
	}

	/// <summary>Creates a judgement for a run.</summary>
	/// <response code="201">The created judgement.</response>
	/// <response code="400">The request body is malformed.</response>
	/// <response code="404">For an invalid judgement.</response>
	/// <response code="500">If the client's User model cannot be retrieved for some reason.</response>
	[ApiConventionMethod(typeof(Conventions),
						nameof(Conventions.Post))]
	[ProducesResponseType(StatusCodes.Status500InternalServerError)]
	[Authorize(Policy = UserTypes.Mod)]
	[HttpPost("{id}")]
	public async Task<ActionResult<Judgement>> CreateJudgement([FromBody] CreateJudgementRequest body) {
		User? mod = await _userService.GetUserFromClaims(HttpContext.User);
		Run? run = await _runService.GetRun(body.RunId);

		if (mod is null)
		{
			// This shouldn't happen, as authZ should block already.
			_logger.LogError($"CreateJudgement: retrieved mod is null. Run ID = {body.RunId}");
			// FIXME: Return a 500 here instead. Dunno what the right function's called rn.
			return NotFound();
		}

		if (run is null)
		{
			_logger.LogError($"CreateJudgement: run is null. ID = {body.RunId}");
			return NotFound($"Run not found for ID = {body.RunId}");
		}

		Judgement judgement = new()
		{
			Approved = body.Approved,
			Mod = mod,
			ModId = mod.Id,
			Note = body.Note,
			Run = run,
			RunId = run.Id,
		};

		await _judgementService.CreateJudgement(judgement);

		return CreatedAtAction(nameof(GetJudgement), new { id = judgement.Id }, judgement);
	}
}
