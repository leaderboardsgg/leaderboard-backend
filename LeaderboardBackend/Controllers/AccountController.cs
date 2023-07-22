using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    ///     Registers a new User.
    /// </summary>
    /// <param name="request">
    ///     The `RegisterRequest` instance from which register the `User`.
    /// </param>
    /// <response code="201">The `User` was registered and returned successfully.</response>
    /// <response code="400">
    ///     The request was malformed.
    /// </response>
    /// <response code="422">
    ///     The request contains errors.<br/><br/>
    ///     Validation error codes by property:
    ///     - **Username**:
    ///       - **UsernameFormat**: Invalid username format
    ///     - **Password**:
    ///       - **PasswordFormat**: Invalid password format
    ///     - **Email**:
    ///       - **EmailValidator**: Invalid email format
    /// </response>
    /// <response code="409">
    ///     A `User` with the specified username or email already exists.<br/><br/>
    ///     Validation error codes by property:
    ///     - **Username**:
    ///       - **UsernameTaken**: the username is already in use
    ///     - **Email**:
    ///       - **EmailAlreadyUsed**: the email is already in use
    /// </response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.PostAnon))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<UserViewModel>> Register([FromBody] RegisterRequest request)
    {
        CreateUserResult result = await _userService.CreateUser(request);
        return result.Match<ActionResult<UserViewModel>>(
            user => CreatedAtAction(nameof(UsersController.GetUserById), "Users",
                new { id = user.Id }, UserViewModel.MapFrom(user)),
            conflicts =>
            {
                if (conflicts.Username)
                {
                    ModelState.AddModelError(nameof(request.Username), "UsernameTaken");
                }

                if (conflicts.Email)
                {
                    ModelState.AddModelError(nameof(request.Email), "EmailAlreadyUsed");
                }

                return Conflict(new ValidationProblemDetails(ModelState));
            });
    }
}
