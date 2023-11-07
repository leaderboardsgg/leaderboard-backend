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
using Swashbuckle.AspNetCore.Annotations;

namespace LeaderboardBackend.Controllers;

[Route("[controller]")]
public class AccountController : ApiController
{
    private readonly IUserService _userService;

    public AccountController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.PostAnon))]
    [FeatureGate(Features.ACCOUNT_REGISTRATION)]
    [HttpPost("register")]
    [SwaggerOperation("Registers a new User.")]
    [SwaggerResponse(201, "The `User` was registered and returned successfully.")]
    [SwaggerResponse(400, "The request was malformed.")]
    [SwaggerResponse(
        409,
        """
        A `User` with the specified username or email already exists.<br/><br/>
        Validation error codes by property:
        - **Username**:
          - **UsernameTaken**: the username is already in use
        - **Email**:
          - **EmailAlreadyUsed**: the email is already in use
        """,
        typeof(ValidationProblemDetails)
    )]
    [SwaggerResponse(
        422,
        """
        The request contains errors.<br/><br/>
        Validation error codes by property:
        - **Username**:
          - **UsernameFormat**: Invalid username format
        - **Password**:
          - **PasswordFormat**: Invalid password format
        - **Email**:
          - **EmailValidator**: Invalid email format
        """,
        typeof(ValidationProblemDetails)
    )]
    public async Task<ActionResult<UserViewModel>> Register(

        [FromBody, SwaggerRequestBody(
            "The `RegisterRequest` instance from which to register the `User`.",
            Required = true
        )] RegisterRequest request
    ,
        [FromServices] IAccountConfirmationService confirmationService
    )
    {
        CreateUserResult result = await _userService.CreateUser(request);

        if (result.TryPickT0(out User user, out CreateUserConflicts conflicts))
        {
            CreateConfirmationResult r = await confirmationService.CreateConfirmationAndSendEmail(user);

            return r.Match<ActionResult>(
                confirmation => CreatedAtAction(
                    nameof(UsersController.GetUserById),
                    "Users",
                    new { id = user.Id },
                    UserViewModel.MapFrom(confirmation.User)
                ),
                badRole => StatusCode(StatusCodes.Status500InternalServerError),
                emailFailed => StatusCode(StatusCodes.Status500InternalServerError)
            );
        }

        if (conflicts.Username)
        {
            ModelState.AddModelError(nameof(request.Username), "UsernameTaken");
        }

        if (conflicts.Email)
        {
            ModelState.AddModelError(nameof(request.Email), "EmailAlreadyUsed");
        }

        return Conflict(new ValidationProblemDetails(ModelState));
    }

    [AllowAnonymous]
    [ApiConventionMethod(typeof(Conventions), nameof(Conventions.PostAnon))]
    [FeatureGate(Features.LOGIN)]
    [HttpPost("/login")]
    [SwaggerOperation("Logs a User in.")]
    [SwaggerResponse(
        200,
        "The `User` was logged in successfully. A `LoginResponse` is returned, containing a token.",
        typeof(LoginResponse)
    )]
    [SwaggerResponse(400, "The request was malformed.")]
    [SwaggerResponse(401, "The password given was incorrect.")]
    [SwaggerResponse(403, "The associated `User` is banned.")]
    [SwaggerResponse(404, "No `User` with the requested details could be found.")]
    [SwaggerResponse(
        422,
        """
        The request contains errors.<br/><br/>
        Validation error codes by property:
        - **Password**:
          - **NotEmptyValidator**: No password was passed
          - **PasswordFormat**: Invalid password format
        - **Email**:
          - **NotNullValidator**: No email was passed
          - **EmailValidator**: Invalid email format
        """,
        typeof(ValidationProblemDetails)
    )]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody, SwaggerRequestBody(
            "The `LoginRequest` instance with which to perform the login.",
            Required = true
        )] LoginRequest request
    )
    {
        LoginResult result = await _userService.LoginByEmailAndPassword(request.Email, request.Password);

        return result.Match<ActionResult<LoginResponse>>(
            loginToken => Ok(new LoginResponse { Token = loginToken }),
            notFound => NotFound(),
            banned => Forbid(),
            badCredentials => Unauthorized()
        );
    }

    [HttpPost("confirm")]
    [SwaggerOperation("Resends the account confirmation link.")]
    [SwaggerResponse(200, "A new confirmation link was generated.")]
    [SwaggerResponse(400, "The request was malformed.")]
    [SwaggerResponse(401, "The request doesn't contain a valid session token.")]
    [SwaggerResponse(409, "The `User`'s account has already been confirmed.")]
    [SwaggerResponse(500, "The account recovery email failed to be created.")]
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

    [AllowAnonymous]
    [HttpPost("recover")]
    [SwaggerOperation("Sends an account recovery email.")]
    [SwaggerResponse(200, "This endpoint returns 200 OK regardless of whether the email was sent successfully or not.")]
    [SwaggerResponse(400, "The request object was malformed.")]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    public async Task<ActionResult> RecoverAccount(
        [FromServices] IAccountRecoveryService recoveryService,
        [FromServices] ILogger<AccountController> logger,
        [FromBody, SwaggerRequestBody("The account recovery request.")] RecoverAccountRequest request
    )
    {
        User? user = await _userService.GetUserByNameAndEmail(request.Username, request.Email);

        if (user is null)
        {
            logger.LogWarning("Account recovery attempt failed. User not found: {username}", request.Username);
        }
        else
        {
            logger.LogInformation("Sending account recovery email to user: {id}", user.Id);
            await recoveryService.CreateRecoveryAndSendEmail(user);
        }

        return Ok();
    }

    [AllowAnonymous]
    [HttpPut("confirm/{id}")]
    [SwaggerOperation("Confirms a user account.")]
    [SwaggerResponse(200, "The account was confirmed successfully.")]
    [SwaggerResponse(400)]
    [SwaggerResponse(404, "The token provided was invalid or expired.")]
    [SwaggerResponse(409, "the user's account was either already confirmed or banned.")]
    public async Task<ActionResult> ConfirmAccount(
        [SwaggerParameter("The confirmation token.")] Guid id,
        [FromServices] IAccountConfirmationService confirmationService
    )
    {
        ConfirmAccountResult result = await confirmationService.ConfirmAccount(id);

        return result.Match<ActionResult>(
            confirmed => Ok(),
            alreadyUsed => NotFound(),
            badRole => Conflict(),
            notFound => NotFound(),
            expired => NotFound()
        );
    }

    /// <summary>
    /// Tests an account recovery token for validity.
    /// </summary>
    /// <param name="id">The recovery token.</param>
    /// <param name="recoveryService">IAccountRecoveryService dependency.</param>
    /// <response code="200">The token provided is valid.</response>
    /// <response code="404">The token provided is invalid or expired, or the user is banned.</response>
    [AllowAnonymous]
    [HttpGet("recover/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    public async Task<ActionResult> TestRecovery(Guid id, [FromServices] IAccountRecoveryService recoveryService)
    {
        TestRecoveryResult result = await recoveryService.TestRecovery(id);

        return result.Match<ActionResult>(
            alreadyUsed => NotFound(),
            badRole => NotFound(),
            expired => NotFound(),
            notFound => NotFound(),
            success => Ok()
        );
    }

    /// <summary>
    /// Recover the user's account by resetting their password to a new value.
    /// </summary>
    /// <param name="id">The recovery token.</param>
    /// <param name="request">The password recovery request object.</param>
    /// <param name="recoveryService">IAccountRecoveryService dependency</param>
    /// <response code="200">The user's password was reset successfully.</response>
    /// <response code="403">The user is banned.</response>
    /// <response code="404">The token provided is invalid or expired.</response>
    /// <response code="409">The new password is the same as the user's existing password.</response>
    /// <response code="422">
    ///     The request body contains errors.<br/>
    ///     A **PasswordFormat** Validation error on the Password field indicates that the password format is invalid.
    /// </response>
    [AllowAnonymous]
    [HttpPost("recover/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ValidationProblemDetails))]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    public async Task<ActionResult> ResetPassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        [FromServices] IAccountRecoveryService recoveryService
    )
    {
        ResetPasswordResult result = await recoveryService.ResetPassword(id, request.Password);

        return result.Match<ActionResult>(
            alreadyUsed => NotFound(),
            badRole => Forbid(),
            expired => NotFound(),
            notFound => NotFound(),
            samePassword => Conflict(),
            success => Ok()
        );
    }
}
