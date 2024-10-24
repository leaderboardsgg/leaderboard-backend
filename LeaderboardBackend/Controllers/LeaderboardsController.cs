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
    [SwaggerOperation("Gets a leaderboard by its slug.", OperationId = "getLeaderboardBySlug")]
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
    [SwaggerOperation("Gets all leaderboards.", OperationId = "listLeaderboards")]
    [SwaggerResponse(200)]
    public async Task<ActionResult<List<LeaderboardViewModel>>> GetLeaderboards()
    {
        // TODO: Paginate.

        List<Leaderboard> result = await leaderboardService.ListLeaderboards();
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

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPut("leaderboard/{id:long}/restore")]
    [SwaggerOperation("Restores a deleted leaderboard.", OperationId = "restoreLeaderboard")]
    [SwaggerResponse(200, "The restored `Leaderboard`s view model.", typeof(LeaderboardViewModel))]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to restore `Leaderboard`s.")]
    [SwaggerResponse(404, "The `Leaderboard` was not found, or it wasn't deleted in the first place. Includes a field, `title`, which will be \"Not Found\" in the former case, and \"Not Deleted\" in the latter.", typeof(ProblemDetails))]
    [SwaggerResponse(409, "Another `Leaderboard` with the same slug has been created since, and therefore can't be restored. Will include the conflicting board in the response.", typeof(LeaderboardViewModel))]
    public async Task<ActionResult<LeaderboardViewModel>> RestoreLeaderboard(
        long id
    )
    {
        RestoreLeaderboardResult r = await leaderboardService.RestoreLeaderboard(id);

        return r.Match<ActionResult<LeaderboardViewModel>>(
            board => Ok(LeaderboardViewModel.MapFrom(board)),
            notFound => NotFound(),
            neverDeleted =>
                NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 404, "Not Deleted")),
            conflict => Conflict(LeaderboardViewModel.MapFrom(conflict.Board))
        );
    }
}
