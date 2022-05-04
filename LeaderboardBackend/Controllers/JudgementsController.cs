using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers;

public class JudgementsController : ControllerBase
{
	private readonly IJudgementService _judgementService;

	public JudgementsController(
		IJudgementService judgementService
	)
	{
		_judgementService = judgementService;
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

	public async Task<ActionResult<Judgement>> CreateJudgement([FromBody] CreateJudgementRequest body) {
		//
	}
}
