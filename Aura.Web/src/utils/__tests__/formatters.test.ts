import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  formatFileSize,
  formatDuration,
  formatRelativeTime,
  formatNumber,
  formatPercentage,
} from '../formatters';

describe('Formatters', () => {
  describe('formatFileSize', () => {
    it('should format 0 bytes', () => {
      expect(formatFileSize(0)).toBe('0 B');
    });

    it('should format bytes', () => {
      expect(formatFileSize(512)).toBe('512 B');
    });

    it('should format kilobytes', () => {
      expect(formatFileSize(1024)).toBe('1 KB');
      expect(formatFileSize(1536)).toBe('1.5 KB');
    });

    it('should format megabytes', () => {
      expect(formatFileSize(1024 * 1024)).toBe('1 MB');
      expect(formatFileSize(1024 * 1024 * 2.5)).toBe('2.5 MB');
    });

    it('should format gigabytes', () => {
      expect(formatFileSize(1024 * 1024 * 1024)).toBe('1 GB');
    });

    it('should format terabytes', () => {
      expect(formatFileSize(1024 * 1024 * 1024 * 1024)).toBe('1 TB');
    });
  });

  describe('formatDuration', () => {
    it('should format seconds only', () => {
      expect(formatDuration(30)).toBe('0:30');
      expect(formatDuration(5)).toBe('0:05');
    });

    it('should format minutes and seconds', () => {
      expect(formatDuration(90)).toBe('1:30');
      expect(formatDuration(125)).toBe('2:05');
    });

    it('should format hours, minutes, and seconds', () => {
      expect(formatDuration(3600)).toBe('1:00:00');
      expect(formatDuration(3665)).toBe('1:01:05');
      expect(formatDuration(7325)).toBe('2:02:05');
    });

    it('should handle zero duration', () => {
      expect(formatDuration(0)).toBe('0:00');
    });
  });

  describe('formatRelativeTime', () => {
    let originalDate: typeof Date;

    beforeEach(() => {
      originalDate = global.Date;
      const mockDate = new Date('2024-01-15T12:00:00Z');
      vi.useFakeTimers();
      vi.setSystemTime(mockDate);
    });

    afterEach(() => {
      vi.useRealTimers();
      global.Date = originalDate;
    });

    it('should return "just now" for recent times', () => {
      const now = new Date('2024-01-15T12:00:00Z');
      const recent = new Date('2024-01-15T11:59:30Z');
      expect(formatRelativeTime(recent)).toBe('just now');
    });

    it('should format minutes ago', () => {
      const date = new Date('2024-01-15T11:55:00Z');
      expect(formatRelativeTime(date)).toBe('5 minutes ago');
    });

    it('should format hours ago', () => {
      const date = new Date('2024-01-15T09:00:00Z');
      expect(formatRelativeTime(date)).toBe('3 hours ago');
    });

    it('should format days ago', () => {
      const date = new Date('2024-01-13T12:00:00Z');
      expect(formatRelativeTime(date)).toBe('2 days ago');
    });

    it('should format date for older times', () => {
      const date = new Date('2024-01-01T12:00:00Z');
      expect(formatRelativeTime(date)).toMatch(/1\/1\/2024|01\/01\/2024/);
    });

    it('should handle string dates', () => {
      const dateStr = '2024-01-15T11:55:00Z';
      expect(formatRelativeTime(dateStr)).toBe('5 minutes ago');
    });

    it('should handle singular units correctly', () => {
      const oneMinuteAgo = new Date('2024-01-15T11:59:00Z');
      expect(formatRelativeTime(oneMinuteAgo)).toBe('1 minute ago');

      const oneHourAgo = new Date('2024-01-15T11:00:00Z');
      expect(formatRelativeTime(oneHourAgo)).toBe('1 hour ago');

      const oneDayAgo = new Date('2024-01-14T12:00:00Z');
      expect(formatRelativeTime(oneDayAgo)).toBe('1 day ago');
    });
  });

  describe('formatNumber', () => {
    it('should format numbers with thousand separators', () => {
      expect(formatNumber(1000)).toMatch(/1[,\s]000/);
      expect(formatNumber(1000000)).toMatch(/1[,\s]000[,\s]000/);
    });

    it('should handle small numbers', () => {
      expect(formatNumber(100)).toBe('100');
      expect(formatNumber(0)).toBe('0');
    });
  });

  describe('formatPercentage', () => {
    it('should format percentage with no decimals by default', () => {
      expect(formatPercentage(50)).toBe('50%');
      expect(formatPercentage(75.5)).toBe('76%');
    });

    it('should format percentage with specified decimals', () => {
      expect(formatPercentage(50.123, 2)).toBe('50.12%');
      expect(formatPercentage(75.5, 1)).toBe('75.5%');
    });

    it('should handle zero', () => {
      expect(formatPercentage(0)).toBe('0%');
    });

    it('should handle 100%', () => {
      expect(formatPercentage(100)).toBe('100%');
    });
  });
});
