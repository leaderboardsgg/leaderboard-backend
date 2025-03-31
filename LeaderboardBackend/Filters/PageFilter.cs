using LeaderboardBackend.Models.Requests;
using LeaderboardBackend.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace LeaderboardBackend.Filters;

/// <summary>
///     Apply this attribute to controller methods that return a paginated list of resources.
/// </summary>
/// <remarks>
///     <para>
///         Controller methods annotated with this attribute must have a <see cref="Page"/> as one
///         of their parameters. If no limit is set on this page upon execution, one will be
///         sourced from the app's config.
///     </para>
///     <para>
///         Methods annotated with this attribute should return an <see cref="ActionResult{TValue}"/>
///         whose TValue is a <see cref="ListView{T}"/>. The <see cref="ListView{T}.LimitDefault"/>
///         and <see cref="ListView{T}.LimitMax"/> will be populated from the config (or the
///         attribute's properties, if set).
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PaginatedAttribute : TypeFilterAttribute
{
    private int? _limitDefault;
    private int? _limitMax;

    /// <summary>
    ///     Set this value to override the default or per-resource default limit.
    /// </summary>
    /// <remarks>
    ///     The value returned when this property is access will be only what it was
    ///     set to, or zero if it has not been set.
    /// </remarks>
    public int LimitDefault
    {
        get => _limitDefault ?? 0;
        set => _limitDefault = value;
    }

    /// <summary>
    ///     Set this value to override the default or per-resource max limit.
    /// </summary>
    /// <remarks>
    ///     The value returned when this property is access will be only what it was
    ///     set to, or zero if it has not been set.
    /// </remarks>
    public int LimitMax
    {
        get => _limitMax ?? 0;
        set => _limitMax = value;
    }

    public PaginatedAttribute() : base(typeof(PageFilter))
    {
        Arguments = [new LimitConfigNullable()
        {
            Default = _limitDefault,
            Max = _limitMax
        }];
    }

    private record LimitConfigNullable
    {
        public int? Default { get; init; }
        public int? Max { get; init; }
    }

    private class PageFilter : IAsyncActionFilter
    {
        private readonly AppConfig _config;
        private readonly LimitConfigNullable _limitConfigNullable;

        public PageFilter(
            IOptions<AppConfig> config,
            LimitConfigNullable limitConfigNullable
        )
        {
            _config = config.Value;
            _limitConfigNullable = limitConfigNullable;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string resource = context.RouteData.Values["controller"]!.ToString()!;
            LimitConfig limitConfig = _config.Limits.GetValueOrDefault(resource, _config.Limits["Default"]);

            int limitDefault = _limitConfigNullable.Default ?? limitConfig.Default;
            int limitMax = _limitConfigNullable.Max ?? limitConfig.Max;

            Page page = (Page)context.ActionArguments.Single(arg => arg.Value!.GetType() == typeof(Page)).Value!;

            if (!page.LimitSet)
            {
                page.Limit = limitDefault;
            }
            else if (page.Limit > limitMax)
            {
                page.Limit = limitMax;
            }

            ActionExecutedContext executedContext = await next();

            if (executedContext.Result is ObjectResult objectResult)
            {
                if (objectResult.Value is IListView listView)
                {
                    listView.LimitDefault = limitDefault;
                    listView.LimitMax = limitMax;
                }
            }
        }
    }
}
