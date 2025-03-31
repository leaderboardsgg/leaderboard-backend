using System.ComponentModel;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LeaderboardBackend.Models.Requests;

public record Page
{
    private int _limit;

    /// <summary>
    ///     The maximum number of records to return. Fewer records may be returned.
    /// </summary>
    public int Limit
    {
        get => _limit;
        set
        {
            _limit = value;
            LimitSet = true;
        }
    }

    [BindNever]
    public bool LimitSet { get; private set; }

    /// <summary>
    ///     The zero-based index at which to begin selecting records to return.
    /// </summary>
    [DefaultValue(0)]
    public int Offset { get; set; } = 0;
}

public class PageValidator : AbstractValidator<Page>
{
    public PageValidator()
    {
        RuleFor(x => x.Limit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
    }
}
