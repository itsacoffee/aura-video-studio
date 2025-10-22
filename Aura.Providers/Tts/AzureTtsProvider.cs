using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Azure TTS provider with full SSML support and granular voice controls
/// </summary>
public class AzureTtsProvider : ITtsProvider, IDisposable
{
    private readonly ILogger<AzureTtsProvider> _logger;
    private readonly string? _apiKey;
    private readonly string _region;
    private readonly string _outputDirectory;
    private readonly bool _offlineOnly;
    private SpeechConfig? _speechConfig;
    private bool _disposed;

    public AzureTtsProvider(
        ILogger<AzureTtsProvider> logger,
        string? apiKey,
        string region = "eastus",
        bool offlineOnly = false)
    {
        _logger = logger;
        _apiKey = apiKey;
        _region = region;
        _offlineOnly = offlineOnly;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS");

        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }

        // Initialize Speech SDK if not in offline mode
        if (!_offlineOnly && !string.IsNullOrEmpty(_apiKey))
        {
            InitializeSpeechConfig();
        }
    }

    private void InitializeSpeechConfig()
    {
        try
        {
            _speechConfig = SpeechConfig.FromSubscription(_apiKey!, _region);
            _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
            _logger.LogInformation("Azure Speech SDK initialized successfully for region {Region}", _region);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Speech SDK");
            throw new InvalidOperationException("Failed to initialize Azure Speech SDK. Please check your API key and region.", ex);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        if (_offlineOnly || string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Azure TTS not available in offline mode or without API key");
            return Array.Empty<string>();
        }

        try
        {
            using var synthesizer = new SpeechSynthesizer(_speechConfig!);
            using var result = await synthesizer.GetVoicesAsync();

            if (result.Reason == ResultReason.VoicesListRetrieved)
            {
                return result.Voices.Select(v => v.ShortName).ToList();
            }

            _logger.LogWarning("Failed to retrieve voices: {Reason}", result.Reason);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Azure voices");
            return Array.Empty<string>();
        }
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        if (_offlineOnly)
        {
            throw new InvalidOperationException("Azure TTS is not available in offline mode. Please disable OfflineOnly or use a local TTS provider.");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Azure Speech API key is required. Please configure your API key.");
        }

        try
        {
            // Combine all script lines into one SSML document
            var ssml = GenerateSsml(lines, spec, null);
            
            _logger.LogDebug("Generated SSML: {Ssml}", ssml);

            // Create output file path
            var outputPath = Path.Combine(_outputDirectory, $"azure_tts_{Guid.NewGuid()}.wav");

            // Use file output for synthesis
            using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(_speechConfig!, audioConfig);

            // Synthesize
            using var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Azure TTS synthesis completed successfully. Output: {OutputPath}", outputPath);
                return outputPath;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                var errorMessage = $"Azure TTS synthesis canceled: {cancellation.Reason}";
                
                if (cancellation.Reason == CancellationReason.Error)
                {
                    errorMessage += $" - ErrorCode: {cancellation.ErrorCode}, ErrorDetails: {cancellation.ErrorDetails}";
                    
                    // Provide specific error messages
                    if (cancellation.ErrorCode == CancellationErrorCode.ConnectionFailure)
                    {
                        throw new InvalidOperationException("Failed to connect to Azure Speech service. Please check your internet connection.");
                    }
                    else if (cancellation.ErrorCode == CancellationErrorCode.Forbidden)
                    {
                        throw new InvalidOperationException("Invalid Azure Speech API key or insufficient permissions.");
                    }
                    else if (cancellation.ErrorCode == CancellationErrorCode.TooManyRequests)
                    {
                        throw new InvalidOperationException("Azure Speech service quota exceeded. Please try again later.");
                    }
                }

                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            throw new InvalidOperationException($"Unexpected synthesis result: {result.Reason}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Azure TTS synthesis failed");
            throw new InvalidOperationException("Azure TTS synthesis failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Synthesize audio with Azure-specific options
    /// </summary>
    public async Task<string> SynthesizeWithOptionsAsync(
        string text,
        string voiceId,
        AzureTtsOptions? options,
        CancellationToken ct)
    {
        if (_offlineOnly)
        {
            throw new InvalidOperationException("Azure TTS is not available in offline mode.");
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Azure Speech API key is required.");
        }

        try
        {
            var ssml = GenerateSsmlWithOptions(text, voiceId, options);
            _logger.LogDebug("Generated SSML with options: {Ssml}", ssml);

            var outputPath = Path.Combine(_outputDirectory, $"azure_tts_{Guid.NewGuid()}.wav");

            using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
            using var synthesizer = new SpeechSynthesizer(_speechConfig!, audioConfig);
            using var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation("Azure TTS synthesis with options completed successfully");
                return outputPath;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                throw new InvalidOperationException($"Synthesis canceled: {cancellation.ErrorDetails}");
            }

            throw new InvalidOperationException($"Unexpected synthesis result: {result.Reason}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Azure TTS synthesis with options failed");
            throw new InvalidOperationException("Azure TTS synthesis failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Generate SSML from script lines (backward compatible)
    /// </summary>
    private string GenerateSsml(IEnumerable<ScriptLine> lines, VoiceSpec spec, AzureTtsOptions? options)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
        var speak = new XElement("speak",
            new XAttribute("version", "1.0"),
            new XAttribute(XNamespace.Xml + "lang", "en-US"),
            new XAttribute(XNamespace.Xmlns + "mstts", "https://www.w3.org/2001/mstts"));

        var voice = new XElement("voice",
            new XAttribute("name", spec.VoiceName));

        // Apply prosody from VoiceSpec
        var prosody = new XElement("prosody");
        
        // Convert VoiceSpec rate (1.0 = 100%) to percentage
        var ratePercent = (int)((spec.Rate - 1.0) * 100);
        if (ratePercent != 0)
        {
            prosody.Add(new XAttribute("rate", $"{(ratePercent >= 0 ? "+" : "")}{ratePercent}%"));
        }

        // Convert VoiceSpec pitch (0.0 = default) to percentage
        var pitchPercent = (int)(spec.Pitch * 100);
        if (pitchPercent != 0)
        {
            prosody.Add(new XAttribute("pitch", $"{(pitchPercent >= 0 ? "+" : "")}{pitchPercent}%"));
        }

        // Combine all script lines
        var combinedText = string.Join(" ", lines.Select(l => l.Text));
        prosody.Add(combinedText);

        voice.Add(prosody);
        speak.Add(voice);
        doc.Add(speak);

        return doc.ToString();
    }

    /// <summary>
    /// Generate SSML with full Azure-specific options
    /// </summary>
    private string GenerateSsmlWithOptions(string text, string voiceId, AzureTtsOptions? options)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null));
        var speak = new XElement("speak",
            new XAttribute("version", "1.0"),
            new XAttribute(XNamespace.Xml + "lang", "en-US"),
            new XAttribute(XNamespace.Xmlns + "mstts", "https://www.w3.org/2001/mstts"));

        var voice = new XElement("voice", new XAttribute("name", voiceId));

        XElement? currentElement = voice;

        // Apply style and role if specified
        if (options != null && (!string.IsNullOrEmpty(options.Style) || !string.IsNullOrEmpty(options.Role)))
        {
            var expressAs = new XElement(XNamespace.Get("https://www.w3.org/2001/mstts") + "express-as");
            
            if (!string.IsNullOrEmpty(options.Style))
            {
                expressAs.Add(new XAttribute("style", options.Style));
                
                // Add style degree if not default
                if (Math.Abs(options.StyleDegree - 1.0) > 0.01)
                {
                    expressAs.Add(new XAttribute("styledegree", options.StyleDegree.ToString("F2")));
                }
            }

            if (!string.IsNullOrEmpty(options.Role))
            {
                expressAs.Add(new XAttribute("role", options.Role));
            }

            voice.Add(expressAs);
            currentElement = expressAs;
        }

        // Apply audio effects if specified
        if (options?.AudioEffect != AzureAudioEffect.None)
        {
            var effectName = options.AudioEffect switch
            {
                AzureAudioEffect.EqTelecom => "eq_telecom",
                AzureAudioEffect.EqCar => "eq_car",
                AzureAudioEffect.Reverb => "reverb",
                _ => null
            };

            if (effectName != null)
            {
                speak.Add(new XAttribute(XNamespace.Get("https://www.w3.org/2001/mstts") + "audio-effect", effectName));
            }
        }

        // Apply prosody (rate, pitch, volume, contour)
        var prosody = new XElement("prosody");
        bool hasProsody = false;

        if (options != null)
        {
            // Rate: convert from -1.0 to 2.0 range to percentage
            if (Math.Abs(options.Rate) > 0.01)
            {
                var ratePercent = (int)(options.Rate * 100);
                prosody.Add(new XAttribute("rate", $"{(ratePercent >= 0 ? "+" : "")}{ratePercent}%"));
                hasProsody = true;
            }

            // Pitch: convert from -0.5 to 0.5 range to percentage
            if (Math.Abs(options.Pitch) > 0.01)
            {
                var pitchPercent = (int)(options.Pitch * 100);
                prosody.Add(new XAttribute("pitch", $"{(pitchPercent >= 0 ? "+" : "")}{pitchPercent}%"));
                hasProsody = true;
            }

            // Volume: convert from 0.0 to 2.0 to percentage
            if (Math.Abs(options.Volume - 1.0) > 0.01)
            {
                var volumePercent = (int)(options.Volume * 100);
                prosody.Add(new XAttribute("volume", $"{volumePercent}%"));
                hasProsody = true;
            }

            // Contour
            if (!string.IsNullOrEmpty(options.ProsodyContour))
            {
                prosody.Add(new XAttribute("contour", options.ProsodyContour));
                hasProsody = true;
            }
        }

        // Add text content with potential emphasis and breaks
        var textContent = ApplyTextMarkup(text, options);
        
        if (hasProsody)
        {
            prosody.Add(textContent);
            currentElement.Add(prosody);
        }
        else
        {
            currentElement.Add(textContent);
        }

        speak.Add(voice);
        doc.Add(speak);

        return doc.ToString();
    }

    /// <summary>
    /// Apply text-level markup (emphasis, breaks, phonemes, say-as)
    /// </summary>
    private object ApplyTextMarkup(string text, AzureTtsOptions? options)
    {
        if (options == null)
        {
            return text;
        }

        // For now, return simple text. In a full implementation, we would:
        // 1. Parse the text and insert <break> elements at CustomBreaks positions
        // 2. Wrap text in <emphasis> tags based on Emphasis setting
        // 3. Use <phoneme> tags for custom pronunciations
        // 4. Use <say-as> tags for special interpretations
        
        // Simple emphasis wrapper if specified
        if (options.Emphasis != EmphasisLevel.None)
        {
            var level = options.Emphasis.ToString().ToLowerInvariant();
            return new XElement("emphasis", new XAttribute("level", level), text);
        }

        return text;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // SpeechConfig doesn't implement IDisposable
            _speechConfig = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
