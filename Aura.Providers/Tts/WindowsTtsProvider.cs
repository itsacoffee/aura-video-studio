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
        
        // Handle "default" voice name by using the system default
        var requestedVoice = spec.VoiceName?.Trim();
        if (string.IsNullOrWhiteSpace(requestedVoice) || 
            requestedVoice.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            selectedVoice = SpeechSynthesizer.DefaultVoice;
            _logger.LogInformation("Using system default voice: {DefaultVoice}", selectedVoice?.DisplayName ?? "Unknown");
        }
        else
        {
            // Try to find the requested voice by exact match or partial match
            foreach (var voice in SpeechSynthesizer.AllVoices)
            {
                if (voice.DisplayName.Equals(requestedVoice, StringComparison.OrdinalIgnoreCase))
                {
                    selectedVoice = voice;
                    break;
                }
            }
            
            // If exact match not found, try partial match
            if (selectedVoice == null)
            {
                foreach (var voice in SpeechSynthesizer.AllVoices)
                {
                    if (voice.DisplayName.Contains(requestedVoice, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedVoice = voice;
                        _logger.LogInformation("Using partial match voice: {MatchedVoice} for requested {RequestedVoice}", 
                            voice.DisplayName, requestedVoice);
                        break;
                    }
                }
            }
        }
        
        if (selectedVoice == null)
        {
            _logger.LogWarning("Voice {Voice} not found, using default voice", spec.VoiceName);
            selectedVoice = SpeechSynthesizer.DefaultVoice;
        }
        
        // Update the spec with the actual voice name for SSML generation
        var actualVoiceName = selectedVoice?.DisplayName ?? "Microsoft David Desktop";
        var effectiveSpec = spec with { VoiceName = actualVoiceName };
        _logger.LogDebug("Effective voice for SSML: {VoiceName}", actualVoiceName);
        
        // Set the voice
        _synthesizer.Voice = selectedVoice;
        
        // Prepare the output file
        string outputFilePath = Path.Combine(_outputDirectory, $"narration_{DateTime.Now:yyyyMMddHHmmss}.wav");
        
        // Process each line with chunking and timeout support
        // Use a dictionary to track per-line outputs (key: line index, value: list of chunk temp files)
        var lineChunkOutputs = new Dictionary<int, List<string>>();
        var allTempFiles = new List<string>(); // Track all temp files for cleanup
        
        // Constants for audio processing
        const int MaxCharsPerChunk = 450; // Safe limit for Windows TTS
        const int WavHeaderSize = 44; // Standard WAV header size in bytes
        const double WavBytesPerSecond = 44100.0; // 22050 Hz sample rate * 2 bytes per sample (16-bit mono) = 44100 bytes/sec
        const double DefaultChunkDurationSeconds = 5.0; // Fallback duration estimate for chunks
        
        var linesList = lines.ToList();
        for (int lineIndex = 0; lineIndex < linesList.Count; lineIndex++)
        {
            var line = linesList[lineIndex];
            ct.ThrowIfCancellationRequested();
            
            lineChunkOutputs[lineIndex] = new List<string>();
            
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
                        effectiveSpec,
                        $"{line.SceneIndex}_{chunkIndex}",
                        TimeSpan.FromSeconds(30),
                        ct).ConfigureAwait(false);
                    
                    if (chunkTempFile != null)
                    {
                        lineChunkOutputs[lineIndex].Add(chunkTempFile);
                        allTempFiles.Add(chunkTempFile);
                    }
                }
            }
            else
            {
                // Normal processing for short lines
                var tempFile = await SynthesizeChunkWithTimeoutAsync(
                    line.Text,
                    effectiveSpec,
                    line.SceneIndex.ToString(),
                    TimeSpan.FromSeconds(30),
                    ct).ConfigureAwait(false);
                
                if (tempFile != null)
                {
                    lineChunkOutputs[lineIndex].Add(tempFile);
                    allTempFiles.Add(tempFile);
                }
            }
        }
        
        // Create per-line merged files for lines that were chunked, or use single chunk directly
        // Store tuples of (line, outputFile) to maintain correct mapping
        var successfulLineOutputs = new List<(ScriptLine Line, string OutputFile)>();
        var mergedLineFiles = new List<string>(); // Track merged line files for cleanup
        
        for (int lineIndex = 0; lineIndex < linesList.Count; lineIndex++)
        {
            var line = linesList[lineIndex];
            var chunkFiles = lineChunkOutputs.GetValueOrDefault(lineIndex) ?? new List<string>();
            
            if (chunkFiles.Count == 0)
            {
                _logger.LogWarning("No audio generated for line {LineIndex}", lineIndex);
                continue;
            }
            else if (chunkFiles.Count == 1)
            {
                // Single file (no chunking needed) - use directly
                successfulLineOutputs.Add((line, chunkFiles[0]));
            }
            else
            {
                // Multiple chunks need to be merged into a single line file first
                var lineOutputFile = Path.Combine(_outputDirectory, $"line_{lineIndex}_{DateTime.Now:yyyyMMddHHmmss}.wav");
                mergedLineFiles.Add(lineOutputFile);
                
                // Create segments for chunk merging (chunks are contiguous, no gaps)
                var chunkSegments = new List<WavSegment>();
                TimeSpan chunkOffset = TimeSpan.Zero;
                
                foreach (var chunkFile in chunkFiles)
                {
                    // Estimate chunk duration from file size using WAV format constants
                    var chunkDuration = TimeSpan.FromSeconds(DefaultChunkDurationSeconds);
                    try
                    {
                        var fileInfo = new FileInfo(chunkFile);
                        var dataBytes = Math.Max(0, fileInfo.Length - WavHeaderSize);
                        chunkDuration = TimeSpan.FromSeconds(dataBytes / WavBytesPerSecond);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogDebug(ex, "Could not read file size for chunk {ChunkFile}, using default duration", chunkFile);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogDebug(ex, "Access denied reading chunk {ChunkFile}, using default duration", chunkFile);
                    }
                    
                    chunkSegments.Add(new WavSegment(
                        FilePath: chunkFile,
                        StartTime: chunkOffset,
                        Duration: chunkDuration
                    ));
                    chunkOffset += chunkDuration;
                }
                
                _logger.LogDebug("Merging {Count} chunks for line {LineIndex}", chunkFiles.Count, lineIndex);
                WavMerger.MergeWavFiles(chunkSegments, lineOutputFile);
                successfulLineOutputs.Add((line, lineOutputFile));
            }
        }
        
        // Combine all line audio files into one using WAV merger
        _logger.LogInformation("Synthesized {Count} lines, combining into final output", successfulLineOutputs.Count);
        
        if (successfulLineOutputs.Count > 0)
        {
            var segments = successfulLineOutputs
                .Select(item => 
                    new WavSegment(
                        FilePath: item.OutputFile,
                        StartTime: item.Line.Start,
                        Duration: item.Line.Duration
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
        
        // Clean up all temp files (individual chunks and merged line files)
        var filesToCleanup = new List<string>(allTempFiles);
        filesToCleanup.AddRange(mergedLineFiles);
        
        foreach (var file in filesToCleanup)
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
            _logger.LogWarning("Windows TTS synthesis timed out after {Timeout}s for chunk {Identifier}. Text length: {Length} chars",
                timeout.TotalSeconds, identifier, text.Length);
            throw new TimeoutException(
                $"Windows TTS synthesis exceeded {timeout.TotalSeconds} second timeout for chunk '{identifier}'. " +
                $"This may occur with very long text segments. Consider using shorter chunks or a different TTS provider.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Windows TTS synthesis failed for chunk {Identifier} (text length: {Length}): {Message}",
                identifier, text.Length, ex.Message);
            
            // Provide specific error messages for common failure modes
            if (ex.Message.Contains("SAPI") || ex.Message.Contains("speech") || ex.Message.Contains("SpeechSynthesizer"))
            {
                throw new InvalidOperationException(
                    $"Windows TTS engine error for chunk '{identifier}': {ex.Message}. " +
                    "This may indicate a problem with the Windows Speech API, voice configuration, or text encoding. " +
                    "Try using a different voice or check Windows Speech settings.",
                    ex);
            }
            
            if (ex is UnauthorizedAccessException || ex.Message.Contains("access") || ex.Message.Contains("permission"))
            {
                throw new InvalidOperationException(
                    $"Access denied when synthesizing chunk '{identifier}'. " +
                    "Check file permissions and ensure the output directory is writable.",
                    ex);
            }
            
            if (ex.Message.Contains("encoding") || ex.Message.Contains("character"))
            {
                throw new InvalidOperationException(
                    $"Text encoding error for chunk '{identifier}': {ex.Message}. " +
                    "The text may contain unsupported characters for Windows TTS.",
                    ex);
            }
            
            throw new InvalidOperationException(
                $"Windows TTS synthesis failed for chunk '{identifier}': {ex.Message}. " +
                "This may be a transient error - try again or use a different TTS provider.",
                ex);
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