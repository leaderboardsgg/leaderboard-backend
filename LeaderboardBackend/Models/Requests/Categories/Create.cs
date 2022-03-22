using LeaderboardBackend.Models.Annotations;
using System.ComponentModel.DataAnnotations;

namespace LeaderboardBackend.Models.Requests.Categories;

/// <summary>Request object sent when creating a Category.</summary>
public record CreateCategoryRequest
{
	/// <summary>Name for the new Category.</summary>
	/// <example>Mongolian Throat Singing%</example>
	[Required]
	public string Name { get; set; } = null!;

	/// <summary>
	/// The bit in the URL that uniquely identifies this Category. <br/>
	/// E.g.: https://leaderboards.gg/slug-for-board/slug-for-category <br/>
	/// Must be 2-25 characters inclusive and only consist of letters, numbers, and
	/// hyphens.
	/// </summary>
	/// <example>mongolian-throat-singing</example>
	[Required]
	public string Slug { get; set; } = null!;

	/// <summary>Category-specific rules.</summary>
	public string? Rules { get; set; }

	/// <summary>Minimum player count for this Category. Defaults to 1.</summary>
	[Range(1, int.MaxValue)]
	public int? PlayersMin { get; set; }

	/// <summary>
	/// Maximum player count for this Category. Defaults to PlayersMin.
	/// </summary>
	[Range(1, int.MaxValue)]
	[PlayersMax]
	public int? PlayersMax { get; set; }

	/// <summary>ID of the Leaderboard this Category belongs to.</summary>
	[Required]
	public long LeaderboardId { get; set; }
}
