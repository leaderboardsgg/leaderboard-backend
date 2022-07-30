using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class Modship
{
	/// <summary>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The mod's ID.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	public User? User { get; set; } = null!;

	/// <summary>
	///     ID of the Leaderboard the User is a mod for.
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
