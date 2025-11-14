using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Validation;

/// <summary>
/// Validates Mimic3 TTS connectivity and functionality
/// </summary>
public class Mimic3Validator : IProviderValidator
{
    private readonly ILogger<Mimic3Validator> _logger;
    private readonly HttpClient _httpClient;

    public string ProviderName => "Mimic3";

    public Mimic3Validator(ILogger<Mimic3Validator> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string? apiKey, string? configUrl, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var baseUrl = configUrl ?? "http://127.0.0.1:59125";

        try
        {
            // Check if Mimic3 server is running by hitting the voices endpoint
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/voices", cts.Token).ConfigureAwait(false);

            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                
                _logger.LogInformation("Mimic3 validation successful");
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = true,
                    Details = "Mimic3 TTS server is running",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
            else
            {
                _logger.LogWarning("Mimic3 validation failed: GET /api/voices returned {StatusCode}", response.StatusCode);
                return new ProviderValidationResult
                {
                    Name = ProviderName,
                    Ok = false,
                    Details = $"Mimic3 not responding (HTTP {response.StatusCode})",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Mimic3 validation timed out");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = "Request timed out",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Mimic3 validation failed: Network error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Cannot connect to Mimic3 at {baseUrl}. Start from Downloads page.",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Mimic3 validation failed: Unexpected error");
            return new ProviderValidationResult
            {
                Name = ProviderName,
                Ok = false,
                Details = $"Error: {ex.Message}",
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }
    }
}
