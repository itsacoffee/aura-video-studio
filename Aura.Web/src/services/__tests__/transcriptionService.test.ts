/**
 * Tests for transcriptionService
 *
 * Tests for the transcription service functions including
 * segmentsToCaptions, formatCaptionTime, and caption export functions.
 */

import { describe, it, expect } from 'vitest';
import {
  segmentsToCaptions,
  formatCaptionTime,
  captionsToSrt,
  captionsToVtt,
  estimateTranscriptionTime,
  type TranscriptionSegment,
  type Caption,
} from '../transcriptionService';

describe('transcriptionService', () => {
  describe('formatCaptionTime', () => {
    it('formats zero seconds correctly', () => {
      expect(formatCaptionTime(0, false)).toBe('00:00:00,000');
      expect(formatCaptionTime(0, true)).toBe('00:00:00.000');
    });

    it('formats seconds with milliseconds', () => {
      expect(formatCaptionTime(1.5, false)).toBe('00:00:01,500');
      expect(formatCaptionTime(1.5, true)).toBe('00:00:01.500');
    });

    it('formats minutes correctly', () => {
      expect(formatCaptionTime(65, false)).toBe('00:01:05,000');
      expect(formatCaptionTime(125.25, true)).toBe('00:02:05.250');
    });

    it('formats hours correctly', () => {
      expect(formatCaptionTime(3661.123, false)).toBe('01:01:01,123');
      expect(formatCaptionTime(7200, true)).toBe('02:00:00.000');
    });

    it('uses comma for SRT format', () => {
      const result = formatCaptionTime(10.5, false);
      expect(result).toContain(',');
      expect(result).not.toContain('.');
    });

    it('uses dot for VTT format', () => {
      const result = formatCaptionTime(10.5, true);
      expect(result).toContain('.');
      expect(result).not.toContain(',');
    });
  });

  describe('segmentsToCaptions', () => {
    const createSegment = (
      text: string,
      startTime: number,
      endTime: number
    ): TranscriptionSegment => ({
      text,
      startTime,
      endTime,
      words: text.split(' ').map((word, idx, arr) => ({
        word,
        startTime: startTime + (idx / arr.length) * (endTime - startTime),
        endTime: startTime + ((idx + 1) / arr.length) * (endTime - startTime),
        confidence: 0.9,
      })),
    });

    it('returns empty array for empty segments', () => {
      const result = segmentsToCaptions([]);
      expect(result).toEqual([]);
    });

    it('keeps short segments as-is', () => {
      const segments: TranscriptionSegment[] = [createSegment('Hello world', 0, 2)];

      const result = segmentsToCaptions(segments);

      expect(result).toHaveLength(1);
      expect(result[0].text).toBe('Hello world');
      expect(result[0].startTime).toBe(0);
      expect(result[0].endTime).toBe(2);
    });

    it('splits long segments by character count', () => {
      const longText =
        'This is a very long segment that exceeds the maximum character limit per caption';
      const segments: TranscriptionSegment[] = [createSegment(longText, 0, 10)];

      const result = segmentsToCaptions(segments, 40);

      expect(result.length).toBeGreaterThan(1);
      result.forEach((caption) => {
        expect(caption.text.length).toBeLessThanOrEqual(40);
      });
    });

    it('splits long segments by duration', () => {
      const text = 'Short text';
      const segments: TranscriptionSegment[] = [
        {
          text,
          startTime: 0,
          endTime: 10, // 10 seconds - exceeds max duration
          words: text.split(' ').map((word, idx) => ({
            word,
            startTime: idx * 5,
            endTime: (idx + 1) * 5,
            confidence: 0.9,
          })),
        },
      ];

      const result = segmentsToCaptions(segments, 80, 3);

      expect(result.length).toBeGreaterThan(1);
    });

    it('respects custom maxCharsPerCaption', () => {
      const text = 'Word one two three four five six seven eight';
      const segments: TranscriptionSegment[] = [createSegment(text, 0, 8)];

      const result = segmentsToCaptions(segments, 20);

      result.forEach((caption) => {
        expect(caption.text.length).toBeLessThanOrEqual(20);
      });
    });

    it('maintains time continuity', () => {
      const segments: TranscriptionSegment[] = [
        createSegment('First segment', 0, 3),
        createSegment('Second segment', 3, 6),
        createSegment('Third segment', 6, 9),
      ];

      const result = segmentsToCaptions(segments);

      for (let i = 1; i < result.length; i++) {
        expect(result[i].startTime).toBeGreaterThanOrEqual(result[i - 1].endTime - 0.01);
      }
    });
  });

  describe('captionsToSrt', () => {
    it('returns empty string for empty captions', () => {
      const result = captionsToSrt([]);
      expect(result).toBe('');
    });

    it('formats single caption correctly', () => {
      const captions: Caption[] = [{ startTime: 0, endTime: 2, text: 'Hello world' }];

      const result = captionsToSrt(captions);

      expect(result).toContain('1\n');
      expect(result).toContain('00:00:00,000 --> 00:00:02,000');
      expect(result).toContain('Hello world');
    });

    it('formats multiple captions with sequential indices', () => {
      const captions: Caption[] = [
        { startTime: 0, endTime: 2, text: 'First caption' },
        { startTime: 2, endTime: 4, text: 'Second caption' },
        { startTime: 4, endTime: 6, text: 'Third caption' },
      ];

      const result = captionsToSrt(captions);

      expect(result).toContain('1\n');
      expect(result).toContain('2\n');
      expect(result).toContain('3\n');
      expect(result).toContain('First caption');
      expect(result).toContain('Second caption');
      expect(result).toContain('Third caption');
    });

    it('uses comma as millisecond separator', () => {
      const captions: Caption[] = [{ startTime: 1.5, endTime: 3.25, text: 'Test' }];

      const result = captionsToSrt(captions);

      expect(result).toContain('00:00:01,500');
      expect(result).toContain('00:00:03,250');
    });
  });

  describe('captionsToVtt', () => {
    it('starts with WEBVTT header', () => {
      const captions: Caption[] = [];

      const result = captionsToVtt(captions);

      expect(result.startsWith('WEBVTT')).toBe(true);
    });

    it('formats single caption correctly', () => {
      const captions: Caption[] = [{ startTime: 0, endTime: 2, text: 'Hello world' }];

      const result = captionsToVtt(captions);

      expect(result).toContain('00:00:00.000 --> 00:00:02.000');
      expect(result).toContain('Hello world');
    });

    it('uses dot as millisecond separator', () => {
      const captions: Caption[] = [{ startTime: 1.5, endTime: 3.25, text: 'Test' }];

      const result = captionsToVtt(captions);

      expect(result).toContain('00:00:01.500');
      expect(result).toContain('00:00:03.250');
    });

    it('does not include index numbers like SRT', () => {
      const captions: Caption[] = [
        { startTime: 0, endTime: 2, text: 'First' },
        { startTime: 2, endTime: 4, text: 'Second' },
      ];

      const result = captionsToVtt(captions);

      // VTT should not have "1\n" at start of caption blocks
      const lines = result.split('\n').filter((line) => line.trim());
      const indexLines = lines.filter((line) => /^\d+$/.test(line));
      expect(indexLines).toHaveLength(0);
    });
  });

  describe('estimateTranscriptionTime', () => {
    it('returns approximately 1.2x the audio duration', () => {
      expect(estimateTranscriptionTime(60)).toBe(72);
      expect(estimateTranscriptionTime(100)).toBe(120);
    });

    it('returns 0 for 0 duration', () => {
      expect(estimateTranscriptionTime(0)).toBe(0);
    });

    it('rounds up to whole seconds', () => {
      expect(estimateTranscriptionTime(1)).toBe(2);
      expect(estimateTranscriptionTime(10)).toBe(12);
    });
  });
});
