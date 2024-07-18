using LeaderboardBackend.Models.Entities;

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

    public static CategoryViewModel MapFrom(Category category)
    {
        return new CategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Info = category.Info
        };
    }
}
