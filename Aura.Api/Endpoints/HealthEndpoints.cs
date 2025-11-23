using Aura.Api.Contracts;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Aura.Core.Services.Providers;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Endpoints;

/// <summary>
/// Health check and system diagnostics endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps health check and diagnostics endpoints to the API route group.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        // Basic liveness check - returns 200 if API is running
        // Path defined in BackendEndpoints.HealthLive for consistency with Electron and frontend
        group.MapGet(BackendEndpoints.HealthLive, (HealthCheckService healthService) =>
        {
            var result = healthService.CheckLiveness();
            return Results.Ok(result);
        })
        .WithName("HealthLive")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Check API liveness";
            operation.Description = "Returns a simple liveness indicator. Used by load balancers and orchestrators.";
            return operation;
        })
        .Produces<object>(200);

        // Canonical system health endpoint - comprehensive status information
        group.MapGet("/api/health", async (HealthCheckService healthService, HttpContext context, CancellationToken ct) =>
        {
            try
            {
                var correlationId = context.TraceIdentifier;
                var result = await healthService.GetSystemHealthAsync(correlationId, ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving system health");
                var correlationId = context.TraceIdentifier;
                
                // Always return 200 with error status in body, never leak exceptions
                return Results.Ok(new
                {
                    backendOnline = true,
                    version = "unknown",
                    overallStatus = "error",
                    database = new { status = "unknown", migrationUpToDate = false, message = "Error checking database" },
                    ffmpeg = new { installed = false, valid = false, message = "Error checking FFmpeg" },
                    providersSummary = new { totalConfigured = 0, totalReachable = 0, message = "Error checking providers" },
                    timestamp = DateTimeOffset.UtcNow,
                    correlationId
                });
            }
        })
        .WithName("SystemHealth")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get comprehensive system health";
            operation.Description = "Returns comprehensive system health status including backend, database, FFmpeg, and providers. Always returns 200 with status in body.";
            return operation;
        })
        .Produces<object>(200);

        // Readiness check - returns 200/503 based on dependency availability
        // Path defined in BackendEndpoints.HealthReady for consistency with Electron and frontend
        group.MapGet(BackendEndpoints.HealthReady, async (IServiceProvider services, CancellationToken ct) =>
        {
            var checks = new Dictionary<string, bool>();
            var errors = new List<string>();
            
            // Check database
            try
            {
                var dbContext = services.GetService<Aura.Core.Data.AuraDbContext>();
                if (dbContext != null)
                {
                    await dbContext.Database.CanConnectAsync(ct).ConfigureAwait(false);
                    checks["database"] = true;
                }
                else
                {
                    checks["database"] = false;
                    errors.Add("Database: DbContext not available");
                }
            }
            catch (Exception ex)
            {
                checks["database"] = false;
                errors.Add($"Database: {ex.Message}");
            }
            
            // Check FFmpeg
            try
            {
                var ffmpegResolver = services.GetService<Aura.Core.Dependencies.FFmpegResolver>();
                if (ffmpegResolver != null)
                {
                    var resolution = await ffmpegResolver.ResolveAsync(null, false, ct).ConfigureAwait(false);
                    checks["ffmpeg"] = resolution.Found;
                    if (!resolution.Found)
                    {
                        errors.Add("FFmpeg: Not found on system");
                    }
                }
                else
                {
                    checks["ffmpeg"] = false;
                    errors.Add("FFmpeg: Resolver not available");
                }
            }
            catch (Exception ex)
            {
                checks["ffmpeg"] = false;
                errors.Add($"FFmpeg: {ex.Message}");
            }
            
            // Check settings
            try
            {
                var settingsService = services.GetService<Aura.Core.Services.Settings.ISettingsService>();
                if (settingsService != null)
                {
                    var settings = await settingsService.GetSettingsAsync(ct).ConfigureAwait(false);
                    checks["settings"] = settings != null;
                    if (settings == null)
                    {
                        errors.Add("Settings: Could not load settings");
                    }
                }
                else
                {
                    checks["settings"] = false;
                    errors.Add("Settings: Service not available");
                }
            }
            catch (Exception ex)
            {
                checks["settings"] = false;
                errors.Add($"Settings: {ex.Message}");
            }
            
            var allReady = checks.Values.All(v => v);
            
            return allReady 
                ? Results.Ok(new { ready = true, checks })
                : Results.Json(new { ready = false, checks, errors }, statusCode: 503);
        })
        .WithName("HealthReady")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Check API readiness";
            operation.Description = "Performs comprehensive dependency checks including database, FFmpeg, and settings. Returns 503 if system is not ready to serve requests.";
            return operation;
        })
        .Produces<object>(200)
        .Produces(503);

        // Health summary - high-level system status
        group.MapGet("/health/summary", async (HealthDiagnosticsService healthDiagnostics, CancellationToken ct) =>
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                Log.Information("Health summary requested, CorrelationId: {CorrelationId}", correlationId);

                var result = await healthDiagnostics.GetHealthSummaryAsync(ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving health summary");
                return Results.Problem("Error retrieving health summary", statusCode: 500);
            }
        })
        .WithName("HealthSummary")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get health summary";
            operation.Description = "Returns high-level system health status and metrics.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Health details - per-check detailed diagnostics
        group.MapGet("/health/details", async (HealthDiagnosticsService healthDiagnostics, CancellationToken ct) =>
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                Log.Information("Health details requested, CorrelationId: {CorrelationId}", correlationId);

                var result = await healthDiagnostics.GetHealthDetailsAsync(ct).ConfigureAwait(false);
                var statusCode = result.IsReady ? 200 : 503;
                return Results.Json(result, statusCode: statusCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving health details");
                return Results.Problem("Error retrieving health details", statusCode: 500);
            }
        })
        .WithName("HealthDetails")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get detailed health diagnostics";
            operation.Description = "Returns per-check detailed diagnostics. Returns 503 if system has failed required checks.";
            return operation;
        })
        .Produces<object>(200)
        .Produces(503)
        .ProducesProblem(500);

        // First-run diagnostics - comprehensive system check with actionable guidance
        group.MapGet("/health/first-run", async (FirstRunDiagnostics diagnostics, CancellationToken ct) =>
        {
            try
            {
                var result = await diagnostics.RunDiagnosticsAsync(ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running first-run diagnostics");
                return Results.Problem("Error running diagnostics", statusCode: 500);
            }
        })
        .WithName("FirstRunDiagnostics")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Run first-run diagnostics";
            operation.Description = "Performs comprehensive system check with actionable guidance for first-time setup.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Auto-fix endpoint - attempt to automatically resolve common issues
        group.MapPost("/health/auto-fix", async (
            [FromBody] Dictionary<string, object>? options,
            FirstRunDiagnostics diagnostics,
            FfmpegInstaller ffmpegInstaller,
            CancellationToken ct) =>
        {
            try
            {
                var issueCode = options?.ContainsKey("issueCode") == true
                    ? options["issueCode"]?.ToString()
                    : null;

                if (string.IsNullOrEmpty(issueCode))
                {
                    return Results.BadRequest(new { success = false, message = "Issue code is required" });
                }

                // Handle FFmpeg installation
                if (issueCode == "E302-FFMPEG_NOT_FOUND")
                {
                    Log.Information("Attempting auto-fix for FFmpeg installation");

                    var progress = new Progress<HttpDownloadProgress>(p =>
                    {
                        Log.Information("FFmpeg download progress: {Percent}%", p.PercentComplete);
                    });

                    var mirrors = new List<string>();
                    if (OperatingSystem.IsWindows())
                    {
                        mirrors.Add("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        mirrors.Add("https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz");
                    }

                    if (!mirrors.Any())
                    {
                        return Results.Ok(new
                        {
                            success = false,
                            message = "Automatic FFmpeg installation is not supported on this platform. Please install manually."
                        });
                    }

                    var result = await ffmpegInstaller.InstallFromMirrorsAsync(
                        mirrors.ToArray(),
                        "latest",
                        null,
                        progress,
                        ct).ConfigureAwait(false);

                    if (result.Success)
                    {
                        Log.Information("FFmpeg installed successfully at: {Path}", result.FfmpegPath);
                        return Results.Ok(new
                        {
                            success = true,
                            message = "FFmpeg installed successfully",
                            ffmpegPath = result.FfmpegPath
                        });
                    }
                    else
                    {
                        Log.Error("FFmpeg installation failed: {Error}", result.ErrorMessage);
                        return Results.Ok(new
                        {
                            success = false,
                            message = $"FFmpeg installation failed: {result.ErrorMessage}"
                        });
                    }
                }

                return Results.Ok(new
                {
                    success = false,
                    message = $"Auto-fix not available for issue: {issueCode}"
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during auto-fix");
                return Results.Problem($"Error during auto-fix: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("AutoFixIssue")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Attempt automatic issue resolution";
            operation.Description = "Tries to automatically fix common system issues like missing FFmpeg.";
            return operation;
        })
        .Produces<object>(200)
        .Produces(400)
        .ProducesProblem(500);

        // Legacy health check endpoint for backward compatibility
        group.MapGet("/healthz", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithName("HealthCheck")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Legacy health check";
                operation.Description = "Simple health check endpoint for backward compatibility.";
                return operation;
            })
            .Produces<object>(200);

        // System check endpoint
        group.MapGet("/health/system-check", async (
            IServiceProvider services,
            CancellationToken ct) =>
        {
            try
            {
                // Get FFmpeg status
                var ffmpegResult = new
                {
                    installed = false,
                    version = (string?)null,
                    path = (string?)null,
                    error = "Not checked"
                };

                try
                {
                    var ffmpegResolver = services.GetService<FFmpegResolver>();
                    if (ffmpegResolver != null)
                    {
                        var resolution = await ffmpegResolver.ResolveAsync(null, false, ct).ConfigureAwait(false);
                        ffmpegResult = new
                        {
                            installed = resolution.Found,
                            version = resolution.Version,
                            path = resolution.Path,
                            error = resolution.Found ? (string?)null : "FFmpeg not found"
                        };
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error checking FFmpeg status");
                    ffmpegResult = new
                    {
                        installed = false,
                        version = (string?)null,
                        path = (string?)null,
                        error = ex.Message
                    };
                }

                // Get provider status
                var configuredProviders = new List<string>();
                var validatedProviders = new List<string>();
                var providerErrors = new Dictionary<string, string>();

                try
                {
                    var providerStatusService = services.GetService<ProviderStatusService>();

                    if (providerStatusService != null)
                    {
                        try
                        {
                            var status = await providerStatusService.GetAllProviderStatusAsync(ct).ConfigureAwait(false);
                            
                            // Configured providers: those that are available (regardless of online status)
                            configuredProviders = status.Providers
                                .Where(p => p.IsAvailable)
                                .Select(p => p.Name.ToLower())
                                .ToList();
                            
                            // Validated providers: those that are both available and online (validated and reachable)
                            validatedProviders = status.Providers
                                .Where(p => p.IsOnline && p.IsAvailable)
                                .Select(p => p.Name.ToLower())
                                .ToList();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Error getting provider status from ProviderStatusService");
                            providerErrors["status"] = ex.Message;
                        }
                    }
                    else
                    {
                        // Fallback: check API keys if ProviderStatusService is not available
                        var keyStore = services.GetService<IKeyStore>();
                        if (keyStore != null)
                        {
                            var allKeys = keyStore.GetAllKeys();
                            var providerNames = new[] { "openai", "anthropic", "google", "ollama", "elevenlabs", "playht", "windows", "stabilityai", "stablediffusion", "pexels", "pixabay", "unsplash" };
                            
                            foreach (var providerName in providerNames)
                            {
                                var hasKey = allKeys.ContainsKey(providerName) && !string.IsNullOrWhiteSpace(allKeys[providerName]);
                                var isLocalProvider = providerName.Equals("ollama", StringComparison.OrdinalIgnoreCase) ||
                                                      providerName.Equals("windows", StringComparison.OrdinalIgnoreCase) ||
                                                      providerName.Equals("stablediffusion", StringComparison.OrdinalIgnoreCase);
                                
                                if (hasKey || isLocalProvider)
                                {
                                    configuredProviders.Add(providerName);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error checking provider status");
                    providerErrors["general"] = ex.Message;
                }

                var result = new
                {
                    ffmpeg = ffmpegResult,
                    diskSpace = new
                    {
                        available = 0,
                        total = 0,
                        unit = "GB",
                        sufficient = false
                    },
                    gpu = new
                    {
                        available = false,
                        name = (string?)null,
                        vramGB = (int?)null
                    },
                    providers = new
                    {
                        configured = configuredProviders,
                        validated = validatedProviders,
                        errors = providerErrors
                    }
                };

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during system check");
                return Results.Problem("Error during system check", statusCode: 500);
            }
        })
        .WithName("SystemCheck")
        .WithOpenApi(operation =>
        {
            operation.Summary = "System check";
            operation.Description = "Returns system information including FFmpeg, disk space, GPU, and provider status.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        return endpoints;
    }
}
