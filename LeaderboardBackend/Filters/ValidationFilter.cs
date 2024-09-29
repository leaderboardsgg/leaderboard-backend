using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace LeaderboardBackend.Filters;

public class ValidationFilter(ProblemDetailsFactory responseFactory) : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context) { }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Result == null && !context.ModelState.IsValid)
        {
            // Model binding errors start with a dollar sign whereas model validation errors do not.
            int statusCode = context.ModelState.Any(ms => ms.Key.StartsWith('$')) ? 400 : 422;

            // Using this method instead of just constructing a new ValidationProblemDetails populates it with extra info.
            ValidationProblemDetails problemDetails = responseFactory.CreateValidationProblemDetails(context.HttpContext, context.ModelState, statusCode);

            ObjectResult result = new(problemDetails)
            {
                StatusCode = statusCode
            };

            result.ContentTypes.Add("application/problem+json");
            context.Result = result;
        }
    }
}
