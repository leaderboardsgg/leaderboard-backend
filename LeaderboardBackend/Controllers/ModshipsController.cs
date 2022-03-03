using LeaderboardBackend.Controllers.Requests;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ModshipsController : ControllerBase
{
	private readonly IModshipService _modshipService;

	public ModshipsController(IModshipService modshipService)
	{
		_modshipService = modshipService;
	}

	[Authorize]
	[HttpGet("{id}")]
	public async Task<ActionResult<Modship>> GetModship(Guid id)
	{
		Modship? modship = await _modshipService.GetModship(id);

		if (modship == null)
		{
			return NotFound();
		}

		return Ok(modship);
	}

	[Authorize]
	[HttpPost]
	public async Task<ActionResult<Leaderboard>> MakeMod([FromBody] ModshipRequest body)
	{
		Modship modship = new()
		{
			LeaderboardId = body.LeaderboardId,
			UserId = body.UserId
		};

		await _modshipService.CreateModship(modship);
		return CreatedAtAction(nameof(MakeMod), new { id = modship.Id }, modship);
	}
}
