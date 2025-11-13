/**
 * Provider Fallback Service
 * Manages automatic provider switching and fallback chains
 */

import { errorReportingService } from './errorReportingService';
import { loggingService } from './loggingService';

export interface ProviderConfig {
  name: string;
  type: 'llm' | 'tts' | 'image' | 'video';
  isAvailable: () => Promise<boolean>;
  priority: number;
  requiresApiKey?: boolean;
  isOffline?: boolean;
}

export interface FallbackChain {
  type: 'llm' | 'tts' | 'image' | 'video';
  providers: ProviderConfig[];
  currentIndex: number;
}

class ProviderFallbackService {
  private fallbackChains: Map<string, FallbackChain> = new Map();
  private providerHealthCache: Map<string, { isHealthy: boolean; lastCheck: number }> = new Map();
  private readonly healthCacheTTL = 60000;

  /**
   * Register a provider fallback chain
   */
  public registerFallbackChain(
    type: 'llm' | 'tts' | 'image' | 'video',
    providers: ProviderConfig[]
  ): void {
    const sortedProviders = [...providers].sort((a, b) => b.priority - a.priority);

    this.fallbackChains.set(type, {
      type,
      providers: sortedProviders,
      currentIndex: 0,
    });

    loggingService.info(
      `Provider fallback chain registered for ${type}`,
      'providerFallback',
      'registerFallbackChain',
      {
        type,
        providerCount: sortedProviders.length,
        providers: sortedProviders.map((p) => p.name),
      }
    );
  }

  /**
   * Get the current active provider for a type
   */
  public getCurrentProvider(type: 'llm' | 'tts' | 'image' | 'video'): ProviderConfig | null {
    const chain = this.fallbackChains.get(type);
    if (!chain || chain.providers.length === 0) {
      return null;
    }

    return chain.providers[chain.currentIndex] || null;
  }

  /**
   * Get all providers for a type
   */
  public getProviders(type: 'llm' | 'tts' | 'image' | 'video'): ProviderConfig[] {
    const chain = this.fallbackChains.get(type);
    return chain ? [...chain.providers] : [];
  }

  /**
   * Check if a provider is healthy
   */
  public async checkProviderHealth(provider: ProviderConfig): Promise<boolean> {
    const cacheKey = `${provider.type}-${provider.name}`;
    const cached = this.providerHealthCache.get(cacheKey);

    if (cached && Date.now() - cached.lastCheck < this.healthCacheTTL) {
      return cached.isHealthy;
    }

    try {
      const isHealthy = await provider.isAvailable();

      this.providerHealthCache.set(cacheKey, {
        isHealthy,
        lastCheck: Date.now(),
      });

      return isHealthy;
    } catch (error) {
      loggingService.warn(
        `Provider health check failed for ${provider.name}`,
        'providerFallback',
        'checkProviderHealth',
        { provider: provider.name, error }
      );

      this.providerHealthCache.set(cacheKey, {
        isHealthy: false,
        lastCheck: Date.now(),
      });

      return false;
    }
  }

  /**
   * Fallback to next available provider
   */
  public async fallbackToNextProvider(
    type: 'llm' | 'tts' | 'image' | 'video'
  ): Promise<ProviderConfig | null> {
    const chain = this.fallbackChains.get(type);
    if (!chain) {
      loggingService.warn(
        `No fallback chain registered for ${type}`,
        'providerFallback',
        'fallbackToNextProvider'
      );
      return null;
    }

    const startIndex = chain.currentIndex;
    let attempts = 0;
    const maxAttempts = chain.providers.length;

    while (attempts < maxAttempts) {
      chain.currentIndex = (chain.currentIndex + 1) % chain.providers.length;
      attempts++;

      if (chain.currentIndex === startIndex && attempts > 1) {
        loggingService.error(
          'All providers exhausted in fallback chain',
          undefined,
          'providerFallback',
          'fallbackToNextProvider',
          { type, attempts }
        );

        errorReportingService.warning(
          'Provider Unavailable',
          `All ${type} providers are currently unavailable. Please check your configuration.`
        );

        return null;
      }

      const provider = chain.providers[chain.currentIndex];
      const isHealthy = await this.checkProviderHealth(provider);

      if (isHealthy) {
        loggingService.info(
          `Fallback successful to ${provider.name}`,
          'providerFallback',
          'fallbackToNextProvider',
          {
            type,
            provider: provider.name,
            attempts,
          }
        );

        errorReportingService.info(
          'Provider Switched',
          `Switched to ${provider.name} provider for ${type} operations.`
        );

        return provider;
      }

      loggingService.warn(
        `Provider ${provider.name} is not healthy, trying next...`,
        'providerFallback',
        'fallbackToNextProvider',
        { type, provider: provider.name }
      );
    }

    return null;
  }

