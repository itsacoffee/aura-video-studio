import { describe, it, expect, vi } from 'vitest';
import { translationIntegrationService } from '../translationIntegrationService';
import type { LineDto, SubtitleOutputDto } from '@/types/api-v1';

describe('translationIntegrationService', () => {
  describe('formatSubtitleDuration', () => {
    it('formats zero seconds correctly', () => {
      const result = translationIntegrationService.formatSubtitleDuration(0);
      expect(result).toBe('00:00:00.000');
    });

    it('formats seconds correctly', () => {
      const result = translationIntegrationService.formatSubtitleDuration(45.5);
      expect(result).toBe('00:00:45.500');
    });

    it('formats minutes correctly', () => {
      const result = translationIntegrationService.formatSubtitleDuration(125.75);
      expect(result).toBe('00:02:05.750');
    });

    it('formats hours correctly', () => {
      const result = translationIntegrationService.formatSubtitleDuration(3665.123);
      expect(result).toBe('01:01:05.123');
    });

    it('formats milliseconds correctly', () => {
      const result = translationIntegrationService.formatSubtitleDuration(1.0);
      expect(result).toBe('00:00:01.000');
    });
  });

  describe('validateScriptLines', () => {
    it('validates empty array', () => {
      const lines: LineDto[] = [];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(false);
      expect(result.errors).toContain('At least one script line is required');
    });

    it('validates valid script lines', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'Hello', startSeconds: 0, durationSeconds: 2 },
        { sceneIndex: 1, text: 'World', startSeconds: 2, durationSeconds: 2 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('detects empty text', () => {
      const lines: LineDto[] = [{ sceneIndex: 0, text: '', startSeconds: 0, durationSeconds: 2 }];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Line 1: Text is required');
    });

    it('detects negative duration', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'Test', startSeconds: 0, durationSeconds: -1 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Line 1: Duration must be positive');
    });

    it('detects negative start time', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'Test', startSeconds: -1, durationSeconds: 2 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Line 1: Start time must be non-negative');
    });

    it('detects overlapping timecodes', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'First', startSeconds: 0, durationSeconds: 3 },
        { sceneIndex: 1, text: 'Second', startSeconds: 2, durationSeconds: 2 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(false);
      expect(result.errors).toContain('Lines 1 and 2: Overlapping timecodes detected');
    });

    it('allows adjacent timecodes without overlap', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'First', startSeconds: 0, durationSeconds: 2 },
        { sceneIndex: 1, text: 'Second', startSeconds: 2, durationSeconds: 2 },
        { sceneIndex: 2, text: 'Third', startSeconds: 4, durationSeconds: 2 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    it('allows gaps between timecodes', () => {
      const lines: LineDto[] = [
        { sceneIndex: 0, text: 'First', startSeconds: 0, durationSeconds: 2 },
        { sceneIndex: 1, text: 'Second', startSeconds: 3, durationSeconds: 2 },
      ];
      const result = translationIntegrationService.validateScriptLines(lines);

      expect(result.valid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });
  });

  describe('downloadSubtitles', () => {
    it('creates download link for SRT format', () => {
      global.URL.createObjectURL = vi.fn(() => 'mock-url');
      global.URL.revokeObjectURL = vi.fn();

      const mockLink = document.createElement('a');
      const clickSpy = vi.spyOn(mockLink, 'click').mockImplementation(() => {});
      const createElementSpy = vi.spyOn(document, 'createElement').mockReturnValue(mockLink);
      const appendChildSpy = vi
        .spyOn(document.body, 'appendChild')
        .mockImplementation(() => mockLink);
      const removeChildSpy = vi
        .spyOn(document.body, 'removeChild')
        .mockImplementation(() => mockLink);

      const subtitles: SubtitleOutputDto = {
        format: 'SRT',
        content: '1\n00:00:00,000 --> 00:00:02,000\nHello\n',
        lineCount: 1,
      };

      translationIntegrationService.downloadSubtitles(subtitles);

      expect(createElementSpy).toHaveBeenCalledWith('a');
      expect(mockLink.download).toBe('subtitles.srt');
      expect(clickSpy).toHaveBeenCalled();
      expect(appendChildSpy).toHaveBeenCalled();
      expect(removeChildSpy).toHaveBeenCalled();

      createElementSpy.mockRestore();
      appendChildSpy.mockRestore();
      removeChildSpy.mockRestore();
      clickSpy.mockRestore();
    });

    it('uses custom filename when provided', () => {
      global.URL.createObjectURL = vi.fn(() => 'mock-url');
      global.URL.revokeObjectURL = vi.fn();

      const mockLink = document.createElement('a');
      const clickSpy = vi.spyOn(mockLink, 'click').mockImplementation(() => {});
      const createElementSpy = vi.spyOn(document, 'createElement').mockReturnValue(mockLink);
      const appendChildSpy = vi
        .spyOn(document.body, 'appendChild')
        .mockImplementation(() => mockLink);
      const removeChildSpy = vi
        .spyOn(document.body, 'removeChild')
        .mockImplementation(() => mockLink);

      const subtitles: SubtitleOutputDto = {
        format: 'VTT',
        content: 'WEBVTT\n\n00:00:00.000 --> 00:00:02.000\nHello\n',
        lineCount: 1,
      };

      translationIntegrationService.downloadSubtitles(subtitles, 'custom-name.vtt');

      expect(mockLink.download).toBe('custom-name.vtt');

      createElementSpy.mockRestore();
      appendChildSpy.mockRestore();
      removeChildSpy.mockRestore();
      clickSpy.mockRestore();
    });
  });
});
