using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Annotations;

namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Category`.
/// </summary>
public record CreateCategoryRequest
{
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
	[Range(1, int.MaxValue)]
	public int? PlayersMin { get; set; }

	/// <summary>
	///     The maximum player count of the `Category`. The default is `PlayersMin`.
	/// </summary>
	[Range(1, int.MaxValue)]
	[PlayersMax]
	public int? PlayersMax { get; set; }

	/// <summary>
	///     The ID of the `Leaderboard` the `Category` is a part of.
	/// </summary>
	[Required]
	public long LeaderboardId { get; set; }
}
