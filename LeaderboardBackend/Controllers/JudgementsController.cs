using LeaderboardBackend.Authorization;
using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public class JudgementsController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger _logger;
    private readonly IJudgementService _judgementService;
    private readonly IRunService _runService;
    private readonly IUserService _userService;

    public JudgementsController(
        IAuthService authService,
        ILogger<JudgementsController> logger,
        IJudgementService judgementService,
        IRunService runService,
        IUserService userService
    )
    {
        _authService = authService;
        _logger = logger;
        _judgementService = judgementService;
        _runService = runService;
        _userService = userService;
    }

    /// <summary>
    ///     Gets a Judgement by its ID.
    /// </summary>
    /// <param name="id">The ID of the `Judgement` which should be retrieved.</param>
    /// <response code="200">The `Judgement` was found and returned successfully.</response>
    /// <response code="404">No `Judgement` with the requested ID could be found.</response>
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
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

    /// <summary>
    ///     Creates a new Judgement for a Run.
    ///     This request is restricted to Moderators.
    /// </summary>
    /// <param name="request">
    ///     The `CreateJudgementRequest` instance from which to create the `Judgement`.
    /// </param>
    /// <response code="201">The `Judgement` was created and returned successfully.</response>
    /// <response code="400">The request was malformed.</response>
    /// <response code="403">
    ///     The requesting `User` is unauthorized to create `Judgement`s.
    /// </response>
    /// <response code="404">No `Run` with the ID from the request could be found.</response>
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Post))]
    [Authorize(Policy = UserTypes.MODERATOR)]
    [HttpPost]
    public async Task<ActionResult<JudgementViewModel>> CreateJudgement(
        [FromBody] CreateJudgementRequest request
    )
    {
        // FIXME: Make sure administrators cannot call this! - Ero

        Guid? modId = _authService.GetUserIdFromClaims(HttpContext.User);

        if (modId is null)
        {
            return Forbid();
        }

        Run? run = await _runService.GetRun(request.RunId);

        if (run is null)
        {
            // TODO: Write a better error message. - Ero
            _logger.LogError($"CreateJudgement: run is null. ID = {request.RunId}");

            return NotFound($"Run not found for ID = {request.RunId}");
        }

        if (run.Status == RunStatus.Created)
        {
            // TODO: Write a better error message. - Ero
            _logger.LogError(
                $"CreateJudgement: run has pending participations (i.e. run status == CREATED). "
                    + $"ID = {request.RunId}"
            );

            return BadRequest($"Run has pending Participations. ID = {request.RunId}");
        }

        // TODO: Update run status on body.Approved's value.
        Judgement judgement =
            new()
            {
                Approved = request.Approved,
                JudgeId = modId.Value,
                Note = request.Note,
                Run = run,
                RunId = run.Id
            };

        await _judgementService.CreateJudgement(judgement);

        JudgementViewModel judgementView = new(judgement);

        return CreatedAtAction(nameof(GetJudgement), new { id = judgementView.Id }, judgementView);
    }
}
