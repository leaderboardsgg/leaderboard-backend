using System.ComponentModel.DataAnnotations;
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
