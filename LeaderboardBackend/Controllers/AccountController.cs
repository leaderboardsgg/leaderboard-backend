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
    /// <response code="409">
    ///     A `User` with the specified username or email already exists.<br/><br/>
    ///     Validation error codes by property:
    ///     - **Username**:
    ///       - **UsernameTaken**: the username is already in use
    ///     - **Email**:
    ///       - **EmailAlreadyUsed**: the email is already in use
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


    /// <summary>
    ///     Logs a User in.
    /// </summary>
    /// <param name="request">
    ///     The `LoginRequest` instance from which to perform the login.
    /// </param>
    /// <response code="200">
    ///     The `User` was logged in successfully. A `LoginResponse` is returned, containing a token.
    /// </response>
    /// <response code="400">The request was malformed.</response>
    /// <response code="401">The password given was incorrect.</response>
    /// <response code="404">No `User` with the requested details could be found.</response>
    /// <response code="422">
    ///     The request contains errors.<br/><br/>
    ///     Validation error codes by property:
    ///     - **Password**:
    ///       - **NotNullValidator**: No password was passed
    ///       - **PasswordFormat**: Invalid password format
    ///     - **Email**:
    ///       - **NotNullValidator**: No email was passed
    ///       - **EmailValidator**: Invalid email format
    /// </response>
    [AllowAnonymous]
    [HttpPost("/login")]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.PostAnon))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    #pragma warning disable CS1573 // Hides warning for not having authService in the XML comment above
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, [FromServices] IAuthService authService)
    {
        User? user = await _userService.GetUserByEmail(request.Email);

        if (user is null)
        {
            return NotFound();
        }

        if (!BCryptNet.EnhancedVerify(request.Password, user.Password))
        {
            return Unauthorized();
        }

        string token = authService.GenerateJSONWebToken(user);

        return Ok(new LoginResponse { Token = token });
    }
    #pragma warning restore CS1573
}
