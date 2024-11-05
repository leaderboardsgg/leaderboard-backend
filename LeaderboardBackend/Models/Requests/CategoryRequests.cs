using FluentValidation;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Validation;

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
    public required string Name { get; set; } = null!;

    /// <summary>
    ///     The URL-scoped unique identifier of the `Category`.<br/>
    ///     Must be [2, 25] in length and consist only of alphanumeric characters and hyphens.
    /// </summary>
    /// <example>foo-bar-baz</example>
    public required string Slug { get; set; } = null!;

    /// <summary>
    ///     Information pertaining to the `Category`.
    /// </summary>
    /// <example>Video proof is required.</example>
    public required string? Info { get; set; }

    /// <summary>
    ///     The ID of the `Leaderboard` the `Category` is a part of.
    /// </summary>
    public required long LeaderboardId { get; set; }

    /// <inheritdoc cref="Category.SortDirection" />
    public required SortDirection SortDirection { get; set; }

    /// <inheritdoc cref="Category.Type" />
    public required RunType Type { get; set; }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator() => RuleFor(x => x.Slug).NotEmpty().Slug();
}
