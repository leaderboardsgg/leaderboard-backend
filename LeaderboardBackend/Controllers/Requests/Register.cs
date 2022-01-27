using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record RegisterRequest
{
	[Required]
	[RegularExpression("/([a-zA-Z][-_']?){1,12}[a-zA-Z]/")]
	public string Username { get; set; } = null!;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[MinLength(8)]
	[MaxLength(80)]
	public string Password { get; set; } = null!;

	[Required]
	[MinLength(8)]
	[MaxLength(80)]
	public string PasswordConfirm { get; set; } = null!;
}
