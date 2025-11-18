import { describe, it, expect } from 'vitest';
import {
  getStatusDisplayText,
  getStatusAppearance,
  formatElapsedTime,
} from '../../services/openAIValidationService';

describe('openAIValidationService', () => {
  describe('getStatusDisplayText', () => {
    it('should return correct text for Valid status', () => {
      const text = getStatusDisplayText('Valid');
      expect(text).toBe('Validated ✓');
    });

    it('should return correct text for Invalid status', () => {
      const text = getStatusDisplayText('Invalid');
      expect(text).toBe('Invalid ✕');
    });

    it('should return correct text for RateLimited status', () => {
      const text = getStatusDisplayText('RateLimited');
      expect(text).toBe('Rate Limited (can continue)');
    });

    it('should return correct text for PermissionDenied status', () => {
      const text = getStatusDisplayText('PermissionDenied');
      expect(text).toBe('Permission Denied');
    });

    it('should return correct text for ServiceIssue status', () => {
      const text = getStatusDisplayText('ServiceIssue');
      expect(text).toBe('Service Issue (can continue)');
    });

    it('should return correct text for NetworkError status', () => {
      const text = getStatusDisplayText('NetworkError');
      expect(text).toBe('Network Error (can continue)');
    });

    it('should return correct text for Timeout status', () => {
      const text = getStatusDisplayText('Timeout');
      expect(text).toBe('Timeout (can continue)');
    });

    it('should return correct text for Offline status', () => {
      const text = getStatusDisplayText('Offline');
      expect(text).toBe('Offline Mode (can continue)');
    });

    it('should return correct text for Pending status', () => {
      const text = getStatusDisplayText('Pending');
      expect(text).toBe('Validating...');
    });
  });

  describe('getStatusAppearance', () => {
    it('should return success appearance for Valid status', () => {
      const appearance = getStatusAppearance('Valid');
      expect(appearance).toBe('success');
    });

    it('should return danger appearance for Invalid status', () => {
      const appearance = getStatusAppearance('Invalid');
      expect(appearance).toBe('danger');
    });

    it('should return danger appearance for PermissionDenied status', () => {
      const appearance = getStatusAppearance('PermissionDenied');
      expect(appearance).toBe('danger');
    });

    it('should return warning appearance for RateLimited status', () => {
      const appearance = getStatusAppearance('RateLimited');
      expect(appearance).toBe('warning');
    });

    it('should return warning appearance for ServiceIssue status', () => {
      const appearance = getStatusAppearance('ServiceIssue');
      expect(appearance).toBe('warning');
    });

    it('should return warning appearance for Offline status', () => {
      const appearance = getStatusAppearance('Offline');
      expect(appearance).toBe('warning');
    });

    it('should return subtle appearance for NetworkError status', () => {
      const appearance = getStatusAppearance('NetworkError');
      expect(appearance).toBe('subtle');
    });

    it('should return subtle appearance for Timeout status', () => {
      const appearance = getStatusAppearance('Timeout');
      expect(appearance).toBe('subtle');
    });

    it('should return subtle appearance for Pending status', () => {
      const appearance = getStatusAppearance('Pending');
      expect(appearance).toBe('subtle');
    });
  });

  describe('formatElapsedTime', () => {
    it('should format elapsed time correctly for small values', () => {
      const formatted = formatElapsedTime(1234);
      expect(formatted).toBe('(1.2s)');
    });

    it('should format elapsed time correctly for large values', () => {
      const formatted = formatElapsedTime(45678);
      expect(formatted).toBe('(45.7s)');
    });

    it('should handle undefined elapsed time', () => {
      const formatted = formatElapsedTime(undefined);
      expect(formatted).toBe('');
    });

    it('should handle zero elapsed time', () => {
      const formatted = formatElapsedTime(0);
      expect(formatted).toBe('(0.0s)');
    });

    it('should round to one decimal place', () => {
      const formatted = formatElapsedTime(1555);
      expect(formatted).toBe('(1.6s)');
    });
  });
});
