using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     Request object sent when logging a User in.
/// </summary>
public record LoginRequest
{
	/// <summary>
	///     User's email.
	/// </summary>
	/// <example>ayylmao.gaming@alg.gg</example>
	[EmailAddress]
	[Required]
	public string Email { get; set; } = null!;

	/// <summary>
	///     User's password. It:
	///     <ul>
	///       <li>must be 8-80 characters long, inclusive;</li>
	///       <li>must have at least:</li>
	///         <ul>
	///           <li>an uppercase letter;</li>
	///           <li>a lowercase letter; and</li>
	///           <li>a number.</li>
	///         </ul>
	///       <li>supports Unicode.</li>
	///     </ul>
	/// </summary>
	/// <example>P4ssword</example>
	[Password]
	[Required]
	public string Password { get; set; } = null!;
}

/// <summary>
///     Response object received on a successful login.
/// </summary>
public record LoginResponse
{
	/// <summary>
	///     A JWT to authenticate and authorize future queries with.
	/// </summary>
	[Required]
	public string Token { get; set; } = null!;
}

/// <summary>
///     Request object sent when registering a User.
/// </summary>
public record RegisterRequest
{
	/// <summary>
	///     The username to register with. It must be:
	///       <ul>
	///         <li>2-25 characters long, inclusive;</li>
	///         <li>made up of letters sandwiching zero or one of:</li>
	///         <ul>
	///           <li>hyphen;</li>
	///           <li>underscore; or</li>
	///           <li>apostrophe</li>
	///         </ul>
	///       </ul>
	///     Usernames are also saved with casing, but matched without. This means you won't
	///     be able to register as "Cool" if someone already called "cool" exists.
	/// </summary>
	/// <example>Ayy-l'maoGaming</example>
	[RegularExpression("(?:[a-zA-Z0-9][-_']?){1,12}[a-zA-Z0-9]",
		ErrorMessage = "Your name must be between 2 and 25 characters, made up of letters sandwiching zero or one hyphen, underscore, or apostrophe.")]
	[Required]
	public string Username { get; set; } = null!;

	/// <summary>
	///     User's email.
	/// </summary>
	/// <example>ayylmao.gaming@alg.gg</example>
	[EmailAddress]
	[Required]
	public string Email { get; set; } = null!;

	/// <summary>
	///     User's password. It:
	///     <ul>
	///       <li>must be 8-80 characters long, inclusive;</li>
	///       <li>must have at least:</li>
	///         <ul>
	///           <li>an uppercase letter;</li>
	///           <li>a lowercase letter; and</li>
	///           <li>a number.</li>
	///         </ul>
	///       <li>supports Unicode.</li>
	///     </ul>
	/// </summary>
	/// <example>P4ssword</example>
	[Password]
	[Required]
	public string Password { get; set; } = null!;

	/// <summary>
	///     Password confirmation. It must match <code>password</code>.
	/// </summary>
	/// <example>P4ssword</example>
	[Compare("Password", ErrorMessage = "The password confirmation must match your password.")]
	[Required]
	public string PasswordConfirm { get; set; } = null!;
}
