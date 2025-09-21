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
    [HttpGet("api/leaderboards/{id:long}")]
    [SwaggerOperation("Gets a leaderboard by its ID.", OperationId = "getLeaderboard")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboard([FromRoute] long id)
    {
        LeaderboardWithStats? leaderboard = await leaderboardService.GetLeaderboard(id);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards/{slug}")]
    [SwaggerOperation("Gets a leaderboard by its slug. Will not return deleted boards.", OperationId = "getLeaderboardBySlug")]
    [SwaggerResponse(200)]
    [SwaggerResponse(404)]
    public async Task<ActionResult<LeaderboardViewModel>> GetLeaderboardBySlug([FromRoute] string slug)
    {
        LeaderboardWithStats? leaderboard = await leaderboardService.GetLeaderboardBySlug(slug);

        if (leaderboard == null)
        {
            return NotFound();
        }

        return Ok(LeaderboardViewModel.MapFrom(leaderboard));
    }

    [AllowAnonymous]
    [HttpGet("api/leaderboards")]
    [Paginated]
    [SwaggerOperation("Gets leaderboards. Includes deleted, if specified.", OperationId = "listLeaderboards")]
    [SwaggerResponse(200)]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<ListView<LeaderboardViewModel>>> GetLeaderboards(
        [FromQuery] Page page,
        [FromQuery] StatusFilter status = StatusFilter.Published,
        [FromQuery, SwaggerParameter("Sorts results by a leaderboard's field, tie-breaking with IDs if needed.")] SortLeaderboardsBy sortBy = SortLeaderboardsBy.Name_Asc
    )
    {
        ListResult<LeaderboardWithStats> result = await leaderboardService.ListLeaderboards(status, page, sortBy);
        return Ok(new ListView<LeaderboardViewModel>()
        {
            Data = [.. result.Items.Select(LeaderboardViewModel.MapFrom)],
            Total = result.ItemsTotal
        });
    }

    [AllowAnonymous]
    [HttpGet("/api/search/leaderboards")]
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

        ListResult<LeaderboardWithStats> result = await leaderboardService.SearchLeaderboards(query, status, page);

        return Ok(new ListView<LeaderboardViewModel>()
        {
            Data = [.. result.Items.Select(LeaderboardViewModel.MapFrom)],
            Total = result.ItemsTotal
        });
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPost("leaderboards")]
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
    [HttpDelete("leaderboards/{id:long}")]
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
    [HttpPatch("/leaderboards/{id:long}")]
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
        typeof(ConflictDetails<LeaderboardViewModel>)
    )]
    [SwaggerResponse(422, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> UpdateLeaderboard(
        [FromRoute] long id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateLeaderboardRequest request
    )
    {
        UpdateResult<Leaderboard> result = await leaderboardService.UpdateLeaderboard(id, request);

        return result.Match<ActionResult>(
            conflict =>
            {
                ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status409Conflict);
                problemDetails.Extensions.Add("conflicting", LeaderboardViewModel.MapFrom(conflict.Conflicting));
                return Conflict(problemDetails);
            },
            notfound => NotFound(),
            success => NoContent()
        );
    }
}
