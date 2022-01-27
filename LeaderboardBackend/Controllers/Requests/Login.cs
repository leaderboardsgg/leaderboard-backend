using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[MinLength(8)]
	[MaxLength(80)]
	public string Password { get; set; } = null!;
}
