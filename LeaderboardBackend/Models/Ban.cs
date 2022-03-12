using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models;

public class Ban
{
	/// <summary>Generated on creation.</summary>
	public ulong Id { get; set; }
	/// <summary>Can't be <code>null</code>.</summary>
	[Required] public string Reason { get; set; } = null!;
	/// <summary>Generated on creation.</summary>
	[Required] public DateTime Time { get; set; } = default!;
	/// <summary>ID of User who set the Ban. Must be either an admin or a mod.</summary>
	[Required] public Guid BanningUserId { get; set; }
	[JsonIgnore] public User BanningUser { get; set; } = null!;
	/// <summary>ID of User who received the Ban.</summary>
	[Required] public Guid BannedUserId { get; set; }
	[JsonIgnore] public User BannedUser { get; set; } = null!;
	/// <summary>
	/// ID of Leaderboard this Ban belongs to. If <code>null</code>, then this Ban is site-wide.
	/// </summary>
	public ulong? LeaderboardId { get; set; }
	[JsonIgnore] public Leaderboard? Leaderboard { get; set; }
}