  /**
   * Execute an operation with automatic fallback on failure
   */
  public async executeWithFallback<T>(
    type: 'llm' | 'tts' | 'image' | 'video',
    operation: (provider: ProviderConfig) => Promise<T>,
    maxAttempts: number = 3
  ): Promise<T> {
    const chain = this.fallbackChains.get(type);
    if (!chain || chain.providers.length === 0) {
      throw new Error(`No providers registered for type: ${type}`);
    }

    let lastError: Error | null = null;
    let attempts = 0;

    while (attempts < maxAttempts) {
      const provider = this.getCurrentProvider(type);
      if (!provider) {
        throw new Error(`No available providers for type: ${type}`);
      }

      try {
        loggingService.info(
          `Executing operation with ${provider.name}`,
          'providerFallback',
          'executeWithFallback',
          { type, provider: provider.name, attempt: attempts + 1 }
        );

        const result = await operation(provider);

        if (attempts > 0) {
          errorReportingService.info(
            'Operation Successful',
            `Operation completed successfully after ${attempts + 1} attempts.`
          );
        }

        return result;
      } catch (error) {
        lastError = error instanceof Error ? error : new Error(String(error));

        loggingService.warn(
          `Operation failed with ${provider.name}, attempting fallback`,
          'providerFallback',
          'executeWithFallback',
          {
            type,
            provider: provider.name,
            attempt: attempts + 1,
            error: lastError.message,
          }
        );

        const nextProvider = await this.fallbackToNextProvider(type);
        if (!nextProvider) {
          break;
        }

        attempts++;
      }
    }

    const errorMessage = lastError
      ? lastError.message
      : 'All providers failed without specific error';

    loggingService.error(
      'All providers failed',
      lastError || new Error(errorMessage),
      'providerFallback',
      'executeWithFallback',
      { type, attempts }
    );

    errorReportingService.error(
      'Operation Failed',
      `All ${type} providers failed. Please check your configuration and try again.`,
      lastError || undefined
    );

    throw new Error(`All providers failed for ${type}: ${errorMessage}`);
  }

  /**
   * Reset fallback chain to first provider
   */
  public resetFallbackChain(type: 'llm' | 'tts' | 'image' | 'video'): void {
    const chain = this.fallbackChains.get(type);
    if (chain) {
      chain.currentIndex = 0;
      loggingService.info(
        `Fallback chain reset for ${type}`,
        'providerFallback',
        'resetFallbackChain'
      );
    }
  }

  /**
   * Clear provider health cache
   */
  public clearHealthCache(): void {
    this.providerHealthCache.clear();
    loggingService.info('Provider health cache cleared', 'providerFallback', 'clearHealthCache');
  }

  /**
   * Get provider health status for all providers
   */
  public async getProviderHealthStatus(): Promise<
    Array<{
      type: string;
      provider: string;
      isHealthy: boolean;
      priority: number;
    }>
  > {
    const results: Array<{
      type: string;
      provider: string;
      isHealthy: boolean;
      priority: number;
    }> = [];

    for (const [type, chain] of this.fallbackChains.entries()) {
      for (const provider of chain.providers) {
        const isHealthy = await this.checkProviderHealth(provider);
        results.push({
          type,
          provider: provider.name,
          isHealthy,
          priority: provider.priority,
        });
      }
    }

    return results;
  }
}

export const providerFallbackService = new ProviderFallbackService();
