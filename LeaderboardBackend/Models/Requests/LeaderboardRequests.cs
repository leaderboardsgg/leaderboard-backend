namespace LeaderboardBackend.Models.Requests;

/// <summary>
///     This request object is sent when creating a `Leaderboard`.
/// </summary>
public record CreateLeaderboardRequest
{
	/// <summary>
	///     The display name of the `Leaderboard` to create.
	/// </summary>
	/// <example>Foo Bar</example>
	public string Name { get; set; } = null!;

	/// <summary>
	///     The URL-scoped unique identifier of the `Leaderboard`.
	/// </summary>
	/// <example>foo-bar</example>
	public string Slug { get; set; } = null!;
}
