using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

public class SseService
{
    private readonly ILogger<SseService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(30);
    
    // Reduced from 5 minutes to allow faster shutdown - connections will be closed during shutdown anyway
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(2);

    public SseService(ILogger<SseService> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    public async Task StreamProgressAsync<T>(
        HttpResponse response,
        Func<IProgress<T>, CancellationToken, Task> operation,
        CancellationToken ct = default)
    {
        // Set SSE headers
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("Connection", "keep-alive");

        // Link with application stopping token to ensure shutdown is respected
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            ct, 
            _lifetime.ApplicationStopping);
        linkedCts.CancelAfter(ConnectionTimeout);

        var progress = new Progress<T>(async value =>
        {
            try
            {
                // Check if cancellation was requested before attempting to write
                if (linkedCts.Token.IsCancellationRequested)
                {
                    return;
                }

                var json = JsonSerializer.Serialize(value);
                var message = FormatSseMessage("progress", json);
                await response.WriteAsync(message, linkedCts.Token).ConfigureAwait(false);
                await response.Body.FlushAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected or shutdown requested, this is normal
                _logger.LogDebug("Progress update cancelled (client disconnected or shutdown)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send progress update");
            }
        });

        try
        {
            await operation(progress, linkedCts.Token).ConfigureAwait(false);
            
            // Send completion event
            await SendEventAsync(response, "complete", new { success = true }, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled");
            await SendEventAsync(response, "error", new { error = "Operation cancelled" }, linkedCts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE stream");
            await SendEventAsync(response, "error", new { error = ex.Message }, linkedCts.Token).ConfigureAwait(false);
        }
    }

    public async Task SendEventAsync(HttpResponse response, string eventType, object data, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            var message = FormatSseMessage(eventType, json);
            await response.WriteAsync(message, ct).ConfigureAwait(false);
            await response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SSE event");
        }
    }

    public async Task SendKeepAliveAsync(HttpResponse response, CancellationToken ct = default)
    {
        try
        {
            await response.WriteAsync(": keepalive\n\n", ct).ConfigureAwait(false);
            await response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send keep-alive");
        }
    }

    private static string FormatSseMessage(string eventType, string data)
    {
        return $"event: {eventType}\ndata: {data}\n\n";
    }

    public static string FormatProgressMessage(int percentage, string status, string currentFile = "", long bytesDownloaded = 0, long totalBytes = 0)
    {
        var data = new
        {
            percentage,
            status,
            currentFile,
            bytesDownloaded,
            totalBytes
        };
        return JsonSerializer.Serialize(data);
    }

    public static string FormatErrorMessage(string error, string? details = null)
    {
        var data = new
        {
            error,
            details
        };
        return JsonSerializer.Serialize(data);
    }

    public static string FormatCompletionMessage(bool success, string? message = null)
    {
        var data = new
        {
            success,
            message
        };
        return JsonSerializer.Serialize(data);
    }
}
