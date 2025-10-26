using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace Aura.Api.Filters;

/// <summary>
/// Action filter that automatically validates requests using FluentValidation
/// and returns standardized 400 Bad Request responses with field-specific errors
/// </summary>
public class ValidationFilter : IActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Skip validation if model state is already invalid
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(CreateValidationErrorResponse(context));
            return;
        }

        // Validate each action parameter using FluentValidation
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argumentValue))
            {
                continue;
            }

            if (argumentValue == null)
            {
                continue;
            }

            var argumentType = argumentValue.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(argumentValue);
                var validationResult = validator.Validate(validationContext);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    context.Result = new BadRequestObjectResult(new
                    {
                        type = "https://docs.aura.studio/errors/E400",
                        title = "Validation Failed",
                        status = 400,
                        detail = "One or more validation errors occurred",
                        errors = errors,
                        correlationId = context.HttpContext.TraceIdentifier
                    });
                    return;
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }

    private object CreateValidationErrorResponse(ActionExecutingContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value?.Errors.Select(er => er.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );

        return new
        {
            type = "https://docs.aura.studio/errors/E400",
            title = "Validation Failed",
            status = 400,
            detail = "One or more validation errors occurred",
            errors = errors,
            correlationId = context.HttpContext.TraceIdentifier
        };
    }
}
