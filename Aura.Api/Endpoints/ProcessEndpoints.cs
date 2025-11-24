using Aura.Core.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.Api.Endpoints;

/// <summary>
/// Debug endpoints for process management and monitoring
/// </summary>
public static class ProcessEndpoints
{
    /// <summary>
    /// Maps process management debug endpoints
    /// </summary>
    public static IEndpointRouteBuilder MapProcessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/debug");

        // Get all active processes
        group.MapGet("/processes", (ProcessRegistry registry) =>
        {
            var processes = registry.GetActiveProcesses();
            return Results.Ok(new
            {
                activeCount = registry.ActiveCount,
                processes = processes.Select(p => new
                {
                    processId = p.ProcessId,
                    name = p.Name,
                    jobId = p.JobId,
                    startedAt = p.StartedAt,
                    duration = DateTime.UtcNow - p.StartedAt
                })
            });
        })
        .WithName("GetActiveProcesses")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get all active tracked processes";
            operation.Description = "Returns a list of all processes currently tracked by the ProcessRegistry. Requires admin authorization.";
            return operation;
        })
        .Produces<object>(200)
        .RequireAuthorization("admin");

        // Get processes for a specific job
        group.MapGet("/processes/job/{jobId}", (string jobId, ProcessRegistry registry) =>
        {
            var processes = registry.GetProcessesForJob(jobId);
            return Results.Ok(new
            {
                jobId,
                count = processes.Count,
                processes = processes.Select(p => new
                {
                    processId = p.ProcessId,
                    name = p.Name,
                    startedAt = p.StartedAt,
                    duration = DateTime.UtcNow - p.StartedAt
                })
            });
        })
        .WithName("GetProcessesForJob")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get processes for a specific job";
            operation.Description = "Returns all processes associated with a specific job ID. Requires admin authorization.";
            return operation;
        })
        .Produces<object>(200)
        .RequireAuthorization("admin");

        // Kill all processes for a job
        group.MapPost("/processes/job/{jobId}/kill", async (string jobId, ProcessRegistry registry, CancellationToken ct) =>
        {
            await registry.KillAllForJobAsync(jobId).ConfigureAwait(false);
            return Results.Ok(new { message = $"Killed all processes for job {jobId}" });
        })
        .WithName("KillProcessesForJob")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Kill all processes for a job";
            operation.Description = "Terminates all processes associated with a specific job ID. Requires admin authorization.";
            return operation;
        })
        .Produces<object>(200)
        .RequireAuthorization("admin");

        // Kill a specific process
        group.MapPost("/processes/{processId}/kill", async (int processId, ProcessRegistry registry, CancellationToken ct) =>
        {
            await registry.KillProcessAsync(processId).ConfigureAwait(false);
            return Results.Ok(new { message = $"Killed process {processId}" });
        })
        .WithName("KillProcess")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Kill a specific process";
            operation.Description = "Terminates a specific process by PID. Requires admin authorization.";
            return operation;
        })
        .Produces<object>(200)
        .RequireAuthorization("admin");

        return endpoints;
    }
}

