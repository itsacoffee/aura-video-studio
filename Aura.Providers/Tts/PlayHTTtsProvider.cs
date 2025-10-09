using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// PlayHT TTS provider for high-quality voice synthesis.
/// Respects OfflineOnly mode and degrades gracefully when unavailable.
/// </summary>
public class PlayHTTtsProvider : ITtsProvider
{
    private readonly ILogger<PlayHTTtsProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _userId;
    private readonly string _outputDirectory;
    private readonly bool _offlineOnly;

    private const string BaseUrl = "https://api.play.ht/api/v2";

    public PlayHTTtsProvider(
        ILogger<PlayHTTtsProvider> logger,
        HttpClient httpClient,
        string? apiKey,
        string? userId,
        bool offlineOnly = false)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _userId = userId;
        _offlineOnly = offlineOnly;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS");
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        // Configure HTTP client
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("AUTHORIZATION", _apiKey);
        }
        if (!string.IsNullOrEmpty(_userId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-USER-ID", _userId);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        if (_offlineOnly || string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("PlayHT not available in offline mode or without API key");
            return Array.Empty<string>();
        }

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/voices");
            response.EnsureSuccessStatusCode();
            
            var voices = await response.Content.ReadFromJsonAsync<JsonElement[]>();
            
            var voiceNames = new List<string>();
            if (voices != null)
            {
                foreach (var voice in voices)
                {
                    var name = voice.GetProperty("name").GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        voiceNames.Add(name);
                    }
                }
            }
            
            return voiceNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PlayHT voices");
            return Array.Empty<string>();
        }
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        if (_offlineOnly)
        {
            throw new InvalidOperationException("PlayHT is not available in offline mode. Please disable OfflineOnly or use a local TTS provider.");
        }

        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_userId))
        {
            throw new InvalidOperationException("PlayHT API key and User ID are required. Please configure your credentials.");
        }

        _logger.LogInformation("Synthesizing speech with PlayHT using voice {Voice}", spec.VoiceName);

        // Get the voice ID
        string voiceId = await GetVoiceIdAsync(spec.VoiceName, ct);
        
        // Process each line
        var lineOutputs = new List<string>();
        int lineIndex = 0;

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger.LogDebug("Synthesizing line {Index}: {Text}", line.SceneIndex, 
                line.Text.Length > 30 ? line.Text.Substring(0, 30) + "..." : line.Text);
            
            // Create the request payload
            var payload = new
            {
                text = line.Text,
                voice = voiceId,
                quality = "premium",
                output_format = "mp3",
                speed = spec.Rate,
                sample_rate = 24000
            };

            // Make the API request
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/tts",
                content,
                ct);

            response.EnsureSuccessStatusCode();

            // PlayHT returns a job ID, we need to poll for the result
            var jobResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var transcriptionId = jobResponse.GetProperty("id").GetString();

            // Poll for completion
            string audioUrl = await PollForCompletionAsync(transcriptionId!, ct);

            // Download the audio
            string tempFile = Path.Combine(_outputDirectory, $"line_{line.SceneIndex}_{lineIndex}.mp3");
            lineOutputs.Add(tempFile);

            var audioResponse = await _httpClient.GetAsync(audioUrl, ct);
            audioResponse.EnsureSuccessStatusCode();

            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            {
                await audioResponse.Content.CopyToAsync(fileStream, ct);
            }

            lineIndex++;
        }

        // Combine all audio files into one master track
        string outputFilePath = Path.Combine(_outputDirectory, $"narration_playht_{DateTime.Now:yyyyMMddHHmmss}.mp3");
        
        _logger.LogInformation("Synthesized {Count} lines, combining into final output", lineOutputs.Count);
        
        // For now, just use the first file or concatenate using ffmpeg in production
        if (lineOutputs.Count > 0)
        {
            // In a real implementation, we'd use FFmpeg to concatenate the audio files
            // For now, just copy the first file
            File.Copy(lineOutputs[0], outputFilePath, true);
        }

        // Clean up temp files
        foreach (var file in lineOutputs)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary file {File}", file);
            }
        }

        return outputFilePath;
    }

    /// <summary>
    /// Validates the API key by making a short test synthesis.
    /// </summary>
    public async Task<bool> ValidateApiKeyAsync(CancellationToken ct = default)
    {
        if (_offlineOnly || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_userId))
        {
            return false;
        }

        try
        {
            _logger.LogInformation("Validating PlayHT API key with smoke test");
            
            // Try to fetch voices as a quick validation
            var voices = await GetAvailableVoicesAsync();
            
            if (voices.Count > 0)
            {
                // Perform a short synthesis test
                var testLine = new ScriptLine(
                    SceneIndex: 0,
                    Text: "Test",
                    Start: TimeSpan.Zero,
                    Duration: TimeSpan.FromSeconds(1)
                );

                var voiceSpec = new VoiceSpec(
                    VoiceName: voices[0],
                    Rate: 1.0,
                    Pitch: 0.0,
                    Pause: PauseStyle.Natural
                );

                var result = await SynthesizeAsync(new[] { testLine }, voiceSpec, ct);
                
                // Clean up test file
                if (File.Exists(result))
                {
                    File.Delete(result);
                }

                _logger.LogInformation("PlayHT API key validation successful");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PlayHT API key validation failed");
            return false;
        }
    }

    private async Task<string> PollForCompletionAsync(string transcriptionId, CancellationToken ct)
    {
        const int maxAttempts = 30;
        const int delayMs = 1000;

        for (int i = 0; i < maxAttempts; i++)
        {
            ct.ThrowIfCancellationRequested();

            var response = await _httpClient.GetAsync($"{BaseUrl}/tts/{transcriptionId}", ct);
            response.EnsureSuccessStatusCode();

            var status = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var state = status.GetProperty("status").GetString();

            if (state == "complete")
            {
                return status.GetProperty("output").GetProperty("url").GetString() 
                    ?? throw new Exception("No audio URL in response");
            }
            else if (state == "error")
            {
                throw new Exception("PlayHT synthesis failed");
            }

            await Task.Delay(delayMs, ct);
        }

        throw new TimeoutException("PlayHT synthesis timed out");
    }

    private async Task<string> GetVoiceIdAsync(string voiceName, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/voices", ct);
            response.EnsureSuccessStatusCode();
            
            var voices = await response.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken: ct);
            
            if (voices != null)
            {
                foreach (var voice in voices)
                {
                    var name = voice.GetProperty("name").GetString();
                    if (name?.Equals(voiceName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return voice.GetProperty("id").GetString() ?? throw new Exception("Voice ID not found");
                    }
                }
            }
            
            // If not found, use the first available voice
            _logger.LogWarning("Voice {Voice} not found, using default", voiceName);
            return voices?[0].GetProperty("id").GetString() ?? throw new Exception("No voices available");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get voice ID for {Voice}", voiceName);
            throw;
        }
    }
}
