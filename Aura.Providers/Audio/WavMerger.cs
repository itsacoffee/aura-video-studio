using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aura.Providers.Audio;

/// <summary>
/// Simple WAV file merger for combining scene-aware audio files.
/// Handles 16-bit PCM WAV files only.
/// </summary>
public static class WavMerger
{
    /// <summary>
    /// Merges multiple WAV files into a single output file with gaps for timing.
    /// </summary>
    public static void MergeWavFiles(IEnumerable<WavSegment> segments, string outputPath)
    {
        var segmentsList = segments.OrderBy(s => s.StartTime).ToList();
        
        if (segmentsList.Count == 0)
        {
            throw new ArgumentException("No segments to merge");
        }

        // Read the first file to get format info
        var firstSegment = segmentsList[0];
        using var firstStream = new FileStream(firstSegment.FilePath, FileMode.Open, FileAccess.Read);
        using var firstReader = new BinaryReader(firstStream);
        
        var header = ReadWavHeader(firstReader);
        
        // Calculate total duration needed
        var lastSegment = segmentsList[segmentsList.Count - 1];
        var totalDuration = lastSegment.StartTime + lastSegment.Duration;
        int totalSamples = (int)(totalDuration.TotalSeconds * header.SampleRate);
        
        // Create output buffer
        var outputSamples = new short[totalSamples];
        
        // Process each segment
        foreach (var segment in segmentsList)
        {
            int startSample = (int)(segment.StartTime.TotalSeconds * header.SampleRate);
            
            using var segmentStream = new FileStream(segment.FilePath, FileMode.Open, FileAccess.Read);
            using var segmentReader = new BinaryReader(segmentStream);
            
            // Skip header
            segmentReader.BaseStream.Seek(44, SeekOrigin.Begin);
            
            // Read samples and copy to output buffer
            int sampleIndex = startSample;
            while (segmentReader.BaseStream.Position < segmentReader.BaseStream.Length && sampleIndex < totalSamples)
            {
                try
                {
                    short sample = segmentReader.ReadInt16();
                    outputSamples[sampleIndex] = sample;
                    sampleIndex++;
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }
        }
        
        // Write output file
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var outputWriter = new BinaryWriter(outputStream);
        
        WriteWavFile(outputWriter, outputSamples, header.SampleRate, header.NumChannels, header.BitsPerSample);
    }

    private static WavHeader ReadWavHeader(BinaryReader reader)
    {
        // Read RIFF header
        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
            throw new InvalidDataException("Not a valid WAV file");
        
        reader.ReadInt32(); // File size
        
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
            throw new InvalidDataException("Not a valid WAV file");
        
        // Read fmt chunk
        var fmt = new string(reader.ReadChars(4));
        if (fmt != "fmt ")
            throw new InvalidDataException("Missing fmt chunk");
        
        int fmtSize = reader.ReadInt32();
        reader.ReadInt16(); // Audio format
        short numChannels = reader.ReadInt16();
        int sampleRate = reader.ReadInt32();
        reader.ReadInt32(); // Byte rate
        reader.ReadInt16(); // Block align
        short bitsPerSample = reader.ReadInt16();
        
        // Skip any extra fmt bytes
        if (fmtSize > 16)
        {
            reader.BaseStream.Seek(fmtSize - 16, SeekOrigin.Current);
        }
        
        // Find data chunk
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            
            if (chunkId == "data")
            {
                break;
            }
            else
            {
                // Skip this chunk
                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
            }
        }
        
        return new WavHeader
        {
            SampleRate = sampleRate,
            NumChannels = numChannels,
            BitsPerSample = bitsPerSample
        };
    }

    private static void WriteWavFile(BinaryWriter writer, short[] samples, int sampleRate, short numChannels, short bitsPerSample)
    {
        int dataSize = samples.Length * sizeof(short);
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt subchunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
        
        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }

    private class WavHeader
    {
        public int SampleRate { get; set; }
        public short NumChannels { get; set; }
        public short BitsPerSample { get; set; }
    }
}

public record WavSegment(string FilePath, TimeSpan StartTime, TimeSpan Duration);
