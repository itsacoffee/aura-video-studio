using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        await Task.CompletedTask.ConfigureAwait(false); // Ensure async behavior
        var voiceNames = new List<string>();
        
        foreach (var voice in SpeechSynthesizer.AllVoices)
        {
            voiceNames.Add(voice.DisplayName);
        }
        
        return voiceNames;
#else
        await Task.CompletedTask.ConfigureAwait(false);
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
        
        // Process each line with chunking and timeout support
        var lineOutputs = new List<string>();
        const int MaxCharsPerChunk = 450; // Safe limit for Windows TTS
        
        foreach (var line in lines)
        {
            ct.ThrowIfCancellationRequested();
            
            // Chunk long lines (>450 chars) to prevent silent failures
            if (line.Text.Length > MaxCharsPerChunk)
            {
                _logger.LogWarning("Line {Index} exceeds {MaxChars} characters ({Length}), chunking for Windows TTS",
                    line.SceneIndex, MaxCharsPerChunk, line.Text.Length);
                
                var chunks = ChunkTextForWindowsTts(line.Text);
                for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                {
                    var chunk = chunks[chunkIndex];
                    var chunkTempFile = await SynthesizeChunkWithTimeoutAsync(
                        chunk,
                        spec,
                        $"{line.SceneIndex}_{chunkIndex}",
                        TimeSpan.FromSeconds(30),
                        ct).ConfigureAwait(false);
                    
                    if (chunkTempFile != null)
                    {
                        lineOutputs.Add(chunkTempFile);
                    }
                }
            }
            else
            {
                // Normal processing for short lines
                var tempFile = await SynthesizeChunkWithTimeoutAsync(
                    line.Text,
                    spec,
                    line.SceneIndex.ToString(),
                    TimeSpan.FromSeconds(30),
                    ct).ConfigureAwait(false);
                
                if (tempFile != null)
                {
                    lineOutputs.Add(tempFile);
                }
            }
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
        await Task.CompletedTask.ConfigureAwait(false);
        _logger.LogError("Windows TTS is not available on this platform. Cannot synthesize audio.");
        throw new PlatformNotSupportedException(
            "Windows TTS is only available on Windows 10 (build 19041) or later. " +
            "Please use a different TTS provider (Piper, Mimic3, ElevenLabs, or PlayHT) on this platform.");
#endif
    }
    
#if WINDOWS10_0_19041_0_OR_GREATER
    /// <summary>
    /// Synthesize a text chunk with timeout handling
    /// </summary>
    private async Task<string?> SynthesizeChunkWithTimeoutAsync(
        string text,
        VoiceSpec spec,
        string identifier,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            // Create SSML with prosody adjustments
            string ssml = CreateSsml(text, spec);
            
            // Synthesize the speech with timeout
            using var stream = await _synthesizer.SynthesizeSsmlToStreamAsync(ssml)
                .AsTask(timeoutCts.Token).ConfigureAwait(false);
            
            // Save to temp file
            string tempFile = Path.Combine(_outputDirectory, $"line_{identifier}.wav");
            
            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            {
                await stream.AsStreamForRead().CopyToAsync(fileStream, 81920, timeoutCts.Token).ConfigureAwait(false);
            }
            
            _logger.LogDebug("Synthesized chunk {Identifier}: {Text}",
                identifier,
                text.Length > 30 ? string.Concat(text.AsSpan(0, 30), "...") : text);
            
            return tempFile;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Windows TTS synthesis timed out after {Timeout}s for chunk {Identifier}",
                timeout.TotalSeconds, identifier);
            throw new TimeoutException($"Windows TTS synthesis exceeded {timeout.TotalSeconds} second timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows TTS synthesis failed for chunk {Identifier}: {Message}",
                identifier, ex.Message);
            
            // Provide specific error messages for common failure modes
            if (ex.Message.Contains("SAPI") || ex.Message.Contains("speech"))
            {
                throw new InvalidOperationException(
                    $"Windows TTS engine error: {ex.Message}. " +
                    "This may indicate a problem with the Windows Speech API or voice configuration.",
                    ex);
            }
            
            throw;
        }
    }

    /// <summary>
    /// Chunk text for Windows TTS (splits at sentence boundaries while preserving original punctuation)
    /// </summary>
    private List<string> ChunkTextForWindowsTts(string text)
    {
        const int MaxChunkChars = 450;
        var chunks = new List<string>();
        
        // Split by sentence boundaries while preserving punctuation
        var sentences = new List<(string Text, char Punctuation)>();
        var currentSentence = new StringBuilder();
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            if (c == '.' || c == '!' || c == '?')
            {
                currentSentence.Append(c);
                var sentenceText = currentSentence.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(sentenceText))
                {
                    // Extract the punctuation (last character)
                    var punctuation = c;
                    var textWithoutPunctuation = sentenceText.Substring(0, sentenceText.Length - 1).Trim();
                    sentences.Add((textWithoutPunctuation, punctuation));
                }
                currentSentence.Clear();
                
                // Skip any whitespace after punctuation
                while (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
                {
                    i++;
                }
            }
            else
            {
                currentSentence.Append(c);
            }
        }
        
        // Handle any remaining text (no trailing punctuation)
        if (currentSentence.Length > 0)
        {
            var remaining = currentSentence.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                sentences.Add((remaining, '.'));
            }
        }
        
        // Build chunks preserving original punctuation
        var currentChunk = new StringBuilder();
        foreach (var (sentenceText, punctuation) in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentenceText))
            {
                continue;
            }

            // Calculate space needed: sentence + punctuation + space separator
            int spaceNeeded = sentenceText.Length + 1 + (currentChunk.Length > 0 ? 1 : 0);
            
            if (currentChunk.Length + spaceNeeded > MaxChunkChars && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
            }

            if (currentChunk.Length > 0)
            {
                currentChunk.Append(' ');
            }
            currentChunk.Append(sentenceText);
            currentChunk.Append(punctuation);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks.Count > 0 ? chunks : new List<string> { text };
    }

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