# Swagger API Documentation Guide

This guide explains how to access, use, and contribute to the Swagger/OpenAPI documentation for Aura Video Studio.

## Accessing Swagger UI

### Local Development

When running the API locally, Swagger UI is available at:

```
http://localhost:5005/swagger
```

### Features

The Swagger UI provides:

- **Interactive API Explorer**: Test API endpoints directly from the browser
- **Request/Response Examples**: See sample payloads and responses
- **Schema Documentation**: Explore data models and their properties
- **Authentication Support**: Test authenticated endpoints
- **Try It Out**: Execute real API calls with custom parameters

## Using Swagger UI

### 1. Exploring Endpoints

Navigate through the endpoint categories:

- **Health** - System health and diagnostics
- **Video Generation** - Script, plan, TTS, and rendering
- **Settings** - Configuration management
- **Providers** - Provider configuration
- **Assets** - Asset library management
- **Jobs** - Background job monitoring

### 2. Testing Endpoints

To test an endpoint:

1. Click on the endpoint to expand it
2. Click "Try it out"
3. Fill in required parameters
4. Click "Execute"
5. View the response below

Example: Testing the health endpoint

```http
GET /api/v1/health
```

Response:

```json
{
  "status": "healthy",
  "version": "1.0.0",
  "correlationId": "abc-123",
  "timestamp": "2024-03-15T10:30:00Z"
}
```

### 3. Understanding Request Models

Click on "Schemas" at the bottom to view all data models:

- `VideoRequest` - Video generation request
- `ScriptRequest` - Script generation request
- `Brief` - Video brief structure
- `Timeline` - Video timeline structure
- And more...

### 4. Downloading API Specification

Download the OpenAPI specification in JSON or YAML:

```bash
# JSON format
curl http://localhost:5005/swagger/v1/swagger.json -o openapi.json

# Convert to YAML (requires yq)
curl http://localhost:5005/swagger/v1/swagger.json | yq eval -P - > openapi.yaml
```

## API Authentication

### Local Development

Local API does not require authentication. All endpoints are accessible without credentials.

### Production Deployment

For production deployments, implement one of:

1. **API Keys** - Add `X-API-Key` header
2. **JWT Tokens** - Bearer token authentication
3. **OAuth 2.0** - For third-party integrations

Update Swagger configuration to reflect your authentication scheme:

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});
```

## Code Generation

### Generate Client SDKs

Use the OpenAPI specification to generate client libraries:

#### TypeScript/JavaScript

```bash
# Install OpenAPI Generator
npm install -g @openapitools/openapi-generator-cli

# Generate TypeScript client
openapi-generator-cli generate \
  -i http://localhost:5005/swagger/v1/swagger.json \
  -g typescript-fetch \
  -o ./generated/typescript-client
```

#### Python

```bash
# Generate Python client
openapi-generator-cli generate \
  -i http://localhost:5005/swagger/v1/swagger.json \
  -g python \
  -o ./generated/python-client
```

#### C#

```bash
# Using NSwag
dotnet tool install -g NSwag.ConsoleCore

nswag openapi2csclient \
  /input:http://localhost:5005/swagger/v1/swagger.json \
  /output:AuraApiClient.cs \
  /namespace:Aura.Client
```

### Generate API Documentation

```bash
# Generate static HTML documentation using ReDoc
npx @redocly/cli build-docs http://localhost:5005/swagger/v1/swagger.json \
  -o api-docs.html
