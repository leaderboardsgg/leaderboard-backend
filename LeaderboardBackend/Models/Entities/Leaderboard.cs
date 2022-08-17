using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a collection of `Category` entities.
/// </summary>
public class Leaderboard
{
	/// <summary>
	///     The unique identifier of the `Leaderboard`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The display name of the `Leaderboard` to create.
	/// </summary>
	/// <example>Foo Bar</example>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The bit in the URL after the domain that can be used to identify a Leaderboard.
	///     Meant to be human-readable. It must be:
	///     <ul>
	///       <li>between 2-80 characters, inclusive</li>
	///       <li>a string of characters separated by hyphens, if desired</li>
	///     </ul>
	/// </summary>
	/// <example>mario-goes-to-jail-ii</example>
	[Required]
	public string Slug { get; set; } = null!;

	/// <summary>
	///     The general rules for the Leaderboard.<br/>
	///     Category-specific rules are tied to the Category.
	/// </summary>
	/// <example>Timer starts on selecting New Game and ends when the first tear drops.</example>
	public string? Rules { get; set; }

	[JsonIgnore]
	public List<Ban>? Bans { get; set; }

	[JsonIgnore]
	public List<Category>? Categories { get; set; }

	[JsonIgnore]
	public List<Modship>? Modships { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is Leaderboard leaderboard
			&& Id == leaderboard.Id
			&& Name == leaderboard.Name
			&& Slug == leaderboard.Slug;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Slug);
	}
}
