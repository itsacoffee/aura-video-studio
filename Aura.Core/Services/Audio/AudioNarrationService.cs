using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.OpenAI;
using Aura.Core.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audio;

/// <summary>
/// Service for generating audio narration using OpenAI GPT-4o audio capabilities or TTS providers.
/// Provides caching, fallback to TTS providers, and format conversion.
/// </summary>
public class AudioNarrationService
{
    private readonly ILogger<AudioNarrationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TtsProviderFactory _ttsProviderFactory;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public AudioNarrationService(
        ILogger<AudioNarrationService> logger,
        IMemoryCache cache,
        TtsProviderFactory ttsProviderFactory)
    {
        _logger = logger;
        _cache = cache;
        _ttsProviderFactory = ttsProviderFactory;
    }

    /// <summary>
    /// Generate audio narration for the given text using OpenAI or fallback providers.
    /// </summary>
    /// <param name="text">Text to convert to speech</param>
    /// <param name="voice">Voice to use (OpenAI voice name or TTS provider voice)</param>
    /// <param name="audioGenerator">Optional audio generator function (for OpenAI integration)</param>
    /// <param name="useCache">Whether to use cached audio if available</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Audio file path and metadata</returns>
    public async Task<NarrationResult> GenerateNarrationAsync(
        string text,
        string voice,
        Func<string, AudioConfig, CancellationToken, Task<AudioGenerationResult>>? audioGenerator = null,
        bool useCache = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        _logger.LogInformation("Generating narration for {Length} characters with voice {Voice}",
            text.Length, voice);

        // Check cache first
        var cacheKey = GenerateCacheKey(text, voice);
        if (useCache && _cache.TryGetValue<NarrationResult>(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Returning cached narration for key {CacheKey}", cacheKey);
            
            if (cachedResult == null)
            {
                _logger.LogWarning("Cached result was null, regenerating");
            }
            else
            {
                return cachedResult;
            }
        }

        NarrationResult result;

        // Try audio generator first if provided (e.g., OpenAI audio)
        if (audioGenerator != null)
        {
            try
            {
                _logger.LogInformation("Attempting audio generation via provided generator");
                result = await GenerateWithAudioGeneratorAsync(text, voice, audioGenerator, ct);
                
                // Cache successful result
                if (useCache)
                {
                    _cache.Set(cacheKey, result, _cacheExpiration);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audio generation failed, falling back to TTS provider");
            }
        }

        // Fallback to TTS providers
        _logger.LogInformation("Using TTS provider fallback");
        result = await GenerateWithTtsProviderAsync(text, voice, ct);

        // Cache successful result
        if (useCache)
        {
            _cache.Set(cacheKey, result, _cacheExpiration);
        }

        return result;
    }

    /// <summary>
    /// Generate audio using provided audio generator (e.g., OpenAI GPT-4o audio).
    /// </summary>
    private async Task<NarrationResult> GenerateWithAudioGeneratorAsync(
        string text,
        string voice,
        Func<string, AudioConfig, CancellationToken, Task<AudioGenerationResult>> audioGenerator,
        CancellationToken ct)
    {
        // Map common voice names to OpenAI voices
        var audioVoice = MapToAudioVoice(voice);

        var audioConfig = new AudioConfig
        {
            Voice = audioVoice,
            Format = AudioFormat.Wav,
            Modalities = new List<string> { "text", "audio" }
        };

        var response = await audioGenerator(text, audioConfig, ct);

        // Decode base64 audio data
        var audioBytes = Convert.FromBase64String(response.AudioData);

        // Save to temporary file
        var tempPath = Path.Combine(Path.GetTempPath(), $"narration_{Guid.NewGuid()}.wav");
        await File.WriteAllBytesAsync(tempPath, audioBytes, ct);

        _logger.LogInformation("Audio saved to {Path}, size: {Size} bytes", tempPath, audioBytes.Length);

        return new NarrationResult
        {
            AudioPath = tempPath,
            Transcript = response.Transcript,
            Voice = response.Voice,
            Format = response.Format,
            Provider = "OpenAI",
            DurationSeconds = EstimateAudioDuration(audioBytes.Length, response.Format)
        };
    }

    /// <summary>
    /// Generate audio using TTS provider as fallback.
    /// </summary>
    private async Task<NarrationResult> GenerateWithTtsProviderAsync(
        string text,
        string voice,
        CancellationToken ct)
    {
        // Try ElevenLabs first as highest quality fallback
        var providers = new[] { "elevenlabs", "azure", "sapi" };
        Exception? lastException = null;

        foreach (var providerName in providers)
        {
            try
            {
                _logger.LogInformation("Attempting TTS with provider: {Provider}", providerName);
                
                var provider = _ttsProviderFactory.TryCreateProvider(providerName);
                if (provider == null)
                {
                    _logger.LogWarning("Provider {Provider} not available", providerName);
                    continue;
                }

                // Create a single script line for TTS synthesis
                var scriptLine = new ScriptLine(
                    SceneIndex: 0,
                    Text: text,
                    Start: TimeSpan.Zero,
                    Duration: TimeSpan.FromSeconds(10) // Placeholder duration
                );

                var voiceSpec = new VoiceSpec(
                    VoiceName: voice,
                    Rate: 1.0,
                    Pitch: 0.0,
                    Pause: PauseStyle.Natural
                );

                // Generate audio with TTS provider
                var audioPath = await provider.SynthesizeAsync(new[] { scriptLine }, voiceSpec, ct);

                if (File.Exists(audioPath))
                {
                    var fileInfo = new FileInfo(audioPath);
                    _logger.LogInformation("TTS audio generated with {Provider}: {Path}, size: {Size} bytes",
                        providerName, audioPath, fileInfo.Length);

                    return new NarrationResult
                    {
                        AudioPath = audioPath,
                        Transcript = text,
                        Voice = voice,
                        Format = Path.GetExtension(audioPath).TrimStart('.'),
                        Provider = providerName,
                        DurationSeconds = EstimateAudioDuration((int)fileInfo.Length, "wav")
                    };
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "TTS provider {Provider} failed", providerName);
            }
        }

        throw new InvalidOperationException(
            "Failed to generate narration with all available providers", lastException);
    }

    /// <summary>
    /// Map voice name to OpenAI AudioVoice enum.
    /// </summary>
    private static AudioVoice MapToAudioVoice(string voice)
    {
        return voice.ToLowerInvariant() switch
        {
            "alloy" => AudioVoice.Alloy,
            "echo" => AudioVoice.Echo,
            "fable" => AudioVoice.Fable,
            "onyx" => AudioVoice.Onyx,
            "nova" => AudioVoice.Nova,
            "shimmer" => AudioVoice.Shimmer,
            _ => AudioVoice.Alloy // Default fallback
        };
    }

    /// <summary>
    /// Estimate audio duration based on file size and format.
    /// This is a rough estimate for caching purposes.
    /// </summary>
    private static double EstimateAudioDuration(int sizeBytes, string format)
    {
        // Rough estimates based on typical bitrates
        // WAV: ~176KB/s (16-bit stereo at 44.1kHz)
        // MP3: ~16KB/s (128kbps)
        var bytesPerSecond = format.ToLowerInvariant() switch
        {
            "wav" => 176000,
            "mp3" => 16000,
            "opus" => 12000,
            _ => 176000
        };

        return (double)sizeBytes / bytesPerSecond;
    }

    /// <summary>
    /// Generate cache key for narration.
    /// </summary>
    private static string GenerateCacheKey(string text, string voice)
    {
        // Use hash of text + voice to create stable cache key
        var combined = $"{text}:{voice}";
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(combined));
        return $"narration:{Convert.ToHexString(hash)[..16]}";
    }

    /// <summary>
    /// Clear cached narrations (for testing or manual cache management).
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing narration cache");
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
        }
    }
}

/// <summary>
/// Result of audio generation (returned by audio generator function).
/// </summary>
public class AudioGenerationResult
{
    /// <summary>
    /// Base64-encoded audio data
    /// </summary>
    public string AudioData { get; set; } = string.Empty;

    /// <summary>
    /// Transcript of the generated audio
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Audio format (wav, mp3, etc.)
    /// </summary>
    public string Format { get; set; } = "wav";

    /// <summary>
    /// Voice used for generation
    /// </summary>
    public string Voice { get; set; } = "alloy";
}

/// <summary>
/// Result of narration generation.
/// </summary>
public class NarrationResult
{
    /// <summary>
    /// Path to generated audio file.
    /// </summary>
    public string AudioPath { get; set; } = string.Empty;

    /// <summary>
    /// Transcript of the audio.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Voice used for generation.
    /// </summary>
    public string Voice { get; set; } = string.Empty;

    /// <summary>
    /// Audio format (wav, mp3, etc.).
    /// </summary>
    public string Format { get; set; } = "wav";

    /// <summary>
    /// Provider used (OpenAI, elevenlabs, etc.).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Estimated duration in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
}
