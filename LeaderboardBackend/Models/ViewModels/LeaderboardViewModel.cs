using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>
///     Represents a collection of `Leaderboard` entities.
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
    ///     The general information for the Leaderboard.
    /// </summary>
    /// <example>Timer starts on selecting New Game and ends when the final boss is beaten.</example>
    public required string Info { get; init; }

    /// <summary>
    ///     The time the Leaderboard was created.
    /// </summary>
    public required Instant CreatedAt { get; set; }

    /// <summary>
    ///     The last time the Leaderboard was updated or <see langword="null" />.
    /// </summary>
    public required Instant? UpdatedAt { get; set; }

    /// <summary>
    ///     The time at which the Leaderboard was deleted, or <see langword="null" /> if the Leaderboard has not been deleted.
    /// </summary>
    public required Instant? DeletedAt { get; set; }

    public static LeaderboardViewModel MapFrom(Leaderboard leaderboard) => new()
    {
        Id = leaderboard.Id,
        Name = leaderboard.Name,
        Slug = leaderboard.Slug,
        Info = leaderboard.Info,
        CreatedAt = leaderboard.CreatedAt,
        UpdatedAt = leaderboard.UpdatedAt,
        DeletedAt = leaderboard.DeletedAt,
    };
}
