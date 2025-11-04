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

        // Save settings
        group.MapPost("/settings/save", ([FromBody] Dictionary<string, object> settings) =>
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "settings.json");
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
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura", "settings.json");
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
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aura")
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking portable mode");
                return Results.Problem("Error checking portable mode", statusCode: 500);
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

        return endpoints;
    }
}
