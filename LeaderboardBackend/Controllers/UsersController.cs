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

    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.Get))]
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
    [SwaggerResponse(401, "An invalid JWT was passed in.")]
    [SwaggerResponse(404, "The user was not found in the database.")]
    public async Task<ActionResult<UserViewModel>> Me()
    {
        return (await _userService.GetUserFromClaims(HttpContext.User)).Match<ActionResult<UserViewModel>>(
            user => Ok(UserViewModel.MapFrom(user)),
            badCredentials => Unauthorized(),
            userNotFound => NotFound()
        );
    }
}
