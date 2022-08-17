using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when promoting a `User` to *Moderator* for a `Leaderboard`.
/// </summary>
public record CreateModshipRequest
{
	/// <summary>
	///     The ID of the `Leaderboard` the `User` should become a *Moderator* for.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }

	/// <summary>
	///     The ID of the `User` who should be promoted.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }
}

/// <summary>
///     This request object is sent when demoting a `User` as a *Moderator* from a `Leaderboard`.
/// </summary>
public record RemoveModshipRequest
{
	/// <summary>
	///     The ID of the `Leaderboard` the `User` should be demoted from.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }

	/// <summary>
	///     The ID of the `User` who should be demoted.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }
}
