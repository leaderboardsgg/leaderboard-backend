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
        private readonly IConfiguration _config;

        public UsersController(IUserService userService, IConfiguration config)
        {
            _userService = userService;
            _config = config;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(long id)
        {
            User user;
            try
            {
                user = await _userService.GetUser(id);
            }
            catch (UserNotFoundException)
            {
                // TODO Can we make certain exceptions like UserNotFoundException map to NotFound automatically?
                return NotFound();
            }

            return user;
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
            User user;
            try
            {
                user = await _userService.GetUserByEmail(body.Email);
            }
            catch (UserNotFoundException)
            {
                return NotFound();
            }

            if (!BCryptNet.EnhancedVerify(body.Password, user.Password))
            {
                return Unauthorized();
            }
            string token = _userService.GenerateJSONWebToken(user);
            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<User>> Me()
        {
            User user = await _userService.GetUserFromClaims(HttpContext.User);
            return Ok(User);
        }
    }
}
