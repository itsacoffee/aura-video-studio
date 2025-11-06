import { describe, it, expect, vi, beforeEach } from 'vitest';
import { validateApiKeyThunk } from '../onboarding';
import type { OnboardingAction } from '../onboarding';

// Mock fetch
global.fetch = vi.fn();

describe('API Key Validation', () => {
  let dispatchedActions: OnboardingAction[];
  let mockDispatch: React.Dispatch<OnboardingAction>;

  beforeEach(() => {
    dispatchedActions = [];
    mockDispatch = vi.fn((action: OnboardingAction) => {
      dispatchedActions.push(action);
    }) as React.Dispatch<OnboardingAction>;
    vi.clearAllMocks();
  });

  describe('OpenAI API Key Format Validation', () => {
    it('should accept legacy OpenAI key with sk- prefix', async () => {
      await validateApiKeyThunk('openai', 'sk-1234567890abcdefghij1234567890ab', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should not dispatch API_KEY_INVALID for format validation
      const invalidAction = dispatchedActions.find((a) => a.type === 'API_KEY_INVALID');
      if (invalidAction && invalidAction.type === 'API_KEY_INVALID') {
        // If there is an invalid action, it should not be about format
        expect(invalidAction.payload.error).not.toContain('start with');
      }
    });

    it('should accept project-scoped OpenAI key with sk-proj- prefix', async () => {
      await validateApiKeyThunk('openai', 'sk-proj-1234567890abcdefghij1234567890ab', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should not dispatch API_KEY_INVALID for format validation
      const invalidAction = dispatchedActions.find((a) => a.type === 'API_KEY_INVALID');
      if (invalidAction && invalidAction.type === 'API_KEY_INVALID') {
        // If there is an invalid action, it should not be about format
        expect(invalidAction.payload.error).not.toContain('start with');
      }
    });

    it('should reject OpenAI key that does not start with sk-', async () => {
      await validateApiKeyThunk('openai', 'invalid-key-format-12345678', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should dispatch API_KEY_INVALID
      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'openai',
          error: 'OpenAI API keys start with "sk-" or "sk-proj-"',
        },
      });
    });

    it('should reject OpenAI key that is too short', async () => {
      await validateApiKeyThunk('openai', 'sk-short', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should dispatch API_KEY_INVALID
      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'openai',
          error: 'OpenAI API keys must be at least 20 characters',
        },
      });
    });

    it('should trim whitespace before validating OpenAI key', async () => {
      await validateApiKeyThunk('openai', '  sk-1234567890abcdefghij1234567890ab  ', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should not dispatch API_KEY_INVALID for format validation
      const invalidAction = dispatchedActions.find((a) => a.type === 'API_KEY_INVALID');
      if (invalidAction && invalidAction.type === 'API_KEY_INVALID') {
        // If there is an invalid action, it should not be about format
        expect(invalidAction.payload.error).not.toContain('start with');
      }
    });

    it('should reject empty OpenAI key', async () => {
      await validateApiKeyThunk('openai', '', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should dispatch API_KEY_INVALID
      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'openai',
          error: 'Please enter your API key',
        },
      });
    });

    it('should reject whitespace-only OpenAI key', async () => {
      await validateApiKeyThunk('openai', '   ', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'openai',
      });

      // Should dispatch API_KEY_INVALID
      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'openai',
          error: 'Please enter your API key',
        },
      });
    });
  });

  describe('Other Provider Format Validation', () => {
    it('should accept valid Anthropic key with sk-ant- prefix', async () => {
      await validateApiKeyThunk('anthropic', 'sk-ant-1234567890abcdefghij', mockDispatch);

      // First dispatch should be START_API_KEY_VALIDATION
      expect(dispatchedActions[0]).toEqual({
        type: 'START_API_KEY_VALIDATION',
        payload: 'anthropic',
      });

      // Should not dispatch API_KEY_INVALID for format validation
      const invalidAction = dispatchedActions.find((a) => a.type === 'API_KEY_INVALID');
      if (invalidAction && invalidAction.type === 'API_KEY_INVALID') {
        // If there is an invalid action, it should not be about format
        expect(invalidAction.payload.error).not.toContain('start with');
      }
    });

    it('should reject Anthropic key without sk-ant- prefix', async () => {
      await validateApiKeyThunk('anthropic', 'sk-1234567890abcdefghij', mockDispatch);

      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'anthropic',
          error: 'Anthropic API keys start with "sk-ant-"',
        },
      });
    });

    it('should accept valid Replicate key with r8_ prefix', async () => {
      await validateApiKeyThunk('replicate', 'r8_1234567890abcdefghij', mockDispatch);

      // Should not dispatch API_KEY_INVALID for format validation
      const invalidAction = dispatchedActions.find((a) => a.type === 'API_KEY_INVALID');
      if (invalidAction && invalidAction.type === 'API_KEY_INVALID') {
        expect(invalidAction.payload.error).not.toContain('start with');
      }
    });

    it('should reject Replicate key without r8_ prefix', async () => {
      await validateApiKeyThunk('replicate', 'invalid-1234567890abcdefghij', mockDispatch);

      expect(dispatchedActions[1]).toEqual({
        type: 'API_KEY_INVALID',
        payload: {
          provider: 'replicate',
          error: 'Replicate API keys start with "r8_"',
        },
      });
    });
  });
});
