using Aura.Core.Hardware;
using Serilog;

namespace Aura.Api.Endpoints;

/// <summary>
/// Hardware capabilities detection endpoints.
/// </summary>
public static class CapabilitiesEndpoints
{
    /// <summary>
    /// Maps hardware capabilities endpoints to the API route group.
    /// </summary>
    public static IEndpointRouteBuilder MapCapabilitiesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        // Get system capabilities
        group.MapGet("/capabilities", async (HardwareDetector detector) =>
        {
            try
            {
                var profile = await detector.DetectSystemAsync().ConfigureAwait(false);
                return Results.Ok(new
                {
                    tier = profile.Tier.ToString(),
                    cpu = new { cores = profile.PhysicalCores, threads = profile.LogicalCores },
                    ram = new { gb = profile.RamGB },
                    gpu = profile.Gpu != null ? new { model = profile.Gpu.Model, vramGB = profile.Gpu.VramGB, vendor = profile.Gpu.Vendor } : null,
                    enableNVENC = profile.EnableNVENC,
                    enableSD = profile.EnableSD,
                    offlineOnly = profile.OfflineOnly
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error detecting capabilities");
                return Results.Problem("Error detecting system capabilities", statusCode: 500);
            }
        })
        .WithName("GetCapabilities")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get system hardware capabilities";
            operation.Description = "Detects and returns system hardware profile including CPU, RAM, GPU, and supported features.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        // Run hardware probes
        group.MapPost("/probes/run", async (HardwareDetector detector) =>
        {
            try
            {
                await detector.RunHardwareProbeAsync().ConfigureAwait(false);
                var profile = await detector.DetectSystemAsync().ConfigureAwait(false);

                var gpuDisplay = profile.Gpu != null
                    ? $"{profile.Gpu.Vendor} {profile.Gpu.Model}"
                    : "None";

                return Results.Ok(new
                {
                    success = true,
                    tier = profile.Tier.ToString(),
                    cores = profile.PhysicalCores,
                    threads = profile.LogicalCores,
                    ramGB = profile.RamGB,
                    gpu = gpuDisplay,
                    gpuVramGB = profile.Gpu?.VramGB,
                    enableNVENC = profile.EnableNVENC,
                    enableSD = profile.EnableSD,
                    offlineOnly = profile.OfflineOnly,
                    profile = profile
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running hardware probes");
                return Results.Problem("Error running hardware probes", statusCode: 500);
            }
        })
        .WithName("RunProbes")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Run hardware diagnostic probes";
            operation.Description = "Executes comprehensive hardware detection and returns detailed probe results.";
            return operation;
        })
        .Produces<object>(200)
        .ProducesProblem(500);

        return endpoints;
    }
}
