using System.ComponentModel.DataAnnotations;
using FluentValidation;
using LeaderboardBackend.Models.Validation;

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
    [Required]
    public required string Name { get; set; }

    /// <summary>
    ///     The URL-scoped unique identifier of the `Leaderboard`.<br/>
    ///     Must be [2, 80] in length and consist only of alphanumeric characters and hyphens.
    /// </summary>
    /// <example>foo-bar</example>
    public required string Slug { get; set; }

    /// <inheritdoc cref="Entities.Leaderboard.Info" />
    public required string? Info { get; set; }
}
public class CreateLeaderboardRequestValidator : AbstractValidator<CreateLeaderboardRequest>
{
    public CreateLeaderboardRequestValidator() => RuleFor(x => x.Slug).Slug();
}
