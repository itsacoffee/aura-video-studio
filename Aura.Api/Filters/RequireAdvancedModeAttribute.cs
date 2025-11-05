using System;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Filters;

/// <summary>
/// Filter attribute that requires advanced mode to be enabled
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdvancedModeAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var advancedModeService = context.HttpContext.RequestServices.GetRequiredService<AdvancedModeService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireAdvancedModeAttribute>>();
        
        var isAdvancedModeEnabled = await advancedModeService.IsAdvancedModeEnabledAsync();
        
        if (!isAdvancedModeEnabled)
        {
            var correlationId = context.HttpContext.TraceIdentifier;
            logger.LogWarning(
                "Advanced mode feature accessed without advanced mode enabled, CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId,
                context.HttpContext.Request.Path);
            
            context.Result = new ObjectResult(new
            {
                type = "https://docs.aura.studio/errors/E403",
                title = "Advanced Mode Required",
                status = 403,
                detail = "This feature requires Advanced Mode to be enabled. Please enable Advanced Mode in Settings > General.",
                correlationId = correlationId,
                advancedModeEnabled = false
            })
            {
                StatusCode = 403
            };
            
            return;
        }
        
        await next();
    }
}
