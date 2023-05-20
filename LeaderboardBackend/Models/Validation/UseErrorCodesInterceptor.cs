using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace LeaderboardBackend.Models.Validation;

/// <summary>
/// Replaces error messages with error codes in automatic API controller model validation
/// </summary>
public class UseErrorCodeInterceptor : IValidatorInterceptor
{
    public IValidationContext BeforeAspNetValidation(ActionContext controllerContext, IValidationContext validationContext)
    {
        return validationContext;
    }

    public ValidationResult AfterAspNetValidation(ActionContext controllerContext, IValidationContext validationContext, ValidationResult result)
    {
        foreach (ValidationFailure error in result.Errors)
        {
            error.ErrorMessage = error.ErrorCode;
        }

        return result;
    }
}
