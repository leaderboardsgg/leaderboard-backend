using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    /// <remarks>
    /// If omitted, will result in an empty string.
    /// </remarks>
    public string Info { get; set; } = null;
}

public record UpdateLeaderboardRequest
{
    /// <inheritdoc cref="Entities.Leaderboard.Name" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Name { get; set; }

    /// <inheritdoc cref="Entities.Leaderboard.Slug" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Slug { get; set; }

    /// <inheritdoc cref="Entities.Leaderboard.Info" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Info { get; set; }
}

public class UpdateLeaderboardRequestValidator : AbstractValidator<UpdateLeaderboardRequest>
{
    public UpdateLeaderboardRequestValidator()
    {
        RuleFor(x => x).Must(ulr => ulr.Info is not null || ulr.Name is not null || ulr.Slug is not null);
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Name).MinimumLength(1);
    }
}

public class CreateLeaderboardRequestValidator : AbstractValidator<CreateLeaderboardRequest>
{
    public CreateLeaderboardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().Slug();
    }
}
