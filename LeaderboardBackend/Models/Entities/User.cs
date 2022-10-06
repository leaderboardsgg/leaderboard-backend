using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a user account registered on the website.
/// </summary>
public class User : BaseEntity
{
	/// <summary>
	///     The unique identifier of the `User`.<br/>
	///     Generated on creation.
	/// </summary>
	public Guid Id { get; set; }

	/// <summary>
	///     The username of the `User`. It:
	///     <ul>
	///       <li>must be [2, 25] in length;</li>
	///       <li>must be made up of alphanumeric characters around zero or one of:</li>
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
	[Required]
	public string Username { get; set; } = null!;

	/// <summary>
	///     The `User`'s email address.
	/// </summary>
	/// <example>john.doe@example.com</example>
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
	[JsonIgnore]
	[Required]
	public string Password { get; set; } = null!;

	/// <summary>
	///     The `User`'s personal description, displayed on their profile.
	/// </summary>
	public string? About { get; set; }

	/// <summary>
	///     The `User`'s administrator status.
	/// </summary>
	[Required]
	public bool Admin { get; set; } = false;

	/// <summary>
	///     The `Ban`s the `User` has issued.
	/// </summary>
	[InverseProperty("BanningUser")]
	public List<Ban>? BansGiven { get; set; }

	/// <summary>
	///     The `Ban`s the `User` has received.
	/// </summary>
	[InverseProperty("BannedUser")]
	public List<Ban>? BansReceived { get; set; }

	/// <summary>
	///     The `Modship`s associated with the `User`.
	/// </summary>
	public List<Modship>? Modships { get; set; }

	/// <summary>
	///     The `Participation`s associated with the `User`.
	/// </summary>
	public List<Participation>? Participations { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is User user
			&& Id.Equals(user.Id);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Username, Email);
	}
}
