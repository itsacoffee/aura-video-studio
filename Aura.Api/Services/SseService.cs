using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

public class SseService
{
    private readonly ILogger<SseService> _logger;
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(5);

    public SseService(ILogger<SseService> logger)
    {
        _logger = logger;
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

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(ConnectionTimeout);

        var progress = new Progress<T>(value =>
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                var message = FormatSseMessage("progress", json);
                response.WriteAsync(message, cts.Token).Wait();
                response.Body.FlushAsync(cts.Token).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send progress update");
            }
        });

        try
        {
            await operation(progress, cts.Token).ConfigureAwait(false);
            
            // Send completion event
            await SendEventAsync(response, "complete", new { success = true }, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled");
            await SendEventAsync(response, "error", new { error = "Operation cancelled" }, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE stream");
            await SendEventAsync(response, "error", new { error = ex.Message }, cts.Token).ConfigureAwait(false);
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
