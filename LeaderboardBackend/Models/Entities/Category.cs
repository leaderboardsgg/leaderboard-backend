using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

/// <summary>A Category tied to a Leaderboard.</summary>
public class Category
{
	/// <summary>The Category's ID. Generated on creation.</summary>
	public long Id { get; set; }

	/// <summary>The Category's name.</summary>
	/// <example>Mongolian Throat Singing%</example>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>
	/// The Category's slug. <br/>
	/// Must be 2-25 characters inclusive and only consist of letters, numbers, and
	/// hyphens.
	/// </summary>
	/// <example>mongolian-throat-singing</example>
	[Required]
	public string Slug { get; set; } = null!;

	/// <summary>Category-specific rules.</summary>
	public string? Rules { get; set; }

	/// <summary>Minimum player count for this Category. Defaults to 1.</summary>
	[Required]
	public int PlayersMin { get; set; }

	/// <summary>
	/// Maximum player count for this Category. Defaults to PlayersMin.
	/// </summary>
	[Required]
	public int PlayersMax { get; set; }

	/// <summary>ID of the Leaderboard this Category belongs to.</summary>
	[Required]
	public long LeaderboardId { get; set; }

	[JsonIgnore]
	public Leaderboard? Leaderboard { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is Category category &&
			   Id == category.Id &&
			   Name == category.Name &&
			   Slug == category.Slug &&
			   PlayersMax == category.PlayersMax &&
			   PlayersMin == category.PlayersMin &&
			   LeaderboardId == category.LeaderboardId;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Slug, LeaderboardId);
	}
}
