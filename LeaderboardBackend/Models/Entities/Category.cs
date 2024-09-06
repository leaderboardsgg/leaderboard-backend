using System.ComponentModel.DataAnnotations;
using LeaderboardBackend.Models.Validation;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LeaderboardBackend.Models.Entities;

public enum SortDirection
{
    Ascending,
    Descending
}

/// <summary>
///     Represents a `Category` tied to a `Leaderboard`.
/// </summary>
[Index(nameof(Slug), IsUnique = true)]
public class Category : IHasUpdateTimestamp
{
    /// <summary>
    ///     The unique identifier of the `Category`.<br/>
    ///     Generated on creation.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    ///     The ID of the `Leaderboard` the `Category` is a part of.
    /// </summary>
    public long LeaderboardId { get; set; }

    /// <summary>
    ///     Relationship model for `LeaderboardId`.
    /// </summary>
    public Leaderboard? Leaderboard { get; set; }

    /// <summary>
    ///     The display name of the `Category`.
    /// </summary>
    /// <example>Foo Bar Baz%</example>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    ///     The URL-scoped unique identifier of the `Category`.<br/>
    ///     Must be [2, 25] in length and consist only of alphanumeric characters and hyphens.
    /// </summary>
    /// <example>foo-bar-baz</example>
    [StringLength(80, MinimumLength = 2)]
    [RegularExpression(SlugRule.REGEX)]
    public required string Slug { get; set; }

    /// <summary>
    ///     Information pertaining to the `Category`.
    /// </summary>
    /// <example>Video proof is required.</example>
    public string? Info { get; set; }

    /// <summary>
    ///     The direction used to rank runs belonging to this category.
    /// </summary>
    public SortDirection SortDirection { get; set; }

    /// <summary>
    ///     The type of run this category accepts.
    ///     Determines how the TimeOrScore of a Run belonging to this category is interpreted.
    /// </summary>
    public RunType Type { get; set; }

    /// <summary>
    ///     The time the Category was created.
    /// </summary>
    public Instant CreatedAt { get; set; }

    /// <summary>
    ///     The last time the Category was updated or <see langword="null" />.
    /// </summary>
    public Instant? UpdatedAt { get; set; }

    /// <summary>
    ///     The time at which the Category was deleted, or <see langword="null" /> if the Category has not been deleted.
    /// </summary>
    public Instant? DeletedAt { get; set; }
}
