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
		return await _userService.GetUser(id) is User user
			? Ok(user)
			: NotFound();
	}

	[AllowAnonymous]
	[HttpPost("register")]
	public async Task<ActionResult<User>> Register([FromBody] RegisterRequest body)
	{
		if (body.Password != body.PasswordConfirm)
		{
			return BadRequest();
		}

		var newUser = new User
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
		if (await _userService.GetUserByEmail(body.Email) is not User user)
		{
			return NotFound();
		}

		if (!BCryptNet.EnhancedVerify(body.Password, user.Password))
		{
			return Unauthorized();
		}

		var token = _authService.GenerateJSONWebToken(user);
		return Ok(new { token });
	}

	[Authorize]
	[HttpGet("me")]
	public async Task<ActionResult<User>> Me()
	{
		return await _userService.GetUserFromClaims(HttpContext.User) is User user ? Ok(user) : Forbid();
	}
}
