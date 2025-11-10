using Aura.Api.HostedServices;
using Aura.Api.Hubs;
using Aura.Core.Services.Generation;
using Aura.Core.Services.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering job queue services
/// </summary>
public static class JobQueueServiceCollectionExtensions
{
    /// <summary>
    /// Adds background job queue services and workers
    /// </summary>
    public static IServiceCollection AddJobQueueServices(this IServiceCollection services)
    {
        // Register enhanced resource monitor
        services.AddSingleton<EnhancedResourceMonitor>();
        services.AddSingleton<ResourceMonitor>(sp => sp.GetRequiredService<EnhancedResourceMonitor>());
        
        // Register job queue manager
        services.AddSingleton<BackgroundJobQueueManager>();
        
        // Register SignalR notification service
        services.AddSingleton<JobQueueNotificationService>();
        
        // Register background workers
        services.AddHostedService<BackgroundJobProcessorService>();
        services.AddHostedService<QueueMaintenanceService>();
        
        return services;
    }
    
    /// <summary>
    /// Wires up events from job queue manager to SignalR notifications
    /// </summary>
    public static IServiceProvider WireJobQueueEvents(this IServiceProvider serviceProvider)
    {
        var queueManager = serviceProvider.GetRequiredService<BackgroundJobQueueManager>();
        var notificationService = serviceProvider.GetRequiredService<JobQueueNotificationService>();
        
        // Wire status change events
        queueManager.JobStatusChanged += async (sender, args) =>
        {
            await notificationService.NotifyJobStatusChangedAsync(
                args.JobId,
                args.NewStatus,
                args.CorrelationId,
                args.OutputPath,
                args.ErrorMessage);
                
            // Send specific notifications for completion/failure
            if (args.NewStatus == "Completed" && args.OutputPath != null)
            {
                await notificationService.NotifyJobCompletedAsync(
                    args.JobId,
                    args.OutputPath,
                    args.CorrelationId);
            }
            else if (args.NewStatus == "Failed" && args.ErrorMessage != null)
            {
                await notificationService.NotifyJobFailedAsync(
                    args.JobId,
                    args.ErrorMessage,
                    args.CorrelationId);
            }
        };
        
        // Wire progress update events
        queueManager.JobProgressUpdated += async (sender, args) =>
        {
            await notificationService.NotifyJobProgressAsync(args);
        };
        
        return serviceProvider;
    }
}
