using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record RegisterRequest 
{
	[Required]
	[RegularExpression("(?:[a-zA-Z][-_']?){1,12}[a-zA-Z]",
		ErrorMessage = "Your name must be between 2 and 25 characters, made up of letters sandwiching zero or one hyphen, underscore, or apostrophe.")]
	public string Username { get; set; } = null!;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	[Required]
	[Password]
	public string Password { get; set; } = null!;

	[Required]
	[Compare("Password", ErrorMessage = "The password confirmation must match your password.")]
	public string PasswordConfirm { get; set; } = null!;
}
