using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a site-scoped or `Leaderboard`-scoped `Ban` tied to a `User`.
/// </summary>
public class Ban
{
	/// <summary>
	///     The unique identifier of the `Ban`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The reason for the issued `Ban`.<br/>
	///     Must not be null.
	/// </summary>
	[Required]
	public string Reason { get; set; } = null!;

	/// <summary>
	///     The time the `Ban` was issued.<br/>
	///     Generated on creation.
	/// </summary>
	[Required]
	public Instant CreatedAt { get; set; }

	/// <summary>
	///     The time the `Ban` was lifted.
	/// </summary>
	public Instant? DeletedAt { get; set; }

	/// <summary>
	///     The ID of the `User` who issued the `Ban`.<br/>
	///     Must be a *Moderator* or *Administrator*.
	/// </summary>
	public Guid? BanningUserId { get; set; }

	[JsonIgnore]
	public User? BanningUser { get; set; }

	/// <summary>
	///     The ID of the `User` who received the `Ban`.
	/// </summary>
	[Required]
	public Guid BannedUserId { get; set; }

	[JsonIgnore]
	public User? BannedUser { get; set; }

	/// <summary>
	///     ID of the `Leaderboard` the `Ban` belongs to.<br/>
	///     If this value is null, the `Ban` is site-wide.
	/// </summary>
	public long? LeaderboardId { get; set; }

	[JsonIgnore]
	public Leaderboard? Leaderboard { get; set; }
}
