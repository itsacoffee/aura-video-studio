using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Voice;
using Aura.Core.Services.TTS;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// ElevenLabs voice cloning provider for creating custom voices from audio samples.
/// </summary>
public class ElevenLabsVoiceCloningProvider : IVoiceCloningService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly ILogger<ElevenLabsVoiceCloningProvider> _logger;
    private readonly string _outputDirectory;
    private readonly List<ClonedVoice> _clonedVoices = new();

    private const string BaseUrl = "https://api.elevenlabs.io/v1";
    private static readonly TimeSpan MinSampleDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxSampleDuration = TimeSpan.FromMinutes(5);
    private static readonly string[] SupportedExtensions = { ".wav", ".mp3", ".m4a" };

    public ElevenLabsVoiceCloningProvider(
        HttpClient httpClient,
        string? apiKey,
        ILogger<ElevenLabsVoiceCloningProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "VoiceCloning");

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("xi-api-key"))
            {
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
            }
        }

        Directory.CreateDirectory(_outputDirectory);
    }

    /// <inheritdoc />
    public async Task<ClonedVoice> CreateClonedVoiceAsync(
        string name,
        IReadOnlyList<string> samplePaths,
        VoiceCloneSettings settings,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Voice name is required", nameof(name));
        }

        if (samplePaths == null || samplePaths.Count < 1)
        {
            throw new ArgumentException("At least one audio sample is required", nameof(samplePaths));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("ElevenLabs API key is required for voice cloning");
        }

        _logger.LogInformation(
            "Creating cloned voice '{Name}' from {SampleCount} samples",
            name,
            samplePaths.Count);

        // Validate samples
        foreach (var path in samplePaths)
        {
            await ValidateSampleAsync(path, ct).ConfigureAwait(false);
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(settings.Description ?? $"Cloned voice: {name}"), "description");

        foreach (var samplePath in samplePaths)
        {
            var bytes = await File.ReadAllBytesAsync(samplePath, ct).ConfigureAwait(false);
            var fileContent = new ByteArrayContent(bytes);
            
            var contentType = Path.GetExtension(samplePath).ToLowerInvariant() switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            };
            
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "files", Path.GetFileName(samplePath));
        }

        var response = await _httpClient.PostAsync($"{BaseUrl}/voices/add", content, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError(
                "ElevenLabs voice cloning failed: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);

            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => 
                    new InvalidOperationException("ElevenLabs API key is invalid"),
                System.Net.HttpStatusCode.PaymentRequired => 
                    new InvalidOperationException("ElevenLabs subscription required for voice cloning"),
                System.Net.HttpStatusCode.TooManyRequests => 
                    new InvalidOperationException("ElevenLabs rate limit exceeded"),
                _ => new InvalidOperationException($"Voice cloning failed: {errorContent}")
            };
        }

        var result = await response.Content.ReadFromJsonAsync<ElevenLabsVoiceResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        if (result == null || string.IsNullOrEmpty(result.VoiceId))
        {
            throw new InvalidOperationException("Invalid response from ElevenLabs API");
        }

        _logger.LogInformation(
            "Successfully created cloned voice: {Name} with ID {VoiceId}",
            name,
            result.VoiceId);

        var clonedVoice = new ClonedVoice(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            ProviderId: result.VoiceId,
            Provider: VoiceProvider.ElevenLabs,
            CreatedAt: DateTime.UtcNow,
            Quality: settings.Quality,
            SamplePaths: samplePaths);

        _clonedVoices.Add(clonedVoice);

        return clonedVoice;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ClonedVoice>> GetClonedVoicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ClonedVoice>>(_clonedVoices.ToList());
    }

    /// <inheritdoc />
    public async Task DeleteClonedVoiceAsync(string voiceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            throw new ArgumentException("Voice ID is required", nameof(voiceId));
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("ElevenLabs API key is required");
        }

        var voice = _clonedVoices.FirstOrDefault(v => v.Id == voiceId);
        if (voice == null)
        {
            throw new InvalidOperationException($"Voice with ID '{voiceId}' not found");
        }

        _logger.LogInformation("Deleting cloned voice: {VoiceId}", voiceId);

        var response = await _httpClient.DeleteAsync(
            $"{BaseUrl}/voices/{voice.ProviderId}",
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning(
                "Failed to delete voice from ElevenLabs: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);
        }

        _clonedVoices.Remove(voice);
        _logger.LogInformation("Cloned voice deleted: {VoiceId}", voiceId);
    }

    /// <inheritdoc />
    public async Task<VoiceSampleResult> GenerateSampleAsync(
        string voiceId,
        string sampleText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(voiceId))
        {
            throw new ArgumentException("Voice ID is required", nameof(voiceId));
        }

        if (string.IsNullOrWhiteSpace(sampleText))
        {
            sampleText = "Hello! This is a preview of your cloned voice. How does it sound?";
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("ElevenLabs API key is required");
        }

        var voice = _clonedVoices.FirstOrDefault(v => v.Id == voiceId);
        var providerId = voice?.ProviderId ?? voiceId;

        _logger.LogInformation(
            "Generating sample for voice {VoiceId} with text: {Text}",
            voiceId,
            sampleText.Length > 50 ? sampleText[..50] + "..." : sampleText);

        var payload = new
        {
            text = sampleText,
            model_id = "eleven_monolingual_v1",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75,
                style = 0.0,
                use_speaker_boost = true
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{BaseUrl}/text-to-speech/{providerId}",
            payload,
            ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogError(
                "Sample generation failed: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);
            throw new InvalidOperationException($"Failed to generate sample: {errorContent}");
        }

        var outputPath = Path.Combine(_outputDirectory, $"preview_{voiceId}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp3");
        
        using (var fileStream = File.Create(outputPath))
        {
            await response.Content.CopyToAsync(fileStream, ct).ConfigureAwait(false);
        }

        // Estimate duration based on text length (rough approximation)
        var estimatedDuration = TimeSpan.FromSeconds(sampleText.Length / 15.0);

        _logger.LogInformation("Sample generated: {OutputPath}", outputPath);

        return new VoiceSampleResult
        {
            AudioPath = outputPath,
            Duration = estimatedDuration,
            SampleText = sampleText,
            AudioFormat = "mp3"
        };
    }

    private async Task ValidateSampleAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Sample file not found: {path}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"Unsupported audio format: {extension}. Use WAV, MP3, or M4A.");
        }

        // Check file size as a proxy for duration
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length < 10000) // Very small file, likely too short
        {
            throw new ArgumentException(
                $"Sample file too small ({fileInfo.Length} bytes). Minimum 10 seconds of audio required.");
        }

        if (fileInfo.Length > 50_000_000) // 50MB limit
        {
            throw new ArgumentException(
                $"Sample file too large ({fileInfo.Length / 1_000_000}MB). Maximum 50MB allowed.");
        }

        await Task.CompletedTask;
    }

    private class ElevenLabsVoiceResponse
    {
        public string? VoiceId { get; set; }
    }
}
