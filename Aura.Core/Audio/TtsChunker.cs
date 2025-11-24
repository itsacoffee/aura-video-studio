using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aura.Core.Audio;

/// <summary>
/// Text chunk for TTS processing
/// </summary>
public record TextChunk(int Index, string Text);

/// <summary>
/// Chunks long text into smaller segments suitable for TTS processing
/// Respects sentence boundaries to maintain natural speech flow
/// </summary>
public class TtsChunker
{
    private const int MaxChunkChars = 450; // Safe limit for Windows TTS
    private const int IdealChunkChars = 300;

    /// <summary>
    /// Chunk text into smaller segments for TTS processing
    /// </summary>
    public IReadOnlyList<TextChunk> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<TextChunk> { new TextChunk(0, text ?? string.Empty) };
        }

        var chunks = new List<TextChunk>();
        var sentences = SplitIntoSentences(text);
        var currentChunk = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            if (string.IsNullOrWhiteSpace(trimmedSentence))
            {
                continue;
            }

            // If adding this sentence would exceed max, finalize current chunk
            if (currentChunk.Length > 0 && currentChunk.Length + trimmedSentence.Length + 1 > MaxChunkChars)
            {
                chunks.Add(new TextChunk(chunks.Count, currentChunk.ToString().Trim()));
                currentChunk.Clear();
            }

            // If a single sentence exceeds max, split it by commas/clauses
            if (trimmedSentence.Length > MaxChunkChars)
            {
                // Finalize current chunk if any
                if (currentChunk.Length > 0)
                {
                    chunks.Add(new TextChunk(chunks.Count, currentChunk.ToString().Trim()));
                    currentChunk.Clear();
                }

                // Split long sentence by commas, semicolons, or conjunctions
                var subSentences = SplitLongSentence(trimmedSentence);
                foreach (var subSentence in subSentences)
                {
                    if (currentChunk.Length + subSentence.Length + 1 > MaxChunkChars)
                    {
                        if (currentChunk.Length > 0)
                        {
                            chunks.Add(new TextChunk(chunks.Count, currentChunk.ToString().Trim()));
                            currentChunk.Clear();
                        }
                    }
                    currentChunk.Append(subSentence).Append(' ');
                }
            }
            else
            {
                currentChunk.Append(trimmedSentence).Append(' ');
            }
        }

        // Add final chunk if any
        if (currentChunk.Length > 0)
        {
            chunks.Add(new TextChunk(chunks.Count, currentChunk.ToString().Trim()));
        }

        // If no chunks were created (edge case), create one with the original text
        if (chunks.Count == 0)
        {
            chunks.Add(new TextChunk(0, text));
        }

        return chunks;
    }

    /// <summary>
    /// Split text into sentences using punctuation
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        // Pattern to match sentence endings: period, exclamation, question mark
        // Followed by whitespace or end of string
        var pattern = @"([.!?]+)\s+";
        var sentences = new List<string>();
        var matches = Regex.Matches(text, pattern);

        if (matches.Count == 0)
        {
            // No sentence endings found, return entire text as one sentence
            return new List<string> { text };
        }

        int lastIndex = 0;
        foreach (Match match in matches)
        {
            var sentence = text.Substring(lastIndex, match.Index + match.Length - lastIndex).Trim();
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                sentences.Add(sentence);
            }
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last sentence
        if (lastIndex < text.Length)
        {
            var remaining = text.Substring(lastIndex).Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                sentences.Add(remaining);
            }
        }

        return sentences;
    }

    /// <summary>
    /// Split a very long sentence into smaller parts using commas, semicolons, or conjunctions
    /// </summary>
    private List<string> SplitLongSentence(string sentence)
    {
        var parts = new List<string>();

        // Try splitting by semicolons first
        var semicolonParts = sentence.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (semicolonParts.Length > 1)
        {
            foreach (var part in semicolonParts)
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    parts.Add(trimmed);
                }
            }
            return parts;
        }

        // Try splitting by commas
        var commaParts = sentence.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (commaParts.Length > 1)
        {
            // Recombine if parts are too small
            var currentPart = new StringBuilder();
            foreach (var part in commaParts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (currentPart.Length + trimmed.Length + 2 > IdealChunkChars && currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }

                if (currentPart.Length > 0)
                {
                    currentPart.Append(", ");
                }
                currentPart.Append(trimmed);
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString().Trim());
            }

            if (parts.Count > 0)
            {
                return parts;
            }
        }

        // Last resort: split by spaces if still too long
        if (sentence.Length > MaxChunkChars)
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentPart = new StringBuilder();
            foreach (var word in words)
            {
                if (currentPart.Length + word.Length + 1 > MaxChunkChars && currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString().Trim());
                    currentPart.Clear();
                }
                if (currentPart.Length > 0)
                {
                    currentPart.Append(' ');
                }
                currentPart.Append(word);
            }
            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString().Trim());
            }
        }
        else
        {
            parts.Add(sentence);
        }

        return parts;
    }
}

