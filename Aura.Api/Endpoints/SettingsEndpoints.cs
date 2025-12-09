using Aura.Api.Models;
using Aura.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Endpoints;

/// <summary>
/// Settings management endpoints for saving and loading application settings.
/// </summary>
public static class SettingsEndpoints
{
    /// <summary>
    /// Maps settings endpoints to the API route group.
    /// </summary>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");
        var dataRoot = AuraEnvironmentPaths.ResolveDataRoot(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura"));

        // Save settings
        group.MapPost("/settings/save", ([FromBody] Dictionary<string, object> settings) =>
        {
            try
            {
                var settingsPath = Path.Combine(dataRoot, "settings.json");
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath, System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                Log.Information("Settings saved successfully");
                return Results.Ok(new { success = true, message = "Settings saved" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings");
                return Results.Problem("Error saving settings", statusCode: 500);
            }
        })
        .WithName("SaveSettings")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Save application settings";
            operation.Description = "Saves user settings to local storage.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Load settings
        group.MapGet("/settings/load", () =>
        {
            try
            {
                var settingsPath = Path.Combine(dataRoot, "settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    return Results.Ok(settings);
                }
                return Results.Ok(new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings");
                return Results.Problem("Error loading settings", statusCode: 500);
            }
        })
        .WithName("LoadSettings")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Load application settings";
            operation.Description = "Retrieves saved user settings from local storage.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Get portable mode status
        group.MapGet("/settings/portable", () =>
        {
            try
            {
                var portableMarkerPath = Path.Combine(AppContext.BaseDirectory, ".portable");
                var isPortable = File.Exists(portableMarkerPath);

                return Results.Ok(new
                {
                    isPortable,
                    baseDirectory = AppContext.BaseDirectory,
                    dataDirectory = isPortable
                        ? Path.Combine(AppContext.BaseDirectory, "AuraData")
                        : dataRoot
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking portable mode");
                // Return a safe fallback instead of surfacing a 500 to the UI
                return Results.Ok(new
                {
                    isPortable = false,
                    baseDirectory = AppContext.BaseDirectory,
                    dataDirectory = dataRoot,
                    error = "Portable detection failed"
                });
            }
        })
        .WithName("GetPortableMode")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Check if running in portable mode";
            operation.Description = "Returns whether the application is running in portable mode.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Open tools folder
        group.MapPost("/settings/open-tools-folder", () =>
        {
            try
            {
                var toolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "Tools");
                Directory.CreateDirectory(toolsDir);

                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Process.Start("explorer.exe", toolsDir);
                }
                else if (OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("xdg-open", toolsDir);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    System.Diagnostics.Process.Start("open", toolsDir);
                }

                return Results.Ok(new { success = true, path = toolsDir });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening tools folder");
                return Results.Problem("Error opening tools folder", statusCode: 500);
            }
        })
        .WithName("OpenToolsFolder")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Open tools directory in file explorer";
            operation.Description = "Opens the tools directory in the system's file explorer.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Get selected Ollama model
        group.MapGet("/settings/ollama/model", (ProviderSettings providerSettings) =>
        {
            try
            {
                var model = providerSettings.GetOllamaModel();
                return Results.Ok(new { success = true, model });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Ollama model setting");
                return Results.Ok(new { success = false, model = (string?)null, error = ex.Message });
            }
        })
        .WithName("GetOllamaModel")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get selected Ollama model";
            operation.Description = "Retrieves the currently selected Ollama model for script generation.";
            return operation;
        })
        .Produces<object>(200);

        // Allow preflight OPTIONS for Ollama model endpoint
        group.MapMethods("/settings/ollama/model", new[] { "OPTIONS" }, () => Results.NoContent())
            .WithName("OptionsOllamaModel");

        // Set selected Ollama model
        group.MapPost("/settings/ollama/model", ([FromBody] SetOllamaModelRequest request, ProviderSettings providerSettings) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return Results.BadRequest(new { success = false, error = "Model name is required" });
                }

                providerSettings.SetOllamaModel(request.Model);
                Log.Information("Ollama model set to: {Model}", request.Model);
                return Results.Ok(new { success = true, model = request.Model, message = "Ollama model saved successfully" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting Ollama model");
                return Results.Problem("Error saving Ollama model setting", statusCode: 500);
            }
        })
        .WithName("SetOllamaModel")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Set selected Ollama model";
            operation.Description = "Saves the selected Ollama model for script generation.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);

        // Get global LLM selection (provider + model)
        group.MapGet("/settings/llm/selection", (ProviderSettings providerSettings) =>
        {
            try
            {
                // Try to get the global LLM selection from settings
                var settingsPath = LlmSelectionPaths.GetLlmSelectionFilePath();
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var selection = System.Text.Json.JsonSerializer.Deserialize<GlobalLlmSelectionDto>(json);
                    if (selection != null)
                    {
                        return Results.Ok(new { success = true, provider = selection.Provider, modelId = selection.ModelId });
                    }
                }

                // Fallback: Check if Ollama model is configured
                var ollamaModel = providerSettings.GetOllamaModel();
                if (!string.IsNullOrEmpty(ollamaModel))
                {
                    return Results.Ok(new { success = true, provider = "Ollama", modelId = ollamaModel });
                }

                return Results.Ok(new { success = false, provider = (string?)null, modelId = (string?)null });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting global LLM selection");
                return Results.Ok(new { success = false, provider = (string?)null, modelId = (string?)null, error = ex.Message });
            }
        })
        .WithName("GetGlobalLlmSelection")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get global LLM selection";
            operation.Description = "Retrieves the globally selected LLM provider and model.";
            return operation;
        })
        .Produces<object>(200);

        // Allow preflight OPTIONS for LLM selection endpoint
        group.MapMethods("/settings/llm/selection", new[] { "OPTIONS" }, () => Results.NoContent())
            .WithName("OptionsLlmSelection");

        // Set global LLM selection (provider + model)
        group.MapPost("/settings/llm/selection", ([FromBody] GlobalLlmSelectionDto request, ProviderSettings providerSettings) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Provider))
                {
                    return Results.BadRequest(new { success = false, error = "Provider is required" });
                }

                // Save to dedicated settings file
                var settingsPath = LlmSelectionPaths.GetLlmSelectionFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

                var json = System.Text.Json.JsonSerializer.Serialize(new GlobalLlmSelectionDto
                {
                    Provider = request.Provider,
                    ModelId = request.ModelId ?? ""
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(settingsPath, json);

                // Also update Ollama model if Ollama is selected
                if (request.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(request.ModelId))
                {
                    providerSettings.SetOllamaModel(request.ModelId);
                }

                Log.Information("Global LLM selection set to: {Provider} / {ModelId}", request.Provider, request.ModelId);
                return Results.Ok(new { success = true, provider = request.Provider, modelId = request.ModelId, message = "Global LLM selection saved successfully" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting global LLM selection");
                return Results.Problem("Error saving global LLM selection", statusCode: 500);
            }
        })
        .WithName("SetGlobalLlmSelection")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Set global LLM selection";
            operation.Description = "Saves the globally selected LLM provider and model.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);

        return endpoints;
    }
}

/// <summary>
/// DTO for global LLM selection
/// </summary>
public class GlobalLlmSelectionDto
{
    public string Provider { get; set; } = "";
    public string? ModelId { get; set; }
}

/// <summary>
/// Helper class for LLM selection settings paths
/// </summary>
internal static class LlmSelectionPaths
{
    /// <summary>
    /// Gets the path to the LLM selection settings file
    /// </summary>
    public static string GetLlmSelectionFilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "llm-selection.json");
    }
}