```

## Contributing to API Documentation

### Adding XML Documentation

Add XML comments to controllers and models:

```csharp
/// <summary>
/// Generates a video script from a brief
/// </summary>
/// <param name="request">The script generation request</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>Generated script with scenes and narration</returns>
/// <response code="200">Returns the generated script</response>
/// <response code="400">Invalid request parameters</response>
/// <response code="500">Internal server error</response>
[HttpPost("/api/v1/script")]
[ProducesResponseType(typeof(ScriptResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GenerateScript(
    [FromBody] ScriptRequest request,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Enabling XML Comments

Ensure XML documentation is generated:

```xml
<!-- Aura.Api.csproj -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### Using WithOpenApi()

For minimal APIs, use `WithOpenApi()` to add documentation:

```csharp
app.MapGet("/api/v1/health", async () =>
{
    return Results.Ok(new { status = "healthy" });
})
.WithName("GetHealth")
.WithTags("Health")
.WithOpenApi(operation =>
{
    operation.Summary = "Health check endpoint";
    operation.Description = "Returns the health status of the API";
    return operation;
});
```

### Adding Examples

Provide request/response examples:

```csharp
options.SwaggerDoc("v1", new OpenApiInfo
{
    // ... other properties ...
    Examples = new Dictionary<string, IOpenApiAny>
    {
        ["VideoRequest"] = new OpenApiObject
        {
            ["brief"] = new OpenApiObject
            {
                ["title"] = new OpenApiString("My First Video"),
                ["description"] = new OpenApiString("A tutorial about coding")
            }
        }
    }
});
```

## Advanced Configuration

### Custom Operation Filters

Add custom metadata to all operations:

```csharp
public class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.Add("200", new OpenApiResponse
        {
            Description = "Success",
            Headers = new Dictionary<string, OpenApiHeader>
            {
                ["X-Correlation-ID"] = new OpenApiHeader
                {
                    Description = "Unique correlation ID for request tracing",
                    Schema = new OpenApiSchema { Type = "string" }
                }
            }
        });
    }
}
```

Register the filter:

```csharp
options.OperationFilter<CorrelationIdOperationFilter>();
```

### Grouping Endpoints

Group related endpoints using tags:

```csharp
.WithTags("Video Generation")
.WithTags("Settings")
.WithTags("Providers")
```

### Versioning

Support multiple API versions:

```csharp
options.SwaggerDoc("v1", new OpenApiInfo
{
    Version = "v1",
    Title = "Aura Video Studio API v1"
});

options.SwaggerDoc("v2", new OpenApiInfo
{
    Version = "v2",
    Title = "Aura Video Studio API v2"
});
```

## Troubleshooting

### Swagger UI Not Loading

**Problem**: Swagger UI shows blank page or 404.

**Solution**:
1. Ensure `app.UseSwagger()` is called before `app.UseSwaggerUI()`
2. Check that `ASPNETCORE_ENVIRONMENT` is set to `Development` or configure for production
3. Verify the URL: `http://localhost:5005/swagger/index.html`

### XML Comments Not Showing

**Problem**: Method descriptions missing in Swagger UI.

**Solution**:
1. Enable XML documentation in `.csproj`:
   ```xml
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
   ```
2. Verify XML file exists in output directory
3. Check Swagger configuration includes XML comments:
   ```csharp
   options.IncludeXmlComments(xmlPath);
   ```

### Schema Issues

**Problem**: Complex types not showing correctly.

**Solution**:
1. Ensure types are public
2. Add `[JsonPropertyName]` attributes if needed
3. Use `[SwaggerSchema]` for custom descriptions:
   ```csharp
   [SwaggerSchema("A video generation brief")]
   public class Brief { }
   ```

### CORS Errors in Swagger UI

**Problem**: "Failed to fetch" errors in Try It Out.

**Solution**:
1. Enable CORS in Program.cs
2. Allow Swagger origins:
   ```csharp
   app.UseCors(policy => policy
       .AllowAnyOrigin()
       .AllowAnyMethod()
       .AllowAnyHeader());
   ```

## Best Practices

### Documentation Quality

- **Be specific**: Describe what the endpoint does, not how
- **Include examples**: Show real-world request/response data
- **Document errors**: List all possible error codes
- **Explain parameters**: Don't just repeat the parameter name
- **Use proper HTTP methods**: GET for reads, POST for creates, etc.

### API Design

- **Consistent naming**: Use clear, consistent endpoint names
- **RESTful patterns**: Follow REST conventions
- **Versioning**: Plan for future API versions
- **Pagination**: Document pagination parameters
- **Filtering**: Explain query string filters

### Security

- **Sensitive data**: Never expose secrets in examples
- **Rate limits**: Document rate limiting policies
- **Authentication**: Clearly explain auth requirements
- **HTTPS only**: Recommend HTTPS for production

## Resources

- [OpenAPI Specification](https://swagger.io/specification/)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Generator](https://openapi-generator.tech/)
- [ReDoc](https://github.com/Redocly/redoc)
- [Swagger Editor](https://editor.swagger.io/)

## Related Documentation

- [API Contract v1](./API_CONTRACT_V1.md)
- [Error Handling](./errors.md)
- [Rate Limiting](./rate-limits.md)
- [Health Checks](./health.md)
