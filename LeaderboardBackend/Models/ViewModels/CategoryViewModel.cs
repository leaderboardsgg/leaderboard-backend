using LeaderboardBackend.Models.Entities;
using NodaTime;

namespace LeaderboardBackend.Models.ViewModels;

/// <summary>
///     Represents a `Category` tied to a `Leaderboard`.
/// </summary>
public record CategoryViewModel
{
    /// <summary>
    ///     The unique identifier of the `Category`.<br/>
    /// </summary>
    public required long Id { get; set; }

    /// <summary>
    ///     The display name of the `Category`.
    /// </summary>
    /// <example>Foo Bar Baz%</example>
    public required string Name { get; set; }

    /// <summary>
    ///     The URL-scoped unique identifier of the `Category`.<br/>
    /// </summary>
    /// <example>foo-bar-baz</example>
    public required string Slug { get; set; }

    /// <summary>
    ///     Information pertaining to the `Category`.
    /// </summary>
    /// <example>Video proof is required.</example>
    public required string? Info { get; set; }

    public required RunType Type { get; set; }

    /// <inheritdoc cref="Category.SortDirection" />
    public required SortDirection SortDirection { get; set; }

    /// <inheritdoc cref="Category.CreatedAt" />
    public required Instant CreatedAt { get; set; }

    /// <inheritdoc cref="Category.UpdatedAt" />
    public required Instant? UpdatedAt { get; set; }

    /// <inheritdoc cref="Category.DeletedAt" />
    public required Instant? DeletedAt { get; set; }

    public static CategoryViewModel MapFrom(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Slug = category.Slug,
        Info = category.Info,
        SortDirection = category.SortDirection,
        Type = category.Type,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt,
        DeletedAt = category.DeletedAt
    };
}
