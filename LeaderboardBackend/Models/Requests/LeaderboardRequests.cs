using System.ComponentModel.DataAnnotations;
using FluentValidation;
using LeaderboardBackend.Models.Validation;

namespace LeaderboardBackend.Models.Requests;

#nullable disable warnings

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
    public string Name { get; set; }

    /// <summary>
    ///     The URL-scoped unique identifier of the `Leaderboard`.<br/>
    ///     Must be [2, 80] in length and consist only of alphanumeric characters and hyphens.
    /// </summary>
    /// <example>foo-bar</example>
    [Required]
    public string Slug { get; set; }

    /// <inheritdoc cref="Entities.Leaderboard.Info" />
    public string? Info { get; set; }
}

public class CreateLeaderboardRequestValidator : AbstractValidator<CreateLeaderboardRequest>
{
    public CreateLeaderboardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).Slug();
    }
}
