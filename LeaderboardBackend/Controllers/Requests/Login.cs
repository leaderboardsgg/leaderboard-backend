using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;
}
