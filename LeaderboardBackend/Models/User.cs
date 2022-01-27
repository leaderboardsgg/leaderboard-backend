using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models;

public class User
{
	[Required] public Guid Id { get; set; }

	[Required]
	[EmailAddress]
	public string? Email { get; set; }

	[Required]
	[MinLength(8)]
	[MaxLength(80)]
	public string Password { get; set; } = null!;
}
