using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class LeaderboardsController : ApiController
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardsController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [AllowAnonymous]
    [HttpGet("{id:long}")]
    [SwaggerOperation("Gets a leaderboard by its ID.")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboard(long id)
    {
        Leaderboard? leaderboard = await _leaderboardService.GetLeaderboard(id);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    [SwaggerOperation("Gets a Leaderboard by its slug.")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboardBySlug(string slug)
    {
        Leaderboard? leaderboard = await _leaderboardService.GetLeaderboardBySlug(slug);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet]
    [SwaggerOperation("Gets leaderboards by their IDs.")]
    [SwaggerResponse(200)]
    public async Task<ActionResult<List<LeaderboardViewModel>>> GetLeaderboards(
        [FromQuery] long[] ids
    )
    {
        List<Leaderboard> result = await _leaderboardService.GetLeaderboards(ids);
        return Ok(result.Select(LeaderboardViewModel.MapFrom));
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost]
    [SwaggerOperation("Creates a new leaderboard. This request is restricted to Administrators.")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create `Leaderboard`s.")]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<LeaderboardViewModel>> CreateLeaderboard(
        [FromBody] CreateLeaderboardRequest request
    )
    {
        Leaderboard leaderboard = new() { Name = request.Name, Slug = request.Slug };

        await _leaderboardService.CreateLeaderboard(leaderboard);

        return CreatedAtAction(
            nameof(GetLeaderboard),
            new { id = leaderboard.Id },
            LeaderboardViewModel.MapFrom(leaderboard)
        );
    }
}
