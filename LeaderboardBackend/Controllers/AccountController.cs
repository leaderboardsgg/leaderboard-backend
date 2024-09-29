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
public class AccountController(IUserService userService) : ApiController
{
    [AllowAnonymous]
    [FeatureGate(Features.ACCOUNT_REGISTRATION)]
    [HttpPost("register")]
    [SwaggerOperation("Registers a new User.", OperationId = "register")]
    [SwaggerResponse(201, "The `User` was registered and returned successfully.")]
    [SwaggerResponse(
        409,
        """
        A `User` with the specified username or email already exists.
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
        The request contains errors.
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
        CreateUserResult result = await userService.CreateUser(request);

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
    [FeatureGate(Features.LOGIN)]
    [HttpPost("/login")]
    [SwaggerOperation("Logs a User in.", OperationId = "login")]
    [SwaggerResponse(
        200,
        "The `User` was logged in successfully. A `LoginResponse` is returned, containing a token.",
        typeof(LoginResponse)
    )]
    [SwaggerResponse(401, "The password given was incorrect.")]
    [SwaggerResponse(403, "The associated `User` is banned.")]
    [SwaggerResponse(404, "No `User` with the requested details could be found.")]
    [SwaggerResponse(
        422,
        """
        The request contains errors.
        Validation error codes by property:
        - **Password**:
          - **NotEmptyValidator**: No password was passed
          - **PasswordFormat**: Invalid password format
        - **Email**:
          - **NotEmptyValidator**: No email was passed
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
        LoginResult result = await userService.LoginByEmailAndPassword(request.Email, request.Password);

        return result.Match<ActionResult<LoginResponse>>(
            loginToken => Ok(new LoginResponse { Token = loginToken }),
            notFound => NotFound(),
            banned => Forbid(),
            badCredentials => Unauthorized()
        );
    }

    [Authorize]
    [HttpPost("confirm")]
    [SwaggerOperation("Resends the account confirmation link.", OperationId = "resendConfirmationEmail")]
    [SwaggerResponse(200, "A new confirmation link was generated.")]
    [SwaggerResponse(401)]
    [SwaggerResponse(409, "The `User`'s account has already been confirmed.")]
    [SwaggerResponse(500, "The account recovery email failed to be created.")]
    public async Task<ActionResult> ResendConfirmation(
        [FromServices] IAccountConfirmationService confirmationService
    )
    {
        // TODO: Handle rate limiting (429 case) - zysim

        GetUserResult result = await userService.GetUserFromClaims(HttpContext.User);

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
    [SwaggerOperation("Sends an account recovery email.", OperationId = "sendRecoveryEmail")]
    [SwaggerResponse(200, "This endpoint returns 200 OK regardless of whether the email was sent successfully or not.")]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    public async Task<ActionResult> RecoverAccount(
        [FromServices] IAccountRecoveryService recoveryService,
        [FromServices] ILogger<AccountController> logger,
        [FromBody, SwaggerRequestBody("The account recovery request.")] RecoverAccountRequest request
    )
    {
        User? user = await userService.GetUserByNameAndEmail(request.Username, request.Email);

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
    [SwaggerOperation("Confirms a user account.", OperationId = "confirmAccount")]
    [SwaggerResponse(200, "The account was confirmed successfully.")]
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

    [AllowAnonymous]
    [HttpGet("recover/{id}")]
    [SwaggerOperation("Tests an account recovery token for validity.", OperationId = "testRecoveryToken")]
    [SwaggerResponse(200, "The token provided is valid.")]
    [SwaggerResponse(404, "The token provided is invalid or expired, or the user is banned.")]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    public async Task<ActionResult> TestRecovery(
        [SwaggerParameter("The recovery token.")] Guid id,
        [FromServices] IAccountRecoveryService recoveryService
    )
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

    [AllowAnonymous]
    [FeatureGate(Features.ACCOUNT_RECOVERY)]
    [HttpPost("recover/{id}")]
    [SwaggerOperation("Recover the user's account by resetting their password to a new value.", OperationId = "changePassword")]
    [SwaggerResponse(200, "The user's password was reset successfully.")]
    [SwaggerResponse(403, "The user is banned.")]
    [SwaggerResponse(404, "The token provided is invalid or expired.")]
    [SwaggerResponse(409, "The new password is the same as the user's existing password.")]
    [SwaggerResponse(
        422,
        """
        The request body contains errors.
        A **PasswordFormat** Validation error on the Password field indicates that the password format is invalid.
        """,
        typeof(ValidationProblemDetails)
    )]
    public async Task<ActionResult> ResetPassword(
        [SwaggerParameter("The recovery token.")] Guid id,
        [FromBody, SwaggerRequestBody("The password recovery request object.", Required = true)] ChangePasswordRequest request,
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
