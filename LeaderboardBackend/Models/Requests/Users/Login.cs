using LeaderboardBackend.Models.Annotations;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Users;

public record LoginRequest
{
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
}

public record LoginResponse
{
	[Required]
	public string Token { get; set; } = null!;
}
