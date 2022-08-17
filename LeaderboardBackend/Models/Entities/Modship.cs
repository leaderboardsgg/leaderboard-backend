using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents the *Moderator* status of a `User`.
/// </summary>
public class Modship
{
	/// <summary>
	///     The unique identifier of the `Modship`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The ID of the *Moderator* (`User`).
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	public User? User { get; set; } = null!;

	/// <summary>
	///     The ID of the `Leaderboard` the `User` is a *Moderator* for.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }

	public Leaderboard? Leaderboard { get; set; } = null!;

	public override bool Equals(object? obj)
	{
		return obj is Modship modship
			&& Id == modship.Id
			&& UserId == modship.UserId
			&& LeaderboardId == modship.LeaderboardId;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, UserId, LeaderboardId);
	}
}
