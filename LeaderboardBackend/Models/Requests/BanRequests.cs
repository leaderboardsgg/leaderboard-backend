using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Attributes;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when banning a `User` from the site.
/// </summary>
public record CreateSiteBanRequest
{
	/// <summary>
	///     The ID of the `User` which is banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	///     The reason as to the `User`'s ban.
	/// </summary>
	/// <example>Abusive or hateful conduct.</example>
	[Required]
	public string Reason { get; set; } = null!;
};

/// <summary>
///     This request object is sent when banning a `User` from a `Leaderboard`.
/// </summary>
public record CreateLeaderboardBanRequest
{
	/// <summary>
	///     The ID of the `User` which is banned.
	/// </summary>
	[Required]
	public Guid UserId { get; set; }

	/// <summary>
	///     The reason for the `User`'s ban.
	/// </summary>
	/// <example>Abusive or hateful conduct.</example>
	[Required]
	public string Reason { get; set; } = null!;

	/// <summary>
	///     The ID of the `Leaderboard` from which the `User` is banned.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }
}
