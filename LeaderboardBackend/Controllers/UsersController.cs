using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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

    [AllowAnonymous]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.GetAnon))]
    [HttpGet("{id:guid}")]
    [SwaggerOperation("Gets a User by their ID.")]
    [SwaggerResponse(200, "The `User` was found and returned successfully.")]
    [SwaggerResponse(404, "No `User` with the requested ID could be found.")]
    public async Task<ActionResult<UserViewModel>> GetUserById(
        [SwaggerParameter("The ID of the `User` which should be retrieved.")] Guid id
    )
    {
        User? user = await _userService.GetUserById(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(UserViewModel.MapFrom(user));
    }

    [HttpGet("me")]
    [SwaggerOperation(
        "Gets the currently logged-in User.",
        """
        Call this method with the 'Authorization' header. A valid JWT bearer token must be
        passed.<br/>
        Example: `{ 'Authorization': 'Bearer JWT' }`.
        """
    )]
    [SwaggerResponse(200, "The `User` was found and returned successfully.")]
    [SwaggerResponse(403, "An invalid JWT was passed in.")]
    public async Task<ActionResult<UserViewModel>> Me()
    {
        // FIXME: Use ApiConventionMethod here! - Ero

        string? email = _authService.GetEmailFromClaims(HttpContext.User);

        if (email is null)
        {
            // FIXME: This should be a 401 - Ted
            return Forbid();
        }

        User? user = await _userService.GetUserByEmail(email);

        // FIXME: Should return NotFound()! - Ero
        if (user is null)
        {
            return Forbid();
        }

        return Ok(UserViewModel.MapFrom(user));
    }
}
