using LeaderboardBackend.Authorization;
using LeaderboardBackend.Filters;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

public class UsersController(IUserService userService) : ApiController
{
    [AllowAnonymous]
    [HttpGet("api/users/{id}")]
    [SwaggerOperation("Gets a User by their ID.", OperationId = "getUser")]
    public async Task<Results<Ok<UserViewModel>, NotFound>> GetUserById(
        [SwaggerParameter("The ID of the `User` which should be retrieved.")] Guid id
    )
    {
        User? user = await userService.GetUserById(id);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(UserViewModel.MapFrom(user));
    }

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpGet("/users")]
    [Paginated]
    [SwaggerOperation("Gets users, filtered by role.", OperationId = "listUsers")]
    [SwaggerResponse(401)]
    [SwaggerResponse(403)]
    public async Task<Results<
        Ok<ListView<UserViewModel>>,
        UnauthorizedHttpResult,
        ForbidHttpResult,
        UnprocessableEntity<ValidationProblemDetails>
    >> GetUsers(
        [FromQuery] Page page,
        [
            FromQuery,
            SwaggerParameter("Multiple comma-separated values are allowed.")
        ] UserRole role = UserRole.Confirmed | UserRole.Administrator)
    {
        ListResult<User> result = await userService.ListUsers(page, role);
        return TypedResults.Ok(new ListView<UserViewModel>()
        {
            Data = result.Items.Select(UserViewModel.MapFrom).ToList(),
            Total = result.ItemsTotal
        });
    }

    [Authorize]
    [HttpGet("users/me")]
    [SwaggerOperation(
        "Gets the currently logged-in User.",
        OperationId = "me"
    )]
    [SwaggerResponse(401)]
    public async Task<Results<
        Ok<UserViewModel>,
        UnauthorizedHttpResult,
        NotFound
    >> Me() => (await userService.GetUserFromClaims(HttpContext.User)).Match<Results<
        Ok<UserViewModel>,
        UnauthorizedHttpResult,
        NotFound
    >>(
            user => TypedResults.Ok(UserViewModel.MapFrom(user)),
            badCredentials => TypedResults.Unauthorized(),
            userNotFound => TypedResults.NotFound()
        );

    [Authorize(Policy = UserTypes.ADMINISTRATOR)]
    [HttpPatch("users/{id}")]
    [SwaggerOperation(
        "Updates a user. This request is restricted to administrators, and currently " +
        "only for banning/unbanning users.",
        OperationId = "updateUser"
    )]
    [SwaggerResponse(401)]
    [SwaggerResponse(
        403,
        "This request was not sent by an admin, the target user is an admin, or the " +
        "role provided was neither BANNED nor CONFIRMED.",
        typeof(ProblemDetails)
    )]
    public async Task<Results<
        NoContent,
        ForbidHttpResult,
        ProblemHttpResult,
        NotFound
    >> UpdateUser(
        [FromRoute] Guid id,
        [FromBody, SwaggerRequestBody(Required = true)] UpdateUserRequest request
    )
    {
        UpdateUserResult r = await userService.UpdateUser(id, request);

        return r.Match<Results<
            NoContent,
            ForbidHttpResult,
            ProblemHttpResult,
            NotFound
        >>(
            badRole => TypedResults.Problem(
                null,
                null,
                403,
                "Banning Admins Forbidden"
            ),
            roleChangeForbidden => TypedResults.Problem(
                null,
                null,
                403,
                "Role Change Forbidden"
            ),
            notFound => TypedResults.NotFound(),
            success => TypedResults.NoContent()
        );
    }
}
