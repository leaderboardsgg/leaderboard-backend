
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Controllers.Requests
{
	public class LoginRequest
	{
		public string Email { get; set; } = null!;

		public string Password { get; set; } = null!;
	}
}
