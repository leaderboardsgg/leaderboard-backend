using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when a `User` is attempting to log in.
/// </summary>
public record LoginRequest
{
	/// <summary>
	///     The `User`'s email address.
	/// </summary>
	/// <example>john.doe@example.com</example>
	[EmailAddress]
	[Required]
	public string Email { get; set; } = null!;

	/// <summary>
	///     The `User`'s password. It:
	///     <ul>
	///       <li>supports Unicode;</li>
	///       <li>must be [8, 80] in length;</li>
	///       <li>must have at least:</li>
	///         <ul>
	///           <li>one uppercase letter;</li>
	///           <li>one lowercase letter; and</li>
	///           <li>one number.</li>
	///         </ul>
	///     </ul>
	/// </summary>
	/// <example>P4ssword</example>
	[Password]
	[Required]
	public string Password { get; set; } = null!;
}

/// <summary>
///     This response object is received upon a successful log-in request.
/// </summary>
public record LoginResponse
{
	/// <summary>
	///     A JSON Web Token to authenticate and authorize queries with.
	/// </summary>
	[Required]
	public string Token { get; set; } = null!;
}

/// <summary>
///     This request object is sent when a `User` is attempting to register.
/// </summary>
public record RegisterRequest
{
	/// <summary>
	///     The username of the `User`. It:
	///     <ul>
	///       <li>must be [2, 25] in length;</li>
	///       <li>must be made up of letters sandwiching zero or one of:</li>
	///       <ul>
	///         <li>hyphen;</li>
	///         <li>underscore; or</li>
	///         <li>apostrophe</li>
	///       </ul>
	///     </ul>
	///     Usernames are saved case-sensitively, but matcehd against case-insensitively.
	///     A `User` may not register with the name 'Cool' when another `User` with the name 'cool'
	///     exists.
	/// </summary>
	/// <example>J'on-Doe</example>
	[RegularExpression("(?:[a-zA-Z0-9][-_']?){1,12}[a-zA-Z0-9]",
		ErrorMessage = "Your name must be between 2 and 25 characters, made up of letters sandwiching zero or one hyphen, underscore, or apostrophe.")]
	[Required]
	public string Username { get; set; } = null!;

	/// <summary>
	///     The `User`'s email address.
	/// </summary>
	/// <example>john.doe@example.com</example>
	[EmailAddress]
	[Required]
	public string Email { get; set; } = null!;

	/// <summary>
	///     The `User`'s password. It:
	///     <ul>
	///       <li>supports Unicode;</li>
	///       <li>must be [8, 80] in length;</li>
	///       <li>must have at least:</li>
	///         <ul>
	///           <li>one uppercase letter;</li>
	///           <li>one lowercase letter; and</li>
	///           <li>one number.</li>
	///         </ul>
	///     </ul>
	/// </summary>
	/// <example>P4ssword</example>
	[Password]
	[Required]
	public string Password { get; set; } = null!;

	/// <summary>
	///     The password confirmation. This value must match `Password`.
	/// </summary>
	[Compare("Password", ErrorMessage = "The password confirmation must match your password.")]
	[Required]
	public string PasswordConfirm { get; set; } = null!;
}
