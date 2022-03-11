using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Controllers.Requests;

namespace LeaderboardBackend.Models;

public class User
{
	[Required] public Guid Id { get; set; }

	[Required]
	public string Username { get; set; } = null!;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;
}
