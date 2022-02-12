using System.ComponentModel.DataAnnotations;
namespace LeaderboardBackend.Controllers.Requests;

public record RegisterRequest
{
	/// <summary>User's name.</summary>
	/// <example>Ayylmao Gaming</example>
	[Required]
	[RegularExpression("(?:[a-zA-Z][-_']?){1,12}[a-zA-Z]",
		ErrorMessage = "Your name must be between 2 and 25 characters, made up of letters sandwiching zero or one hyphen, underscore, or apostrophe.")]
	public string Username { get; set; } = null!;

	/// <summary>User's email.</summary>
	/// <example>ayylmao.gaming@alg.gg</example>
	[Required]
	[EmailAddress]
	public string Email { get; set; } = null!;

	/// <summary>
	/// User's password. It:
	/// <ul>
	///   <li>must be 8-80 characters long, inclusive;</li>
	///   <li>must have at least:</li>
	///     <ul>
	///       <li>an uppercase letter;</li>
	///       <li>a lowercase letter; and</li>
	///       <li>a number.</li>
	///     </ul>
	///   <li>supports Unicode.</li>
	/// </ul>
	/// </summary>
	/// <example>P4ssword</example>
	[Required]
	[Password]
	public string Password { get; set; } = null!;

	/// <summary>Confirmation of the User's password. This <em>must</em> match Password.</summary>
	/// <example>P4ssword</example>
	[Required]
	[Compare("Password", ErrorMessage = "The password confirmation must match your password.")]
	public string PasswordConfirm { get; set; } = null!;
}
