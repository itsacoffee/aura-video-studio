using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Aura.Api.Filters;

/// <summary>
/// Swagger operation filter to add correlation ID header to all API operations
/// </summary>
public class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Optional correlation ID for request tracking. If not provided, one will be generated automatically.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });
        
        // Add correlation ID to responses
        if (operation.Responses != null)
        {
            foreach (var response in operation.Responses.Values)
            {
                response.Headers ??= new Dictionary<string, OpenApiHeader>();
                
                if (!response.Headers.ContainsKey("X-Correlation-ID"))
                {
                    response.Headers.Add("X-Correlation-ID", new OpenApiHeader
                    {
                        Description = "Correlation ID for request tracking and log correlation",
                        Schema = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "uuid"
                        }
                    });
                }
            }
        }
    }
}
