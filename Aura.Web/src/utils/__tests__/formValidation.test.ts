/**
 * Tests for form validation utilities
 */

import { describe, it, expect } from 'vitest';
import {
  validators,
  validateBriefRequest,
  apiKeysSchema,
  providerPathsSchema,
} from '../formValidation';

describe('Form Validation', () => {
  describe('validators', () => {
    it('should validate required fields', () => {
      const schema = validators.required();
      expect(schema.safeParse('test').success).toBe(true);
      expect(schema.safeParse('').success).toBe(false);
    });

    it('should validate string length', () => {
      const schema = validators.length(3, 10);
      expect(schema.safeParse('test').success).toBe(true);
      expect(schema.safeParse('ab').success).toBe(false);
      expect(schema.safeParse('verylongstring').success).toBe(false);
    });

    it('should validate email addresses', () => {
      const schema = validators.email();
      expect(schema.safeParse('test@example.com').success).toBe(true);
      expect(schema.safeParse('invalid-email').success).toBe(false);
    });

    it('should validate URLs', () => {
      const schema = validators.url();
      expect(schema.safeParse('https://example.com').success).toBe(true);
      expect(schema.safeParse('not-a-url').success).toBe(false);
    });

    it('should validate HTTP URLs', () => {
      const schema = validators.httpUrl();
      expect(schema.safeParse('http://example.com').success).toBe(true);
      expect(schema.safeParse('https://example.com').success).toBe(true);
      expect(schema.safeParse('ftp://example.com').success).toBe(false);
    });

    it('should validate positive numbers', () => {
      const schema = validators.positiveNumber();
      expect(schema.safeParse(10).success).toBe(true);
      expect(schema.safeParse(-5).success).toBe(false);
    });

    it('should validate number ranges', () => {
      const schema = validators.range(1, 10);
      expect(schema.safeParse(5).success).toBe(true);
      expect(schema.safeParse(0).success).toBe(false);
      expect(schema.safeParse(11).success).toBe(false);
    });

    it('should validate hex colors', () => {
      const schema = validators.hexColor();
      expect(schema.safeParse('FF0000').success).toBe(true);
      expect(schema.safeParse('abc123').success).toBe(true);
      expect(schema.safeParse('invalid').success).toBe(false);
      expect(schema.safeParse('#FF0000').success).toBe(false); // Should not include #
    });

    it('should validate API keys', () => {
      const schema = validators.apiKey(10);
      expect(schema.safeParse('abc123def456').success).toBe(true);
      expect(schema.safeParse('short').success).toBe(false);
      expect(schema.safeParse('has spaces!').success).toBe(false);
    });
  });

  describe('validateBriefRequest', () => {
    it('should validate a valid brief', () => {
      const result = validateBriefRequest({
        topic: 'Machine Learning',
        durationMinutes: 3,
      });
      expect(result.valid).toBe(true);
      expect(result.errors).toEqual([]);
    });

    it('should reject brief with short topic', () => {
      const result = validateBriefRequest({
        topic: 'ML',
        durationMinutes: 3,
      });
      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });

    it('should reject brief with invalid duration', () => {
      const result = validateBriefRequest({
        topic: 'Machine Learning',
        durationMinutes: 0.05, // Too short (< 10 seconds)
      });
      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
    });
  });

  describe('apiKeysSchema', () => {
    it('should validate OpenAI API key format', () => {
      const result = apiKeysSchema.safeParse({
        openai: 'sk-1234567890abcdefghij',
      });
      expect(result.success).toBe(true);
    });

    it('should reject invalid OpenAI API key', () => {
      const result = apiKeysSchema.safeParse({
        openai: 'invalid-key',
      });
      expect(result.success).toBe(false);
    });

    it('should allow empty API keys', () => {
      const result = apiKeysSchema.safeParse({
        openai: '',
        elevenlabs: '',
      });
      expect(result.success).toBe(true);
    });
  });

  describe('providerPathsSchema', () => {
    it('should validate Stable Diffusion URL', () => {
      const result = providerPathsSchema.safeParse({
        stableDiffusionUrl: 'http://127.0.0.1:7860',
      });
      expect(result.success).toBe(true);
    });

    it('should validate Ollama URL', () => {
      const result = providerPathsSchema.safeParse({
        ollamaUrl: 'http://localhost:11434',
      });
      expect(result.success).toBe(true);
    });

    it('should reject invalid URL format', () => {
      const result = providerPathsSchema.safeParse({
        stableDiffusionUrl: 'not-a-valid-url',
      });
      expect(result.success).toBe(false);
    });

    it('should allow empty paths', () => {
      const result = providerPathsSchema.safeParse({
        ffmpegPath: '',
        ffprobePath: '',
      });
      expect(result.success).toBe(true);
    });
  });
});
