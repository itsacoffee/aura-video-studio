using Aura.Core.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace Aura.Api.HostedServices;

public class StartupDiagnosticsService : IHostedService
{
    private readonly ILogger<StartupDiagnosticsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public StartupDiagnosticsService(
        ILogger<StartupDiagnosticsService> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Aura API Starting ===");
        _logger.LogInformation("Environment: {Environment}", _environment.EnvironmentName);
        _logger.LogInformation("Content Root: {ContentRoot}", _environment.ContentRootPath);
        _logger.LogInformation("Web Root: {WebRoot}", _environment.WebRootPath);
        _logger.LogInformation("URLs: {Urls}", _configuration["ASPNETCORE_URLS"] ?? "Not configured");
        _logger.LogInformation(".NET Version: {Version}", Environment.Version);
        _logger.LogInformation("OS: {OS}", Environment.OSVersion);
        _logger.LogInformation("Machine: {Machine}", Environment.MachineName);
        _logger.LogInformation("Processors: {Processors}", Environment.ProcessorCount);
        _logger.LogInformation("Working Set: {WorkingSet:N0} bytes", Environment.WorkingSet);
        
        // Check for critical dependencies
        var ffmpegPath = _configuration["FFmpeg:BinaryPath"] ?? "ffmpeg";
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            if (process != null)
            {
                process.WaitForExit(1000);
                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("FFmpeg: Available at {Path}", ffmpegPath);
                }
                else
                {
                    _logger.LogWarning("FFmpeg: Found but returned non-zero exit code");
                }
            }
        }
        catch
        {
            _logger.LogWarning("FFmpeg: Not found or not accessible at {Path}", ffmpegPath);
        }
        
        _logger.LogInformation("=== Startup Diagnostics Complete ===");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Aura API Stopping ===");
        return Task.CompletedTask;
    }
}
