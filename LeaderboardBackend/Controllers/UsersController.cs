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

	[HttpGet("{id}")]
	public async Task<ActionResult<User>> GetUser(Guid id)
	{
		User? user = await _userService.GetUser(id);
		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	[AllowAnonymous]
	[HttpPost("register")]
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

	[AllowAnonymous]
	[HttpPost("login")]
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

	[Authorize]
	[HttpGet("me")]
	public async Task<ActionResult<User>> Me()
	{
		return await _userService.GetUserFromClaims(HttpContext.User) is User user ? Ok(user) : Forbid();
	}
}
