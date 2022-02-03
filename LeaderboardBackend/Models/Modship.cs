using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models;

public class Modship
{
	/// <summary>Generated on creation.</summary>
	public ulong Id { get; set; }
	/// <summary>The mod's ID.</summary>
	[Required] public Guid UserId { get; set; }
	[Required] [JsonIgnore] public User User { get; set; } = null!;
	/// <summary>ID of the Leaderboard the User is a mod for.</summary>
	[Required] public ulong LeaderboardId { get; set; }
	[Required] [JsonIgnore] public Leaderboard Leaderboard { get; set; } = null!;
}
