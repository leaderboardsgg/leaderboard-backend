using LeaderboardBackend.Models.Annotations;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Users;

public record LoginRequest
{
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;
}

public record LoginResponse
{
	[Required]
	public string Token { get; set; } = null!;
}
