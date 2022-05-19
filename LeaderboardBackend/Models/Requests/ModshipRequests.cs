using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>Request object sent when setting a User as mod for a Leaderboard.</summary>
public record CreateModshipRequest
{
	/// <summary>The Leaderboard ID.</summary>
	[Required]
	public long LeaderboardId { get; set; }
	/// <summary>The User ID.</summary>
	[Required]
	public Guid UserId { get; set; }
}

/// <summary>Request object sent when removing a User as mod for a Leaderboard.</summary>
public record RemoveModshipRequest
{
	/// <summary>The Leaderboard ID.</summary>
	[Required]
	public long LeaderboardId { get; set; }
	/// <summary>The User ID.</summary>
	[Required]
	public Guid UserId { get; set; }
}