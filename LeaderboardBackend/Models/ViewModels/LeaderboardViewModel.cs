using LeaderboardBackend.Models.Entities;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>
///     Represents a collection of `Category` entities.
/// </summary>
public record LeaderboardViewModel
{
	/// <summary>
	///     The unique identifier of the `Leaderboard`.<br/>
	///     Generated on creation.
	/// </summary>
	public required long Id { get; set; }

	/// <summary>
	///     The display name of the `Leaderboard` to create.
	/// </summary>
	/// <example>Foo Bar</example>
	public required string Name { get; init; }

	/// <summary>
	///     The URL-scoped unique identifier of the `Leaderboard`.<br/>
	///     Must be [2, 80] in length and consist only of alphanumeric characters and hyphens.
	/// </summary>
	/// <example>foo-bar</example>
	public required string Slug { get; init; }

	/// <summary>
	///     The general rules for the Leaderboard.
	/// </summary>
	/// <example>Timer starts on selecting New Game and ends when the final boss is beaten.</example>
	public required string? Rules { get; init; }

	/// <summary>
	///     A collection of `Category` entities for the `Leaderboard`.
	/// </summary>
	public required IList<CategoryViewModel> Categories { get; init; }

	public static LeaderboardViewModel MapFrom(Leaderboard leaderboard)
	{
		IList<CategoryViewModel>? categories = leaderboard.Categories?
			.Select(CategoryViewModel.MapFrom)
			.ToList();
		return new LeaderboardViewModel
		{
			Id = leaderboard.Id,
			Name = leaderboard.Name,
			Slug = leaderboard.Slug,
			Rules = leaderboard.Rules,
			Categories = categories ?? Array.Empty<CategoryViewModel>(),
		};
	}
}
