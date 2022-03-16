using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models;

public class Leaderboard
{
	/// <summary>Generated on creation.</summary>
	public ulong Id { get; set; }

	/// <summary>The Leaderboard's aka game's name. Pretty straightforward.</summary>
	/// <example>Mario Goes to Jail II</example>
	[Required] public string Name { get; set; } = null!;

	/// <summary>
	/// The bit in the URL after the domain that can be used to identify a Leaderboard.
	/// Meant to be human-readable. It must be:
	/// <ul>
	///   <li>between 2-80 characters, inclusive</li>
	///   <li>a string of characters separated by hyphens, if desired</li>
	/// </ul>
	/// </summary>
	/// <example>mario-goes-to-jail-ii</example>
	[Required] public string Slug { get; set; } = null!;

	/// <summary>
	/// The general rules for the Leaderboard.<br/>
	/// Category-specific rules are tied to the Category.
	/// </summary>
	/// <example>Timer starts on selecting New Game and ends when the first tear drops.</example>
	public string? Rules { get; set; }
}
