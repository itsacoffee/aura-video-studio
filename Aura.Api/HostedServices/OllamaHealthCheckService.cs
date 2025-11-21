using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that checks Ollama availability and caches the result
/// </summary>
public class OllamaHealthCheckService : BackgroundService
{
    private readonly ILogger<OllamaHealthCheckService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private bool _isAvailable = false;
    private DateTime _lastCheck = DateTime.MinValue;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);

    public OllamaHealthCheckService(
        ILogger<OllamaHealthCheckService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsOllamaAvailable => _isAvailable;
    public DateTime LastCheckTime => _lastCheck;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial check with short delay
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOllamaAvailabilityAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking Ollama availability");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckOllamaAvailabilityAsync(CancellationToken ct)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(3);
            
            var response = await httpClient.GetAsync("http://127.0.0.1:11434/api/tags", ct);
            _isAvailable = response.IsSuccessStatusCode;
            _lastCheck = DateTime.UtcNow;
            
            if (_isAvailable)
            {
                _logger.LogDebug("Ollama service is available at http://127.0.0.1:11434");
            }
        }
        catch
        {
            _isAvailable = false;
            _lastCheck = DateTime.UtcNow;
        }
    }

    public async Task<bool> CheckNowAsync(CancellationToken ct = default)
    {
        await CheckOllamaAvailabilityAsync(ct);
        return _isAvailable;
    }
}
