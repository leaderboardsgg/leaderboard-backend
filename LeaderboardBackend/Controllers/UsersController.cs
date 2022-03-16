using LeaderboardBackend.Controllers.Requests;
using LeaderboardBackend.Models;
using LeaderboardBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LeaderboardBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly IAuthService _authService;

	public UsersController(IUserService userService, IAuthService authService)
	{
		_userService = userService;
		_authService = authService;
	}

	/// <summary>Gets a User.</summary>
	/// <param name="id">The User's ID. It must be a GUID.</param>
	/// <response code="200">The User with the provided ID.</response>
	/// <response code="404">If no User is found with the provided ID.</response>
	[HttpGet("{id}")]
	[ApiConventionMethod(typeof(Conventions),
						 nameof(Conventions.Get))]
	public async Task<ActionResult<User>> GetUser(Guid id)
	{
		User? user = await _userService.GetUser(id);
		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	/// <summary>Registers a new user.</summary>
	/// <param name="body">A RegisterRequest instance.</param>
	/// <response code="201">The created User object.</response>
	/// <response code="400">If the passwords don't match.</response>
	/// <response code="409">If login details can't be found.</response>
	[AllowAnonymous]
	[HttpPost("register")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<User>> Register([FromBody] RegisterRequest body)
	{
		// This shouldn't hit normally, since we have the CompareAttribute in Register.cs
		if (body.Password != body.PasswordConfirm)
		{
			return BadRequest();
		}

		if (await _userService.GetUserByEmail(body.Email) != null)
		{
			// FIXME: Return 404 here. Don't return a 409.
			return Conflict("A user already exists with this email.");
		}

		if (await _userService.GetUserByName(body.Username) != null)
		{
			// FIXME: Return 404 here. Don't return a 409.
			return Conflict("A user already exists with this name.");
		}

		User newUser = new()
		{
			Username = body.Username,
			Email = body.Email,
			Password = BCryptNet.EnhancedHashPassword(body.Password)
		};

		await _userService.CreateUser(newUser);
		return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
	}

	/// <summary>Logs a new user in.</summary>
	/// <param name="body">A LoginRequest instance.</param>
	/// <response code="200">An object <code>{ token: JWT }</code></response>
	/// <response code="401">If the wrong details were passed.</response>
	/// <response code="404">If a User can't be found.</response>
	[AllowAnonymous]
	[HttpPost("login")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<User>> Login([FromBody] LoginRequest body)
	{
		User? user = await _userService.GetUserByEmail(body.Email);
		if (user == null)
		{
			return NotFound();
		}

		if (!BCryptNet.EnhancedVerify(body.Password, user.Password))
		{
			return Unauthorized();
		}

		string token = _authService.GenerateJSONWebToken(user);
		return Ok(new { token });
	}

	/// <summary>Gets the currently logged-in user.</summary>
	/// <remarks>
	/// <p>You <em>must</em> call this with the 'Authorization' header, passing a valid JWT bearer token. </p>
	/// <p>I.e. <code>{ 'Authorization': 'Bearer JWT' }</code></p>
	/// </remarks>
	/// <response code="200">Returns with the User's details.</response>
	/// <response code="403">If an invalid JWT was passed in.</response>
	[Authorize]
	[HttpGet("me")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<User>> Me()
	{
		return await _userService.GetUserFromClaims(HttpContext.User) is User user ? Ok(user) : Forbid();
	}
}
