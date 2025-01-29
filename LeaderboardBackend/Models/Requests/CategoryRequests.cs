using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using LeaderboardBackend.Models.Entities;
using LeaderboardBackend.Models.Validation;

namespace LeaderboardBackend.Models.Requests;

#nullable disable warnings

/// <summary>
///     This request object is sent when creating a `Category`.
/// </summary>
public record CreateCategoryRequest
{
    /// <summary>
    ///     The display name of the `Category`.
    /// </summary>
    /// <example>Foo Bar Baz%</example>
    [Required]
    public string Name { get; set; }

    /// <summary>
    ///     The URL-scoped unique identifier of the `Category`.<br/>
    ///     Must be [2, 25] in length and consist only of alphanumeric characters and hyphens.
    /// </summary>
    /// <example>foo-bar-baz</example>
    [Required]
    public string Slug { get; set; }

    /// <summary>
    ///     Information pertaining to the `Category`.
    /// </summary>
    /// <example>Video proof is required.</example>
    public string Info { get; set; }

    /// <inheritdoc cref="Category.SortDirection" />
    [Required]
    public SortDirection SortDirection { get; set; }

    /// <inheritdoc cref="Category.Type" />

    [Required]
    public RunType Type { get; set; }
}

public record UpdateCategoryRequest
{
    /// <inheritdoc cref="Entities.Category.Name" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Name { get; set; }

    /// <inheritdoc cref="Entities.Category.Slug" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Slug { get; set; }

    /// <inheritdoc cref="Entities.Category.Info" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Info { get; set; }

    /// <inheritdoc cref="Entities.Category.SortDirection" />
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SortDirection? SortDirection { get; set; }
}

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty().Slug();
        RuleFor(x => x.SortDirection).Cascade(CascadeMode.Stop).NotEmpty().IsInEnum();
        RuleFor(x => x.Type).Cascade(CascadeMode.Stop).NotEmpty().IsInEnum();
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x).Must(
            ucr => ucr.Info is not null ||
            ucr.Name is not null ||
            ucr.Slug is not null ||
            ucr.SortDirection is not null);
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Name).MinimumLength(1);
        RuleFor(x => x.SortDirection).IsInEnum();
    }
}
