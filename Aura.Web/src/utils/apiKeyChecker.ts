/**
 * Utility for checking if required API keys are configured
 */

import type { ApiKeysSettings } from '../types/settings';

export interface ApiKeyCheckResult {
  hasAnyLlmKey: boolean;
  hasAnyTtsKey: boolean;
  hasAnyImageKey: boolean;
  configuredLlmProviders: string[];
  configuredTtsProviders: string[];
  configuredImageProviders: string[];
  missingLlmProviders: string[];
}

/**
 * Check which API keys are configured
 */
export function checkApiKeys(apiKeys: ApiKeysSettings): ApiKeyCheckResult {
  const llmProviders = ['openAI', 'anthropic', 'google'] as const;
  const ttsProviders = ['elevenLabs'] as const;
  const imageProviders = ['stabilityAI', 'pexels', 'pixabay', 'unsplash'] as const;

  const configuredLlmProviders: string[] = [];
  const configuredTtsProviders: string[] = [];
  const configuredImageProviders: string[] = [];

  // Check LLM providers
  llmProviders.forEach((provider) => {
    const key = apiKeys[provider];
    if (key && key.trim().length > 0) {
      configuredLlmProviders.push(getProviderDisplayName(provider));
    }
  });

  // Check TTS providers
  ttsProviders.forEach((provider) => {
    const key = apiKeys[provider];
    if (key && key.trim().length > 0) {
      configuredTtsProviders.push(getProviderDisplayName(provider));
    }
  });

  // Check image providers
  imageProviders.forEach((provider) => {
    const key = apiKeys[provider];
    if (key && key.trim().length > 0) {
      configuredImageProviders.push(getProviderDisplayName(provider));
    }
  });

  // Determine missing LLM providers
  const missingLlmProviders = llmProviders
    .filter((provider) => {
      const key = apiKeys[provider];
      return !key || key.trim().length === 0;
    })
    .map(getProviderDisplayName);

  return {
    hasAnyLlmKey: configuredLlmProviders.length > 0,
    hasAnyTtsKey: configuredTtsProviders.length > 0,
    hasAnyImageKey: configuredImageProviders.length > 0,
    configuredLlmProviders,
    configuredTtsProviders,
    configuredImageProviders,
    missingLlmProviders,
  };
}

/**
 * Get display name for API provider
 */
function getProviderDisplayName(provider: string): string {
  const displayNames: Record<string, string> = {
    openAI: 'OpenAI',
    anthropic: 'Anthropic (Claude)',
    google: 'Google (Gemini)',
    elevenLabs: 'ElevenLabs',
    stabilityAI: 'Stability AI',
    pexels: 'Pexels',
    pixabay: 'Pixabay',
    unsplash: 'Unsplash',
  };
  return displayNames[provider] || provider;
}

/**
 * Get friendly message for missing API keys
 */
export function getMissingApiKeyMessage(
  checkResult: ApiKeyCheckResult,
  featureName: string
): string {
  if (checkResult.hasAnyLlmKey) {
    return '';
  }

  const providers = ['OpenAI', 'Anthropic (Claude)', 'Google (Gemini)'];
  return `${featureName} requires an API key from at least one LLM provider (${providers.join(', ')}). Configure your API keys in Settings to use this feature.`;
}
