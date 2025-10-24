import { describe, it, expect } from 'vitest';
import { durationToSeconds, secondsToDuration, formatDuration, calculatePercentageChange } from '../services/pacingService';

describe('pacingService utilities', () => {
  describe('durationToSeconds', () => {
    it('should convert PT15S to 15 seconds', () => {
      expect(durationToSeconds('PT15S')).toBe(15);
    });

    it('should convert PT1M30S to 90 seconds', () => {
      expect(durationToSeconds('PT1M30S')).toBe(90);
    });

    it('should convert PT1H30M45S to 5445 seconds', () => {
      expect(durationToSeconds('PT1H30M45S')).toBe(5445);
    });

    it('should handle PT0S as 0 seconds', () => {
      expect(durationToSeconds('PT0S')).toBe(0);
    });

    it('should handle invalid format gracefully', () => {
      expect(durationToSeconds('invalid')).toBe(0);
    });
  });

  describe('secondsToDuration', () => {
    it('should convert 15 seconds to PT15.0S', () => {
      expect(secondsToDuration(15)).toBe('PT15.0S');
    });

    it('should convert 90 seconds to PT1M30.0S', () => {
      expect(secondsToDuration(90)).toBe('PT1M30.0S');
    });

    it('should convert 3661 seconds to PT1H1M1.0S', () => {
      expect(secondsToDuration(3661)).toBe('PT1H1M1.0S');
    });

    it('should handle 0 seconds', () => {
      expect(secondsToDuration(0)).toBe('PT0.0S');
    });
  });

  describe('formatDuration', () => {
    it('should format PT15S as "15s"', () => {
      expect(formatDuration('PT15S')).toBe('15s');
    });

    it('should format PT1M30S as "1m 30s"', () => {
      expect(formatDuration('PT1M30S')).toBe('1m 30s');
    });

    it('should format PT2M0S as "2m 0s"', () => {
      expect(formatDuration('PT2M0S')).toBe('2m 0s');
    });
  });

  describe('calculatePercentageChange', () => {
    it('should calculate positive percentage change', () => {
      expect(calculatePercentageChange(100, 150)).toBe(50);
    });

    it('should calculate negative percentage change', () => {
      expect(calculatePercentageChange(100, 75)).toBe(-25);
    });

    it('should return 0 when current is 0', () => {
      expect(calculatePercentageChange(0, 100)).toBe(0);
    });

    it('should return 0 when values are equal', () => {
      expect(calculatePercentageChange(100, 100)).toBe(0);
    });
  });
});
