/**
 * Provider Configuration API Client
 * Handles unified provider configuration (OpenAI, Ollama, Stable Diffusion, etc.)
 * across the stack. This is the single source of truth for provider settings.
 */

import { apiUrl } from '../../config/api';

/**
 * OpenAI configuration
 */
export interface OpenAiConfig {
  apiKey?: string | null;
  endpoint?: string | null;
}

/**
 * Ollama configuration
 */
export interface OllamaConfig {
  url?: string | null;
  model?: string | null;
}

/**
 * Stable Diffusion configuration
 */
export interface StableDiffusionConfig {
  url?: string | null;
}

/**
 * Anthropic configuration
 */
export interface AnthropicConfig {
  apiKey?: string | null;
}

/**
 * Gemini configuration
 */
export interface GeminiConfig {
  apiKey?: string | null;
}

/**
 * ElevenLabs configuration
 */
export interface ElevenLabsConfig {
  apiKey?: string | null;
}

/**
 * Full provider configuration response
 */
export interface ProviderConfiguration {
  openAi: OpenAiConfig;
  ollama: OllamaConfig;
  stableDiffusion: StableDiffusionConfig;
  anthropic: AnthropicConfig;
  gemini: GeminiConfig;
  elevenLabs: ElevenLabsConfig;
}

/**
 * Provider configuration update request (non-secret fields)
 */
export interface ProviderConfigurationUpdate {
  openAi?: {
    endpoint?: string;
  };
  ollama?: {
    url?: string;
    model?: string;
  };
  stableDiffusion?: {
    url?: string;
  };
}

/**
 * Provider secrets update request (API keys)
 */
export interface ProviderSecretsUpdate {
  openAiApiKey?: string;
  anthropicApiKey?: string;
  geminiApiKey?: string;
  elevenLabsApiKey?: string;
}

/**
 * Get current provider configuration (without secrets)
 */
export async function getProviderConfiguration(): Promise<ProviderConfiguration> {
  const response = await fetch(apiUrl('/api/ProviderConfiguration/config'));

  if (!response.ok) {
    throw new Error(
      `Failed to load provider configuration: ${response.status} ${response.statusText}`
    );
  }

  return (await response.json()) as ProviderConfiguration;
}

/**
 * Update provider configuration (non-secret fields like URLs and endpoints)
 */
export async function updateProviderConfiguration(
  payload: ProviderConfigurationUpdate
): Promise<void> {
  const response = await fetch(apiUrl('/api/ProviderConfiguration/config'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(
      `Failed to update provider configuration: ${response.status} ${response.statusText}\n${errorText}`
    );
  }
}

/**
 * Update provider secrets (API keys)
 * This endpoint handles sensitive data securely
 */
export async function updateProviderSecrets(payload: ProviderSecretsUpdate): Promise<void> {
  const response = await fetch(apiUrl('/api/ProviderConfiguration/config/secrets'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(
      `Failed to update provider secrets: ${response.status} ${response.statusText}\n${errorText}`
    );
  }
}
