using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Controllers;

public class UsersController : ApiController
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public UsersController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    ///     Gets a User by their ID.
    /// </summary>
    /// <param name="id">The ID of the `User` which should be retrieved.</param>
    /// <response code="200">The `User` was found and returned successfully.</response>
    /// <response code="404">No `User` with the requested ID could be found.</response>
    [AllowAnonymous]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserViewModel>> GetUserById(Guid id)
    {
        User? user = await _userService.GetUserById(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(UserViewModel.MapFrom(user));
    }

    /// <summary>
    ///     Gets the currently logged-in User.
    /// </summary>
    /// <remarks>
    ///     Call this method with the 'Authorization' header. A valid JWT bearer token must be
    ///     passed.<br/>
    ///     Example: `{ 'Authorization': 'Bearer JWT' }`.
    /// </remarks>
    /// <response code="200">The `User` was found and returned successfully..</response>
    /// <response code="401">An invalid JWT was passed in.</response>
    /// <response code="404">The user was not found in the database.</response>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
    public async Task<ActionResult<UserViewModel>> Me()
    {
        return (await _userService.GetUserFromClaims(HttpContext.User)).Match<ActionResult<UserViewModel>>(
            user => Ok(UserViewModel.MapFrom(user)),
            badCredentials => Unauthorized(),
            userNotFound => NotFound()
        );
    }
}
