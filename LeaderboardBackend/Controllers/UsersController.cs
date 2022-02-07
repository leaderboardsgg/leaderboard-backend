using Microsoft.AspNetCore.Mvc;
using LeaderboardBackend.Models;
using LeaderboardBackend.Controllers.Requests;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization;
using LeaderboardBackend.Services;

namespace LeaderboardBackend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly IUserService _userService;
		private readonly IAuthService _authService;

		public UsersController(
			IUserService userService,
			IAuthService authService
		)
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
			User? user = await _userService.GetUserFromClaims(HttpContext.User);
			if (user == null)
			{
				return Forbid();
			}
			return Ok(user);
		}
	}
}
