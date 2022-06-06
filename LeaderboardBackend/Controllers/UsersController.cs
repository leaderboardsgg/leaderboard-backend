using LeaderboardBackend.Controllers.Annotations;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
	private readonly IUserService UserService;
	private readonly IAuthService AuthService;

	public UsersController(IUserService userService, IAuthService authService)
	{
		UserService = userService;
		AuthService = authService;
	}

	/// <summary>Gets a User by ID.</summary>
	/// <param name="id">The User's ID. It must be a GUID.</param>
	/// <response code="200">The User with the provided ID.</response>
	/// <response code="404">If no User is found with the provided ID.</response>
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.GetAnon))]
	[AllowAnonymous]
	[HttpGet("{id:guid}")]
	public async Task<ActionResult<User>> GetUserById(Guid id)
	{
		User? user = await UserService.GetUserById(id);
		if (user is null)
		{
			return NotFound();
		}

		// FIXME: Make user view model
		user.Email = "";
		return Ok(user);
	}

	/// <summary>Registers a new user.</summary>
	/// <param name="body">A RegisterRequest instance.</param>
	/// <response code="201">The created User object.</response>
	/// <response code="400">If the passwords don't match, or if the request is otherwise malformed.</response>
	/// <response code="409">If login details can't be found.</response>
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[AllowAnonymous]
	[HttpPost("register")]
	public async Task<ActionResult<User>> Register([FromBody] RegisterRequest body)
	{
		if (await UserService.GetUserByEmail(body.Email) is not null)
		{
			// FIXME: Do a redirect to the login page.
			// ref: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-6.0#1-methods-resulting-in-an-empty-response-body
			return Conflict("A user already exists with this email.");
		}

		if (await UserService.GetUserByName(body.Username) is not null)
		{
			// FIXME: Do a redirect to the login page.
			// ref: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-6.0#1-methods-resulting-in-an-empty-response-body
			return Conflict("A user already exists with this name.");
		}

		User newUser = new()
		{
			Username = body.Username,
			Email = body.Email,
			Password = BCryptNet.EnhancedHashPassword(body.Password)
		};

		await UserService.CreateUser(newUser);
		return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
	}

	/// <summary>Logs a new user in.</summary>
	/// <param name="body">A LoginRequest instance.</param>
	/// <response code="200">A <code>LoginResponse</code> object.</response>
	/// <response code="400">If the request is malformed.</response>
	/// <response code="401">If the wrong details were passed.</response>
	/// <response code="404">If a User can't be found.</response>
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body)
	{
		User? user = await UserService.GetUserByEmail(body.Email);
		if (user is null)
		{
			return NotFound();
		}

		if (!BCryptNet.EnhancedVerify(body.Password, user.Password))
		{
			return Unauthorized();
		}

		string token = AuthService.GenerateJSONWebToken(user);
		return Ok(new LoginResponse { Token = token });
	}

	/// <summary>Gets the currently logged-in user.</summary>
	/// <remarks>
	/// <p>You <em>must</em> call this with the 'Authorization' header, passing a valid JWT bearer token. </p>
	/// <p>I.e. <code>{ 'Authorization': 'Bearer JWT' }</code></p>
	/// </remarks>
	/// <response code="200">Returns with the User's details.</response>
	/// <response code="403">If an invalid JWT was passed in.</response>
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[HttpGet("me")]
	public async Task<ActionResult<User>> Me()
	{
		string? email = AuthService.GetEmailFromClaims(HttpContext.User);
		if (email is null)
		{
			return Forbid();
		}
		User? user = await UserService.GetUserByEmail(email);
		if (user is null)
		{
			return Forbid();
		}
		return Ok(user);
	}
}
