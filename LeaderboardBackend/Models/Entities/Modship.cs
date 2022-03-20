using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

public class Modship
{
	/// <summary>Generated on creation.</summary>
	public long Id { get; set; }

	/// <summary>The mod's ID.</summary>
	[Required] 
	public Guid UserId { get; set; }

	[JsonIgnore] 
	public User User { get; set; } = null!;

	/// <summary>ID of the Leaderboard the User is a mod for.</summary>
	[Required] 
	public long LeaderboardId { get; set; }

	[JsonIgnore] 
	public Leaderboard Leaderboard { get; set; } = null!;
}
