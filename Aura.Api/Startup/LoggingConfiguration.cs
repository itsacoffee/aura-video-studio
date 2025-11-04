using Serilog;
using Serilog.Events;

namespace Aura.Api.Startup;

/// <summary>
/// Configures Serilog for structured logging with multiple output sinks.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with console and file sinks, structured logging, and log enrichment.
    /// </summary>
    public static void ConfigureSerilog(IConfiguration configuration)
    {
        var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Aura.Api")
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/aura-api-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: outputTemplate)
            .WriteTo.File("logs/errors-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: outputTemplate)
            .WriteTo.File("logs/warnings-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                outputTemplate: outputTemplate)
            .WriteTo.File("logs/performance-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: outputTemplate,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .WriteTo.File("logs/audit-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                outputTemplate: outputTemplate)
            .CreateLogger();
    }
}
