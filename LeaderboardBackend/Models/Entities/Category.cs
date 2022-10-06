using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LeaderboardBackend.Models.Entities;

/// <summary>
///     Represents a `Category` tied to a `Leaderboard`.
/// </summary>
public class Category : BaseEntity
{
	/// <summary>
	///     The unique identifier of the `Category`.<br/>
	///     Generated on creation.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	///     The ID of the `Leaderboard` the `Category` is a part of.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }

	/// <summary>
	///     Relationship model for `LeaderboardId`.
	/// </summary>
	[JsonIgnore]
	public Leaderboard? Leaderboard { get; set; }

	/// <summary>
	///     The display name of the `Category`.
	/// </summary>
	/// <example>Foo Bar Baz%</example>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The URL-scoped unique identifier of the `Category`.<br/>
	///     Must be [2, 25] in length and consist only of alphanumeric characters and hyphens.
	/// </summary>
	/// <example>foo-bar-baz</example>
	[Required]
	public string Slug { get; set; } = null!;

	/// <summary>
	///     The rules of the `Category`.
	/// </summary>
	/// <example>Video proof is required.</example>
	public string? Rules { get; set; }

	/// <summary>
	///     The minimum player count of the `Category`. The default is 1.
	/// </summary>
	[Required]
	public int PlayersMin { get; set; }

	/// <summary>
	///     The maximum player count of the `Category`. The default is `PlayersMin`.
	/// </summary>
	[Required]
	public int PlayersMax { get; set; }

	/// <summary>
	///     A collection of `NumericalMetric`s scoped to the `Category`.
	/// </summary>
	public List<NumericalMetric>? NumericalMetrics { get; set; }

	public override bool Equals(object? obj)
	{
		return obj is Category category
			&& base.Equals(category)
			&& Id == category.Id
			&& Name == category.Name
			&& Slug == category.Slug
			&& PlayersMax == category.PlayersMax
			&& PlayersMin == category.PlayersMin
			&& LeaderboardId == category.LeaderboardId;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Name, Slug, LeaderboardId);
	}
}
