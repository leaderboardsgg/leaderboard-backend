using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using LeaderboardBackend.Result;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using OneOf;

namespace LeaderboardBackend.Controllers;

[Route("[controller]")]
public class AccountController : ApiController
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
    [FeatureGate(Features.ACCOUNT_REGISTRATION)]
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
    /// <response code="403">The associated `User` is banned.</response>
    /// <response code="404">No `User` with the requested details could be found.</response>
    /// <response code="422">
    ///     The request contains errors.<br/><br/>
    ///     Validation error codes by property:
    ///     - **Password**:
    ///       - **NotEmptyValidator**: No password was passed
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [FeatureGate(Features.LOGIN)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        LoginResult result = await _userService.LoginByEmailAndPassword(request.Email, request.Password);

        return result.Match<ActionResult<LoginResponse>>(
            loginToken => Ok(new LoginResponse { Token = loginToken }),
            notFound => NotFound(),
            banned => Forbid(),
            badCredentials => Unauthorized()
        );
    }

    /// <summary>
    ///     Resends the account confirmation link.
    /// </summary>
    /// <param name="confirmationService">IAccountConfirmationService dependency.</param>
    /// <response code="200">A new confirmation link was generated.</response>
    /// <response code="400">
    ///     The request was malformed.
    /// </response>
    /// <response code="401">
    ///     The request doesn't contain a valid session token.
    /// </response>
    /// <response code="409">
    ///     The `User`'s account has already been confirmed.
    /// </response>
    /// <response code="500">
    ///     The account recovery email failed to be created.
    /// </response>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> ResendConfirmation(
        [FromServices] IAccountConfirmationService confirmationService
    )
    {
        // TODO: Handle rate limiting (429 case) - zysim

        GetUserResult result = await _userService.GetUserFromClaims(HttpContext.User);

        if (result.TryPickT0(out User user, out OneOf<BadCredentials, UserNotFound> errors))
        {
            CreateConfirmationResult r = await confirmationService.CreateConfirmationAndSendEmail(user);

            return r.Match<ActionResult>(
                confirmation => Ok(),
                badRole => Conflict(),
                emailFailed => StatusCode(StatusCodes.Status500InternalServerError)
            );
        }

        return errors.Match<ActionResult>(
            badCredentials => Unauthorized(),
            // Shouldn't be possible; throw 401
            notFound => Unauthorized()
        );
    }

    /// <summary>
    ///     Sends an account recovery email.
    /// </summary>
    /// <param name="recoveryService">IAccountRecoveryService dependency.</param>
    /// <param name="request">The account recovery request.</param>
    /// <response code="200">This endpoint returns 200 OK regardless of whether the email was sent successfully or not.</response>
    [AllowAnonymous]
    [HttpPost("recover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> RecoverAccount(
        [FromServices] IAccountRecoveryService recoveryService,
        [FromBody] RecoverAccountRequest request
    )
    {
        User? user = await _userService.GetUserByNameAndEmail(request.Username, request.Email);

        if (user is not null)
        {
            await recoveryService.CreateRecoveryAndSendEmail(user);
        }

        return Ok();
    }
}
