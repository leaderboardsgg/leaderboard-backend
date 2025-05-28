using System.Net;
using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.Validation;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class LeaderboardsController(
    ILeaderboardService leaderboardService
) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/leaderboard/{id:long}")]
    [SwaggerOperation("Gets a leaderboard by its ID.", OperationId = "getLeaderboard")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboard([FromRoute] long id)
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
    [SwaggerOperation("Gets a leaderboard by its slug. Will not return deleted boards.", OperationId = "getLeaderboardBySlug")]
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
    [Paginated]
    [SwaggerOperation("Gets all leaderboards.", OperationId = "listLeaderboards")]
    [SwaggerResponse(200)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ListView<LeaderboardViewModel>>> GetLeaderboards([FromQuery] Page page, [FromQuery] StatusFilter status = StatusFilter.Published)
    {
        ListResult<Leaderboard> result = await leaderboardService.ListLeaderboards(status, page);
        return Ok(new ListView<LeaderboardViewModel>()
        {
            Data = result.Items.Select(LeaderboardViewModel.MapFrom).ToList(),
            Total = result.ItemsTotal
        });
    }

    [AllowAnonymous]
    [HttpGet("/api/leaderboards/search")]
    [Paginated]
    [SwaggerOperation("Search leaderboards by name or slug.", OperationId = "searchLeaderboards")]
    [SwaggerResponse(200, Type = typeof(ListView<LeaderboardViewModel>))]
    [SwaggerResponse(422, Type = typeof(ProblemDetails))]
    public async Task<ActionResult<ListView<LeaderboardViewModel>>> SearchLeaderboards(
        [
            FromQuery(Name = "q"),
            SwaggerParameter("The query string. Must not be empty.", Required = true)
        ] string query,
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Problem(null, null, 422, "Empty Query");
        }

        ListResult<Leaderboard> result = await leaderboardService.SearchLeaderboards(query, status, page);

        return Ok(new ListView<LeaderboardViewModel>()
        {
            Data = result.Items.Select(LeaderboardViewModel.MapFrom).ToList(),
            Total = result.ItemsTotal
        });
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboards/create")]
    [SwaggerOperation("Creates a new leaderboard. This request is restricted to Administrators.", OperationId = "createLeaderboard")]
    [SwaggerResponse(201)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403, "The requesting `User` is unauthorized to create `Leaderboard`s.")]
    [SwaggerResponse(409, "A Leaderboard with the specified slug already exists and will be returned in the `conflicting` field.", typeof(ConflictDetails<LeaderboardViewModel>))]
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
                ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                problemDetails.Extensions.Add("conflicting", LeaderboardViewModel.MapFrom(conflict.Conflicting));
                return Conflict(problemDetails);
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
    [SwaggerResponse(409, "Another `Leaderboard` with the same slug has been created since and will be returned in the `conflicting` field, and therefore can't be restored.", typeof(ConflictDetails<LeaderboardViewModel>))]
    public async Task<ActionResult<LeaderboardViewModel>> RestoreLeaderboard(
        [FromRoute] long id
    )
    {
        RestoreResult<Leaderboard> r = await leaderboardService.RestoreLeaderboard(id);

        return r.Match<ActionResult<LeaderboardViewModel>>(
            board => Ok(LeaderboardViewModel.MapFrom(board)),
            notFound => NotFound(),
            neverDeleted => Problem(
                null,
                null,
                404,
                "Not Deleted"
            ),
            conflict =>
            {
                ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                problemDetails.Extensions.Add("conflicting", LeaderboardViewModel.MapFrom(conflict.Conflicting));
                return Conflict(problemDetails);
            }
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpDelete("leaderboard/{id:long}")]
    [SwaggerOperation("Deletes a leaderboard. This request is restricted to Administrators.", OperationId = "deleteLeaderboard")]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(
        404,
        """
        The leaderboard does not exist (Not Found) or was already deleted (Already Deleted).
        Use the title field of the response to differentiate between the two cases if necessary.
        """,
        typeof(ProblemDetails)
    )]
    public async Task<ActionResult> DeleteLeaderboard([FromRoute] long id)
    {
        DeleteResult res = await leaderboardService.DeleteLeaderboard(id);

        return res.Match<ActionResult>(
            success => NoContent(),
            notFound => NotFound(),
            alreadyDeleted => Problem(
                null,
                null,
                404,
                "Already Deleted"
            )
        );
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPatch("/leaderboard/{id:long}")]
    [SwaggerOperation(
        "Updates a leaderboard with the specified new fields. This request is restricted to administrators. " +
        "This operation is atomic; if an error occurs, the leaderboard will not be updated. " +
        "All fields of the request body are optional but you must specify at least one.",
        OperationId = "updateLeaderboard"
    )]
    [SwaggerResponse(204)]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    [SwaggerResponse(404, Type = typeof(ProblemDetails))]
    [SwaggerResponse(
        409,
        "The specified slug is already in use by another leaderboard. Returns the conflicting leaderboard.",
        typeof(LeaderboardViewModel)
    )]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> UpdateLeaderboard(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateLeaderboardRequest request
    )
    {
        UpdateResult<Leaderboard> result = await leaderboardService.UpdateLeaderboard(id, request);

        return result.Match<ActionResult>(
            conflict => Conflict(conflict.Conflicting),
            notfound => NotFound(),
            success => NoContent()
        );
    }
}
