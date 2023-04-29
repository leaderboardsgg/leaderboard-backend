using System.ComponentModel.DataAnnotations;

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
	///     The URL-scoped unique identifier of the `Leaderboard`.<br/>
	///     Must be [2, 80] in length and consist only of alphanumeric characters and hyphens.
	/// </summary>
	/// <example>foo-bar</example>
	[Required]
	public string Slug { get; set; } = null!;

	/// <summary>
	///     The general rules for the Leaderboard.
	/// </summary>
	/// <example>Timer starts on selecting New Game and ends when the final boss is beaten.</example>
	public string? Rules { get; set; }

	/// <summary>
	///     A collection of `Category` entities for the `Leaderboard`.
	/// </summary>
	public List<Category>? Categories { get; set; }

	/// <summary>
	///     A collection of *Moderator*s (`Users`) for the `Leaderboard`.
	/// </summary>
	public List<Modship>? Modships { get; set; }

	/// <summary>
	///     A collection of `Ban`s scoped to the `Leaderboard`.
	/// </summary>
	public List<Ban>? Bans { get; set; }

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
