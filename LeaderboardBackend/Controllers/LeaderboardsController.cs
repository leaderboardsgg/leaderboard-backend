using LeaderboardBackend.Authorization;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class LeaderboardsController(ILeaderboardService leaderboardService) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/leaderboard/{id:long}")]
    [SwaggerOperation("Gets a leaderboard by its ID.", OperationId = "getLeaderboard")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboard(long id)
    {
        Leaderboard? leaderboard = await leaderboardService.GetLeaderboard(id);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboard")]
    [SwaggerOperation("Gets a Leaderboard by its slug.", OperationId = "getLeaderboardBySlug")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboardBySlug([FromQuery, SwaggerParameter(Required = true)] string slug)
    {
        Leaderboard? leaderboard = await leaderboardService.GetLeaderboardBySlug(slug);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards")]
    [SwaggerOperation("Gets leaderboards by their IDs.", OperationId = "getLeaderboards")]
    [SwaggerResponse(200)]
    public async Task<ActionResult<List<LeaderboardViewModel>>> GetLeaderboards([FromQuery] long[] ids)
    {
        List<Leaderboard> result = Request.Query.ContainsKey("ids") ?
            await leaderboardService.GetLeaderboardsById(ids) :
            await leaderboardService.ListLeaderboards();

        return Ok(result.Select(LeaderboardViewModel.MapFrom));
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboards/create")]
    [SwaggerOperation("Creates a new leaderboard. This request is restricted to Administrators.", OperationId = "createLeaderboard")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create `Leaderboard`s.")]
    [SwaggerResponse(409, "A Leaderboard with the specified slug already exists.", typeof(ValidationProblemDetails))]
    [SwaggerResponse(422, $"The request contains errors. The following errors can occur: NotEmptyValidator, {SlugRule.SLUG_FORMAT}", Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<LeaderboardViewModel>> CreateLeaderboard(
        [FromBody, SwaggerRequestBody(Required = true)] CreateLeaderboardRequest request
    )
    {
        CreateLeaderboardResult r = await leaderboardService.CreateLeaderboard(request);

        return r.Match<ActionResult<LeaderboardViewModel>>(
            lb => CreatedAtAction(
                nameof(GetLeaderboard),
                new { id = lb.Id },
                LeaderboardViewModel.MapFrom(lb)
            ),
            conflict =>
            {
                ModelState.AddModelError(nameof(request.Slug), "SlugAlreadyUsed");

                return Conflict(new ValidationProblemDetails(ModelState));
            }
        );
    }
}
