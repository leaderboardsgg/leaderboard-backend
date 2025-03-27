using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace LeaderboardBackend.Models.Requests;

public record Page
{
    private int _limit;

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

    public int Offset { get; set; } = 0;
}

public class PageValidator : AbstractValidator<Page>
{
    public PageValidator(IOptions<AppConfig> config, IHttpContextAccessor contextAccessor)
    {
        When(page => page.LimitSet, () =>
        {
            HttpContext context = contextAccessor.HttpContext!;
            string resource = context.GetRouteValue("controller")!.ToString()!;
            LimitConfig limitConfig = config.Value.Limits.GetValueOrDefault(resource, config.Value.Limits["Default"]);

            RuleFor(x => x.Limit).GreaterThanOrEqualTo(0).LessThanOrEqualTo(limitConfig.Max);
        });

        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
    }
}
