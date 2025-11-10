using System;
using System.Collections.Generic;
using System.Diagnostics;
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
/// ElevenLabs TTS provider for high-quality voice synthesis.
/// Respects OfflineOnly mode and degrades gracefully when unavailable.
/// </summary>
public class ElevenLabsTtsProvider : ITtsProvider
{
    private readonly ILogger<ElevenLabsTtsProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _outputDirectory;
    private readonly bool _offlineOnly;
    private readonly VoiceCache? _voiceCache;
    private readonly string? _ffmpegPath;

    private const string BaseUrl = "https://api.elevenlabs.io/v1";

    public ElevenLabsTtsProvider(
        ILogger<ElevenLabsTtsProvider> logger, 
        HttpClient httpClient,
        string? apiKey,
        bool offlineOnly = false,
        VoiceCache? voiceCache = null,
        string? ffmpegPath = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _offlineOnly = offlineOnly;
        _voiceCache = voiceCache;
        _ffmpegPath = ffmpegPath;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS");
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        // Validate API key on initialization (only when not in offline mode)
        if (!_offlineOnly)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("ElevenLabs API key is missing. Provider will not be functional. Please configure your API key in settings.");
            }
            else
            {
                // Configure HTTP client with API key
                _httpClient.DefaultRequestHeaders.Add("xi-api-key", _apiKey);
                _logger.LogInformation("ElevenLabs TTS provider initialized with API key");
            }
        }
        else
        {
            _logger.LogInformation("ElevenLabs TTS provider initialized in offline-only mode (not functional)");
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        if (_offlineOnly || string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("ElevenLabs not available in offline mode or without API key");
            return Array.Empty<string>();
        }

        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/voices");
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var voices = json.GetProperty("voices");
            
            var voiceNames = new List<string>();
            foreach (var voice in voices.EnumerateArray())
            {
                var name = voice.GetProperty("name").GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    voiceNames.Add(name);
                }
            }
            
            return voiceNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ElevenLabs voices");
            return Array.Empty<string>();
        }
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        if (_offlineOnly)
        {
            throw new InvalidOperationException("ElevenLabs is not available in offline mode. Please disable offline-only mode in settings or use a local TTS provider (Piper, Mimic3, or Windows TTS).");
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("ElevenLabs API key is required. Please configure your API key in the application settings.");
        }

        _logger.LogInformation("Synthesizing speech with ElevenLabs using voice {Voice}", spec.VoiceName);

        // Get the voice ID with validation
        string voiceId = await GetVoiceIdAsync(spec.VoiceName, ct);
        
        // Process each line (with caching support)
        var lineOutputs = new List<string>();
        int lineIndex = 0;

        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger.LogDebug("Synthesizing line {Index}: {Text}", line.SceneIndex, 
                line.Text.Length > 30 ? line.Text.Substring(0, 30) + "..." : line.Text);
            
            // Check cache first
            string? cachedAudio = null;
            if (_voiceCache != null)
            {
                cachedAudio = _voiceCache.TryGetCached(
                    "ElevenLabs",
                    spec.VoiceName,
                    line.Text,
                    spec.Rate,
                    spec.Pitch);
            }

            string tempFile;
            if (cachedAudio != null)
            {
                _logger.LogDebug("Using cached audio for line {Index}", line.SceneIndex);
                tempFile = cachedAudio;
            }
            else
            {
                // Create the request payload
                var payload = new
                {
                    text = line.Text,
                    model_id = "eleven_monolingual_v1", // Default model
                    voice_settings = new
                    {
                        stability = 0.5,
                        similarity_boost = 0.75,
                        style = 0.0,
                        use_speaker_boost = true
                    }
                };

                // Make the API request
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{BaseUrl}/text-to-speech/{voiceId}",
                    content,
                    ct);

                // Handle specific error cases
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new InvalidOperationException("ElevenLabs API key is invalid. Please check your API key in settings.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        throw new InvalidOperationException("ElevenLabs rate limit exceeded. Please wait a moment and try again, or upgrade your ElevenLabs plan.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
                    {
                        throw new InvalidOperationException("ElevenLabs quota exceeded. Please check your plan limits or upgrade your subscription.");
                    }
                    else
                    {
                        _logger.LogError("ElevenLabs API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                        throw new InvalidOperationException($"ElevenLabs synthesis failed with status {response.StatusCode}. {errorContent}");
                    }
                }

                response.EnsureSuccessStatusCode();

                // Save to temp file
                tempFile = Path.Combine(_outputDirectory, $"line_{line.SceneIndex}_{lineIndex}.mp3");

                using (var fileStream = new FileStream(tempFile, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream, ct);
                }

                // Store in cache
                if (_voiceCache != null)
                {
                    try
                    {
                        await _voiceCache.StoreAsync(
                            "ElevenLabs",
                            spec.VoiceName,
                            line.Text,
                            tempFile,
                            spec.Rate,
                            spec.Pitch,
                            ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache audio for line {Index}", line.SceneIndex);
                    }
                }
            }

            lineOutputs.Add(tempFile);
            lineIndex++;
        }

        // Combine all audio files into one master track
        string outputFilePath = Path.Combine(_outputDirectory, $"narration_elevenlabs_{DateTime.Now:yyyyMMddHHmmss}.mp3");
        
        _logger.LogInformation("Synthesized {Count} lines, combining into final output", lineOutputs.Count);
        
        if (lineOutputs.Count == 0)
        {
            throw new InvalidOperationException("No audio lines were generated");
        }
        
        if (lineOutputs.Count == 1)
        {
            // Single file, just copy it
            File.Copy(lineOutputs[0], outputFilePath, true);
        }
        else
        {
            // Multiple files, concatenate with FFmpeg
            await ConcatenateAudioFilesAsync(lineOutputs, outputFilePath, ct);
        }

        // Clean up temp files (only non-cached ones)
        foreach (var file in lineOutputs)
        {
            try
            {
                // Don't delete cached files
                if (_voiceCache != null)
                {
                    var isCached = _voiceCache.TryGetCached("ElevenLabs", spec.VoiceName, string.Empty) != null;
                    if (isCached && file.Contains("Cache"))
                    {
                        continue;
                    }
                }

                if (File.Exists(file) && !file.Contains("Cache"))
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
        if (_offlineOnly || string.IsNullOrEmpty(_apiKey))
        {
            return false;
        }

        try
        {
            _logger.LogInformation("Validating ElevenLabs API key with smoke test");
            
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

                _logger.LogInformation("ElevenLabs API key validation successful");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ElevenLabs API key validation failed");
            return false;
        }
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
                _logger.LogError("Failed to fetch voices from ElevenLabs: {StatusCode}", response.StatusCode);
                throw new InvalidOperationException($"Failed to fetch voices from ElevenLabs. Status: {response.StatusCode}. Please check your API key and internet connection.");
            }
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var voices = json.GetProperty("voices");
            
            // First try exact match
            foreach (var voice in voices.EnumerateArray())
            {
                var name = voice.GetProperty("name").GetString();
                if (name?.Equals(voiceName, StringComparison.Ordinal) == true)
                {
                    var voiceId = voice.GetProperty("voice_id").GetString();
                    if (string.IsNullOrEmpty(voiceId))
                    {
                        throw new InvalidOperationException($"Voice ID is empty for voice '{voiceName}'");
                    }
                    _logger.LogDebug("Found voice '{Voice}' with ID: {VoiceId}", voiceName, voiceId);
                    return voiceId;
                }
            }
            
            // Try case-insensitive match
            foreach (var voice in voices.EnumerateArray())
            {
                var name = voice.GetProperty("name").GetString();
                if (name?.Equals(voiceName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var voiceId = voice.GetProperty("voice_id").GetString();
                    if (string.IsNullOrEmpty(voiceId))
                    {
                        throw new InvalidOperationException($"Voice ID is empty for voice '{voiceName}'");
                    }
                    _logger.LogWarning("Voice '{Voice}' found with case-insensitive match", voiceName);
                    return voiceId;
                }
            }
            
            // Voice not found - provide helpful error
            var availableVoices = voices.EnumerateArray()
                .Select(v => v.GetProperty("name").GetString())
                .Where(n => !string.IsNullOrEmpty(n))
                .Take(5)
                .ToList();
            
            var voiceList = availableVoices.Count > 0 
                ? $"Available voices include: {string.Join(", ", availableVoices)}" 
                : "No voices available";
            
            throw new InvalidOperationException($"Voice '{voiceName}' not found in your ElevenLabs account. {voiceList}. Please check the voice name in settings.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            _logger.LogError(ex, "Error fetching voice ID for '{Voice}'", voiceName);
            throw new InvalidOperationException($"Failed to get voice ID for '{voiceName}'. Check your internet connection and API key.", ex);
        }
    }

    /// <summary>
    /// Streams audio generation in real-time (ElevenLabs streaming API)
    /// </summary>
    public async IAsyncEnumerable<byte[]> StreamAudioAsync(
        string text,
        string voiceId,
        CancellationToken ct)
    {
        if (_offlineOnly || string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("ElevenLabs is not available in offline mode");
        }

        _logger.LogInformation("Streaming audio generation for voice {VoiceId}", voiceId);

        var payload = new
        {
            text,
            model_id = "eleven_monolingual_v1",
            voice_settings = new
            {
                stability = 0.5,
                similarity_boost = 0.75,
                style = 0.0,
                use_speaker_boost = true
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/text-to-speech/{voiceId}/stream")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            yield return chunk;
        }

        _logger.LogInformation("Audio streaming completed");
    }

    /// <summary>
    /// Concatenates multiple audio files using FFmpeg
    /// </summary>
    private async Task ConcatenateAudioFilesAsync(
        List<string> inputFiles,
        string outputPath,
        CancellationToken ct)
    {
        var ffmpegPath = await GetFfmpegPathAsync();

        // Create concat file list
        var concatListPath = Path.Combine(_outputDirectory, $"concat_{Guid.NewGuid():N}.txt");
        
        try
        {
            // Write file list for ffmpeg concat demuxer
            var fileList = inputFiles.Select(f => $"file '{f.Replace("'", "'\\''")}'");
            await File.WriteAllLinesAsync(concatListPath, fileList, ct);

            // Concatenate using ffmpeg
            var args = $"-f concat -safe 0 -i \"{concatListPath}\" -c copy -y \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start FFmpeg process");
            }

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("FFmpeg concatenation failed: {Error}", error);
                throw new InvalidOperationException($"Failed to concatenate audio files: {error}");
            }

            _logger.LogDebug("Successfully concatenated {Count} audio files", inputFiles.Count);
        }
        finally
        {
            // Clean up concat list file
            if (File.Exists(concatListPath))
            {
                try
                {
                    File.Delete(concatListPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete concat list file");
                }
            }
        }
    }

    /// <summary>
    /// Gets FFmpeg executable path
    /// </summary>
    private async Task<string> GetFfmpegPathAsync()
    {
        if (!string.IsNullOrEmpty(_ffmpegPath) && File.Exists(_ffmpegPath))
        {
            return _ffmpegPath;
        }

        // Try to find ffmpeg in PATH
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        
        foreach (var path in paths)
        {
            var ffmpegPath = Path.Combine(path, "ffmpeg");
            if (OperatingSystem.IsWindows())
            {
                ffmpegPath += ".exe";
            }

            if (File.Exists(ffmpegPath))
            {
                return ffmpegPath;
            }
        }

        throw new FileNotFoundException(
            "FFmpeg not found. Please install FFmpeg to concatenate audio files. " +
            "Download from: https://ffmpeg.org/download.html");
    }
}
