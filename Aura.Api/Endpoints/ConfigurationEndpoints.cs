using Aura.Core.Configuration;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aura.Api.Endpoints;

/// <summary>
/// API endpoints for configuration status and dependency checks
/// </summary>
public static class ConfigurationEndpoints
{
    /// <summary>
    /// Maps configuration-related endpoints
    /// </summary>
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config")
            .WithTags("Configuration")
            .WithSummary("Configuration and dependency status endpoints");

        // GET /api/config/status - Get comprehensive configuration validation status
        group.MapGet("/status", async (
            SettingsValidationService validator,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAllAsync(ct).ConfigureAwait(false);
            
            return Results.Ok(new
            {
                isValid = result.CanStart,
                canStart = result.CanStart,
                criticalIssues = result.CriticalIssues.Select(i => new
                {
                    category = i.Category,
                    code = i.Code,
                    message = i.Message,
                    resolution = i.Resolution
                }),
                warnings = result.Warnings.Select(w => new
                {
                    category = w.Category,
                    code = w.Code,
                    message = w.Message,
                    resolution = w.Resolution
                }),
                validationDurationMs = result.ValidationDuration.TotalMilliseconds,
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetConfigurationStatus")
        .WithSummary("Get comprehensive configuration validation status")
        .WithDescription("Returns validation results including critical issues and warnings. Critical issues prevent application startup.")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);

        // GET /api/config/dependencies - Get individual dependency status
        group.MapGet("/dependencies", async (
            SettingsValidationService validator,
            OllamaDetectionService? ollamaDetection,
            CancellationToken ct) =>
        {
            var ffmpegResult = await validator.CheckFfmpegAsync(ct).ConfigureAwait(false);
            var databaseResult = validator.CheckDatabase();
            var outputDirResult = validator.CheckOutputDirectory();

            // Check Ollama if available
            var ollamaResult = new
            {
                isAvailable = false,
                message = "Ollama detection service not available",
                details = (string?)null
            };

            if (ollamaDetection != null)
            {
                try
                {
                    var detectionCompleted = await ollamaDetection.WaitForInitialDetectionAsync(
                        TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
                    
                    if (detectionCompleted)
                    {
                        var status = await ollamaDetection.GetStatusAsync(ct).ConfigureAwait(false);
                        ollamaResult = new
                        {
                            isAvailable = status.IsRunning,
                            message = status.IsRunning ? "Ollama service is running" : "Ollama service is not running",
                            details = status.IsRunning ? $"Base URL: {status.BaseUrl}" : null
                        };
                    }
                    else
                    {
                        ollamaResult = new
                        {
                            isAvailable = false,
                            message = "Ollama detection timed out",
                            details = "Ollama may be starting up. Check if 'ollama serve' is running."
                        };
                    }
                }
                catch (Exception ex)
                {
                    ollamaResult = new
                    {
                        isAvailable = false,
                        message = $"Ollama check failed: {ex.Message}",
                        details = (string?)null
                    };
                }
            }

            return Results.Ok(new
            {
                ffmpeg = new
                {
                    isAvailable = ffmpegResult.IsAvailable,
                    message = ffmpegResult.Message,
                    version = ffmpegResult.Details
                },
                ollama = ollamaResult,
                database = new
                {
                    isAvailable = databaseResult.IsAvailable,
                    message = databaseResult.Message,
                    path = databaseResult.Details
                },
                outputDirectory = new
                {
                    isAvailable = outputDirResult.IsAvailable,
                    message = outputDirResult.Message,
                    path = outputDirResult.Details
                },
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetDependencyStatus")
        .WithSummary("Get individual dependency status")
        .WithDescription("Returns status of individual dependencies (FFmpeg, Ollama, database, output directory)")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}

