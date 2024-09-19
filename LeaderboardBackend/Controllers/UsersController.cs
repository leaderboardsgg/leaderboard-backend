using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class UsersController(IUserService userService) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/user/{id:guid}")]
    [SwaggerOperation("Gets a User by their ID.", OperationId = "getUser")]
    [SwaggerResponse(200, "The `User` was found and returned successfully.")]
    [SwaggerResponse(404, "No `User` with the requested ID could be found.")]
    public async Task<ActionResult<UserViewModel>> GetUserById(
        [SwaggerParameter("The ID of the `User` which should be retrieved.")] Guid id
    )
    {
        User? user = await userService.GetUserById(id);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(UserViewModel.MapFrom(user));
    }

    [Authorize]
    [HttpGet("user/me")]
    [SwaggerOperation(
        "Gets the currently logged-in User.",
        """
        Call this method with the 'Authorization' header. A valid JWT bearer token must be
        passed.
        Example: `{ 'Authorization': 'Bearer JWT' }`.
        """,
        OperationId = "me"
    )]
    [SwaggerResponse(200, "The `User` was found and returned successfully.")]
    [SwaggerResponse(401, "An invalid JWT was passed in.")]
    [SwaggerResponse(404, "The user was not found in the database.")]
    public async Task<ActionResult<UserViewModel>> Me() => (await userService.GetUserFromClaims(HttpContext.User)).Match<ActionResult<UserViewModel>>(
            user => Ok(UserViewModel.MapFrom(user)),
            badCredentials => Unauthorized(),
            userNotFound => NotFound()
        );
}
