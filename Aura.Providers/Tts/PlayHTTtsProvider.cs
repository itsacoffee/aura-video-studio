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

        // Validate credentials on initialization (only when not in offline mode)
        if (!_offlineOnly)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_userId))
            {
                _logger.LogWarning("PlayHT credentials are incomplete. Both API key and User ID are required. Provider will not be functional. Please configure your credentials in settings.");
            }
            else
            {
                // Configure HTTP client with credentials
                _httpClient.DefaultRequestHeaders.Add("AUTHORIZATION", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("X-USER-ID", _userId);
                _logger.LogInformation("PlayHT TTS provider initialized with API key and User ID");
            }
        }
        else
        {
            _logger.LogInformation("PlayHT TTS provider initialized in offline-only mode (not functional)");
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        if (_offlineOnly)
        {
            _logger.LogWarning("PlayHT not available in offline mode");
            return Array.Empty<string>();
        }

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_userId))
        {
            _logger.LogWarning("PlayHT not available without API key and User ID");
            return Array.Empty<string>();
        }

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/voices");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch PlayHT voices: {StatusCode}", response.StatusCode);
                return Array.Empty<string>();
            }
            
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
            throw new InvalidOperationException("PlayHT is not available in offline mode. Please disable offline-only mode in settings or use a local TTS provider (Piper, Mimic3, or Windows TTS).");
        }

        if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_userId))
        {
            throw new InvalidOperationException("PlayHT API key and User ID are both required. Please configure your credentials in the application settings.");
        }

        _logger.LogInformation("Synthesizing speech with PlayHT using voice {Voice}", spec.VoiceName);

        // Check if voice exists before attempting synthesis
        var availableVoices = await GetAvailableVoicesAsync();
        if (availableVoices.Count == 0)
        {
            throw new InvalidOperationException("Unable to fetch available voices from PlayHT. Please check your credentials and internet connection.");
        }

        // Get the voice ID with validation
        string voiceId = await GetVoiceIdAsync(spec.VoiceName, ct);
        
        // Process each line
        var lineOutputs = new List<string>();
        int lineIndex = 0;

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger.LogDebug("Synthesizing line {Index}: {Text}", line.SceneIndex, 
                line.Text.Length > 30 ? string.Concat(line.Text.AsSpan(0, 30), "...") : line.Text);
            
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
        if (string.IsNullOrWhiteSpace(voiceName))
        {
            throw new ArgumentException("Voice name cannot be empty", nameof(voiceName));
        }

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/voices", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch voices from PlayHT: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Failed to fetch voices from PlayHT. Status: {response.StatusCode}. Please check your credentials and internet connection.");
            }
            
            response.EnsureSuccessStatusCode();
            
            var voices = await response.Content.ReadFromJsonAsync<JsonElement[]>(cancellationToken: ct);
            
            if (voices != null && voices.Length > 0)
            {
                // First try exact match
                foreach (var voice in voices)
                {
                    var name = voice.GetProperty("name").GetString();
                    if (name?.Equals(voiceName, StringComparison.Ordinal) == true)
                    {
                        var voiceId = voice.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(voiceId))
                        {
                            throw new InvalidOperationException($"Voice ID is empty for voice '{voiceName}'");
                        }
                        _logger.LogDebug("Found voice '{Voice}' with ID: {VoiceId}", voiceName, voiceId);
                        return voiceId;
                    }
                }
                
                // Try case-insensitive match
                foreach (var voice in voices)
                {
                    var name = voice.GetProperty("name").GetString();
                    if (name?.Equals(voiceName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var voiceId = voice.GetProperty("id").GetString();
                        if (string.IsNullOrEmpty(voiceId))
                        {
                            throw new InvalidOperationException($"Voice ID is empty for voice '{voiceName}'");
                        }
                        _logger.LogWarning("Voice '{Voice}' found with case-insensitive match", voiceName);
                        return voiceId;
                    }
                }
                
                // Voice not found - provide helpful error
                var availableVoices = voices
                    .Select(v => v.GetProperty("name").GetString())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Take(5)
                    .ToList();
                
                var voiceList = availableVoices.Count > 0 
                    ? $"Available voices include: {string.Join(", ", availableVoices)}" 
                    : "No voices available";
                
                throw new InvalidOperationException($"Voice '{voiceName}' not found in your PlayHT account. {voiceList}. Please check the voice name in settings.");
            }
            
            throw new InvalidOperationException("No voices available in PlayHT account. Please check your subscription.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error fetching voice ID for '{Voice}'", voiceName);
            throw new InvalidOperationException($"Failed to get voice ID for '{voiceName}'. Check your internet connection and credentials.", ex);
        }
    }
}
