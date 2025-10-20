using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Providers.Audio;
using Microsoft.Extensions.Logging;

#if WINDOWS10_0_19041_0_OR_GREATER
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
#endif

namespace Aura.Providers.Tts;

public class WindowsTtsProvider : ITtsProvider
{
    private readonly ILogger<WindowsTtsProvider> _logger;
#if WINDOWS10_0_19041_0_OR_GREATER
    private readonly SpeechSynthesizer _synthesizer;
#endif
    private readonly string _outputDirectory;

    public WindowsTtsProvider(ILogger<WindowsTtsProvider> logger)
    {
        _logger = logger;
#if WINDOWS10_0_19041_0_OR_GREATER
        _synthesizer = new SpeechSynthesizer();
#endif
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS");
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        await Task.CompletedTask; // Ensure async behavior
        var voiceNames = new List<string>();
        
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            voiceNames.Add(voice.DisplayName);
        }
        
        return voiceNames;
#else
        await Task.CompletedTask;
        return new List<string> { "Microsoft David Desktop", "Microsoft Zira Desktop" };
#endif
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
#if WINDOWS10_0_19041_0_OR_GREATER
        _logger.LogInformation("Synthesizing speech with Windows TTS using voice {Voice}", spec.VoiceName);
        
        // Find the requested voice
        VoiceInformation? selectedVoice = null;
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            if (voice.DisplayName == spec.VoiceName)
            {
                selectedVoice = voice;
                break;
            }
        }
        
        if (selectedVoice == null)
        {
            _logger.LogWarning("Voice {Voice} not found, using default voice", spec.VoiceName);
            selectedVoice = SpeechSynthesizer.DefaultVoice;
        }
        
        // Set the voice
        _synthesizer.Voice = selectedVoice;
        
        // Prepare the output file
        string outputFilePath = Path.Combine(_outputDirectory, $"narration_{DateTime.Now:yyyyMMddHHmmss}.wav");
        
        // Process each line
        var lineOutputs = new List<string>();
        
        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            // Create SSML with prosody adjustments
            string ssml = CreateSsml(line.Text, spec);
            
            // Synthesize the speech
            using var stream = await _synthesizer.SynthesizeSsmlToStreamAsync(ssml);
            
            // Save to temp file
            string tempFile = Path.Combine(_outputDirectory, $"line_{line.SceneIndex}.wav");
            lineOutputs.Add(tempFile);
            
            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            {
                await stream.AsStreamForRead().CopyToAsync(fileStream, 81920, ct);
            }
            
            _logger.LogDebug("Synthesized line {Index}: {Text}", line.SceneIndex, 
                line.Text.Length > 30 ? line.Text.Substring(0, 30) + "..." : line.Text);
        }
        
        // Combine all audio files into one using WAV merger
        _logger.LogInformation("Synthesized {Count} lines, combining into final output", lineOutputs.Count);
        
        if (lineOutputs.Count > 0)
        {
            var linesList = lines.ToList();
            var segments = linesList.Select((line, index) => 
                new WavSegment(
                    FilePath: lineOutputs[index],
                    StartTime: line.Start,
                    Duration: line.Duration
                )).ToList();

            // Merge WAV files with proper timing
            WavMerger.MergeWavFiles(segments, outputFilePath);
            
            // Validate the merged file (only on Windows where TTS is available)
            if (File.Exists(outputFilePath))
            {
                var outputInfo = new FileInfo(outputFilePath);
                if (outputInfo.Length < 128)
                {
                    _logger.LogError("Merged narration file is too small: {Size} bytes", outputInfo.Length);
                    throw new InvalidOperationException($"Failed to create valid narration file (only {outputInfo.Length} bytes)");
                }
                _logger.LogInformation("Narration file created successfully: {Path} ({Size} bytes)", outputFilePath, outputInfo.Length);
            }
            else
            {
                _logger.LogError("Merged narration file was not created: {Path}", outputFilePath);
                throw new InvalidOperationException("Failed to create narration file");
            }
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
#else
        await Task.CompletedTask;
        _logger.LogError("Windows TTS is not available on this platform. Cannot synthesize audio.");
        throw new PlatformNotSupportedException(
            "Windows TTS is only available on Windows 10 (build 19041) or later. " +
            "Please use a different TTS provider (Piper, Mimic3, ElevenLabs, or PlayHT) on this platform.");
#endif
    }
    
#if WINDOWS10_0_19041_0_OR_GREATER
    private string CreateSsml(string text, VoiceSpec spec)
    {
        // Format text with SSML tags including prosody adjustments
        string rateAttribute = $"rate=\"{spec.Rate}\"";
        string pitchAttribute = $"pitch=\"{(spec.Pitch >= 0 ? "+" : "")}{spec.Pitch}st\"";
        
        // Add different pause styles
        string pauseStyle = spec.Pause switch
        {
            PauseStyle.Short => "<break strength=\"weak\"/>",
            PauseStyle.Long => "<break strength=\"strong\"/>",
            PauseStyle.Dramatic => "<break time=\"1s\"/>",
            _ => "<break strength=\"medium\"/>" // Default/Natural
        };
        
        // Replace periods with pause markers (simplified approach)
        text = text.Replace(". ", $". {pauseStyle} ");
        
        // Create final SSML
        return $@"
            <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""en-US"">
                <voice name=""{spec.VoiceName}"">
                    <prosody {rateAttribute} {pitchAttribute}>
                        {text}
                    </prosody>
                </voice>
            </speak>";
    }
#endif
}