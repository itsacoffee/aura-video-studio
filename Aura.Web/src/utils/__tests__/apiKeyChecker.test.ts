import { describe, expect, it } from 'vitest';
import type { ApiKeysSettings } from '../../types/settings';
import { checkApiKeys, getMissingApiKeyMessage } from '../apiKeyChecker';

describe('apiKeyChecker', () => {
  describe('checkApiKeys', () => {
    it('should detect when no API keys are configured', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: '',
        anthropic: '',
        stabilityAI: '',
        elevenLabs: '',
        pexels: '',
        pixabay: '',
        unsplash: '',
        google: '',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyLlmKey).toBe(false);
      expect(result.hasAnyTtsKey).toBe(false);
      expect(result.hasAnyImageKey).toBe(false);
      expect(result.configuredLlmProviders).toHaveLength(0);
      expect(result.missingLlmProviders).toHaveLength(3);
    });

    it('should detect configured OpenAI API key', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: 'sk-test123',
        anthropic: '',
        stabilityAI: '',
        elevenLabs: '',
        pexels: '',
        pixabay: '',
        unsplash: '',
        google: '',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyLlmKey).toBe(true);
      expect(result.configuredLlmProviders).toContain('OpenAI');
      expect(result.configuredLlmProviders).toHaveLength(1);
      expect(result.missingLlmProviders).toHaveLength(2);
    });

    it('should detect multiple configured LLM keys', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: 'sk-test123',
        anthropic: 'sk-ant-test',
        stabilityAI: '',
        elevenLabs: '',
        pexels: '',
        pixabay: '',
        unsplash: '',
        google: 'google-key',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyLlmKey).toBe(true);
      expect(result.configuredLlmProviders).toHaveLength(3);
      expect(result.configuredLlmProviders).toContain('OpenAI');
      expect(result.configuredLlmProviders).toContain('Anthropic (Claude)');
      expect(result.configuredLlmProviders).toContain('Google (Gemini)');
      expect(result.missingLlmProviders).toHaveLength(0);
    });

    it('should detect configured TTS keys', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: '',
        anthropic: '',
        stabilityAI: '',
        elevenLabs: 'elevenlabs-key',
        pexels: '',
        pixabay: '',
        unsplash: '',
        google: '',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyTtsKey).toBe(true);
      expect(result.configuredTtsProviders).toContain('ElevenLabs');
    });

    it('should detect configured image provider keys', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: '',
        anthropic: '',
        stabilityAI: 'stability-key',
        elevenLabs: '',
        pexels: 'pexels-key',
        pixabay: '',
        unsplash: 'unsplash-key',
        google: '',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyImageKey).toBe(true);
      expect(result.configuredImageProviders).toHaveLength(3);
      expect(result.configuredImageProviders).toContain('Stability AI');
      expect(result.configuredImageProviders).toContain('Pexels');
      expect(result.configuredImageProviders).toContain('Unsplash');
    });

    it('should ignore whitespace-only keys', () => {
      const apiKeys: ApiKeysSettings = {
        openAI: '   ',
        anthropic: '\t\n',
        stabilityAI: '',
        elevenLabs: '',
        pexels: '',
        pixabay: '',
        unsplash: '',
        google: '',
        azure: '',
      };

      const result = checkApiKeys(apiKeys);

      expect(result.hasAnyLlmKey).toBe(false);
      expect(result.configuredLlmProviders).toHaveLength(0);
    });
  });

  describe('getMissingApiKeyMessage', () => {
    it('should return empty string when LLM keys are present', () => {
      const checkResult = {
        hasAnyLlmKey: true,
        hasAnyTtsKey: false,
        hasAnyImageKey: false,
        configuredLlmProviders: ['OpenAI'],
        configuredTtsProviders: [],
        configuredImageProviders: [],
        missingLlmProviders: [],
      };

      const message = getMissingApiKeyMessage(checkResult, 'Test Feature');

      expect(message).toBe('');
    });

    it('should return helpful message when no LLM keys are configured', () => {
      const checkResult = {
        hasAnyLlmKey: false,
        hasAnyTtsKey: false,
        hasAnyImageKey: false,
        configuredLlmProviders: [],
        configuredTtsProviders: [],
        configuredImageProviders: [],
        missingLlmProviders: ['OpenAI', 'Anthropic (Claude)', 'Google (Gemini)'],
      };

      const message = getMissingApiKeyMessage(checkResult, 'Content Planning');

      expect(message).toContain('Content Planning');
      expect(message).toContain('API key');
      expect(message).toContain('OpenAI');
      expect(message).toContain('Anthropic (Claude)');
      expect(message).toContain('Google (Gemini)');
      expect(message).toContain('Settings');
    });
  });
});
