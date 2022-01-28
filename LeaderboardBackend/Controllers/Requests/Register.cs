using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record RegisterRequest
{
	[Required]
	[RegularExpression("/([a-zA-Z][-_']?){1,12}[a-zA-Z]/",
		ErrorMessage = "Your name must be between 2 and 25 characters, made up of letters sandwiching zero or one hyphen, underscore, or apostrophe.")]
	public string Username { get; set; } = null!;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[MinLength(8, ErrorMessage = "Your password must be at least 8 characters long.")]
	[MaxLength(80, ErrorMessage = "Your password must be at most 80 characters long.")]
	[Password]
	public string Password { get; set; } = null!;

	[Required]
	[Compare("Password", ErrorMessage = "This must match your password.")]
	[Password]
	public string PasswordConfirm { get; set; } = null!;
}
