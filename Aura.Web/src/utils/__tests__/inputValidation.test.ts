import { describe, it, expect } from 'vitest';
import {
  validateVideoTitle,
  validateVideoDescription,
  validateApiKey,
  validateDuration,
  validateFileSize,
  validateImageResolution,
  validateUrl,
  validateEmail,
  validateNumber,
  validateArrayLength,
  combineValidations,
} from '../inputValidation';

describe('inputValidation', () => {
  describe('validateVideoTitle', () => {
    it('should accept valid titles', () => {
      const result = validateVideoTitle('My Video Title');
      expect(result.isValid).toBe(true);
    });

    it('should reject empty titles', () => {
      const result = validateVideoTitle('');
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('required');
    });

    it('should reject titles that are too long', () => {
      const longTitle = 'a'.repeat(201);
      const result = validateVideoTitle(longTitle);
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('200 characters');
    });

    it('should reject titles with invalid filename characters', () => {
      const result = validateVideoTitle('Invalid<Title>');
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('invalid characters');
    });
  });

  describe('validateVideoDescription', () => {
    it('should accept valid descriptions', () => {
      const result = validateVideoDescription('This is a valid description');
      expect(result.isValid).toBe(true);
    });

    it('should reject empty descriptions', () => {
      const result = validateVideoDescription('');
      expect(result.isValid).toBe(false);
    });

    it('should reject descriptions that are too short', () => {
      const result = validateVideoDescription('Short');
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('10 characters');
    });

    it('should reject descriptions that are too long', () => {
      const longDesc = 'a'.repeat(5001);
      const result = validateVideoDescription(longDesc);
      expect(result.isValid).toBe(false);
    });
  });

  describe('validateApiKey', () => {
    it('should accept valid API keys', () => {
      const result = validateApiKey('OpenAI', 'sk-1234567890abcdefghijklmnop');
      expect(result.isValid).toBe(true);
    });

    it('should reject empty keys', () => {
      const result = validateApiKey('OpenAI', '');
      expect(result.isValid).toBe(false);
    });

    it('should reject keys with whitespace', () => {
      const result = validateApiKey('OpenAI', 'sk-123 456');
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('spaces');
    });

    it('should reject keys with Bearer prefix', () => {
      const result = validateApiKey('OpenAI', 'Bearer sk-123456');
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('Bearer');
    });

    it('should warn for OpenAI keys not starting with sk-', () => {
      const result = validateApiKey('OpenAI', 'invalid-1234567890');
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });

    it('should warn for Anthropic keys not starting with sk-ant-', () => {
      const result = validateApiKey('Anthropic', 'sk-1234567890abcdefghij');
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });
  });

  describe('validateDuration', () => {
    it('should accept valid durations', () => {
      const result = validateDuration(60);
      expect(result.isValid).toBe(true);
    });

    it('should reject zero or negative durations', () => {
      expect(validateDuration(0).isValid).toBe(false);
      expect(validateDuration(-10).isValid).toBe(false);
    });

    it('should reject durations over 10 minutes', () => {
      const result = validateDuration(601);
      expect(result.isValid).toBe(false);
    });

    it('should warn for long durations', () => {
      const result = validateDuration(200);
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });
  });

  describe('validateFileSize', () => {
    it('should accept valid file sizes', () => {
      const result = validateFileSize(50 * 1024 * 1024); // 50MB
      expect(result.isValid).toBe(true);
    });

    it('should reject files over the limit', () => {
      const result = validateFileSize(101 * 1024 * 1024); // 101MB
      expect(result.isValid).toBe(false);
    });

    it('should warn for files close to limit', () => {
      const result = validateFileSize(85 * 1024 * 1024); // 85MB
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });
  });

  describe('validateImageResolution', () => {
    it('should accept valid resolutions', () => {
      const result = validateImageResolution(1920, 1080);
      expect(result.isValid).toBe(true);
    });

    it('should reject invalid dimensions', () => {
      expect(validateImageResolution(0, 1080).isValid).toBe(false);
      expect(validateImageResolution(1920, 0).isValid).toBe(false);
    });

    it('should reject resolutions over 4096', () => {
      const result = validateImageResolution(5000, 5000);
      expect(result.isValid).toBe(false);
    });

    it('should warn for unusual aspect ratios', () => {
      const result = validateImageResolution(1000, 100); // 10:1 aspect ratio
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });

    it('should warn for high resolutions', () => {
      const result = validateImageResolution(3840, 2160); // 4K
      expect(result.isValid).toBe(true);
      expect(result.warning).toBeDefined();
    });
  });

  describe('validateUrl', () => {
    it('should accept valid URLs', () => {
      expect(validateUrl('https://example.com').isValid).toBe(true);
      expect(validateUrl('http://example.com').isValid).toBe(true);
    });

    it('should reject empty URLs', () => {
      const result = validateUrl('');
      expect(result.isValid).toBe(false);
    });

    it('should reject invalid URLs', () => {
      const result = validateUrl('not-a-url');
      expect(result.isValid).toBe(false);
    });

    it('should reject non-HTTP protocols', () => {
      const result = validateUrl('ftp://example.com');
      expect(result.isValid).toBe(false);
    });
  });

  describe('validateEmail', () => {
    it('should accept valid emails', () => {
      expect(validateEmail('user@example.com').isValid).toBe(true);
      expect(validateEmail('user.name+tag@example.co.uk').isValid).toBe(true);
    });

    it('should reject invalid emails', () => {
      expect(validateEmail('').isValid).toBe(false);
      expect(validateEmail('not-an-email').isValid).toBe(false);
      expect(validateEmail('@example.com').isValid).toBe(false);
      expect(validateEmail('user@').isValid).toBe(false);
    });
  });

  describe('validateNumber', () => {
    it('should accept valid numbers', () => {
      const result = validateNumber(50, 0, 100);
      expect(result.isValid).toBe(true);
    });

    it('should reject NaN', () => {
      const result = validateNumber(NaN);
      expect(result.isValid).toBe(false);
    });

    it('should enforce minimum', () => {
      const result = validateNumber(5, 10);
      expect(result.isValid).toBe(false);
    });

    it('should enforce maximum', () => {
      const result = validateNumber(150, undefined, 100);
      expect(result.isValid).toBe(false);
    });
  });

  describe('validateArrayLength', () => {
    it('should accept valid array lengths', () => {
      const result = validateArrayLength([1, 2, 3], 2, 5);
      expect(result.isValid).toBe(true);
    });

    it('should enforce minimum length', () => {
      const result = validateArrayLength([1], 2);
      expect(result.isValid).toBe(false);
    });

    it('should enforce maximum length', () => {
      const result = validateArrayLength([1, 2, 3, 4, 5, 6], undefined, 5);
      expect(result.isValid).toBe(false);
    });
  });

  describe('combineValidations', () => {
    it('should combine multiple valid results', () => {
      const result = combineValidations(
        { isValid: true },
        { isValid: true },
        { isValid: true }
      );
      expect(result.isValid).toBe(true);
    });

    it('should fail if any result is invalid', () => {
      const result = combineValidations(
        { isValid: true },
        { isValid: false, error: 'Error 1' },
        { isValid: false, error: 'Error 2' }
      );
      expect(result.isValid).toBe(false);
      expect(result.error).toContain('Error 1');
      expect(result.error).toContain('Error 2');
    });

    it('should combine warnings', () => {
      const result = combineValidations(
        { isValid: true, warning: 'Warning 1' },
        { isValid: true, warning: 'Warning 2' }
      );
      expect(result.isValid).toBe(true);
      expect(result.warning).toContain('Warning 1');
      expect(result.warning).toContain('Warning 2');
    });
  });
});
