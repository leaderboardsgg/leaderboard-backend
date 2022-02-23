using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models;

public record User
{
	public Guid Id { get; set; }
	[Required] public string Username { get; set; } = null!;
	[Required] public string Email { get; set; } = null!;
	[Required] public string Password { get; set; } = null!;
}
