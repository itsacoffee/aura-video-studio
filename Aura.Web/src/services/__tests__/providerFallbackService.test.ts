/**
 * Provider Fallback Service Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { providerFallbackService, ProviderConfig } from '../providerFallbackService';

describe('ProviderFallbackService', () => {
  beforeEach(() => {
    providerFallbackService.clearHealthCache();
  });

  describe('registerFallbackChain', () => {
    it('should register providers in priority order', () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 1,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 3,
        },
        {
          name: 'Provider C',
          type: 'llm',
          isAvailable: async () => true,
          priority: 2,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);

      const currentProvider = providerFallbackService.getCurrentProvider('llm');
      expect(currentProvider?.name).toBe('Provider B');
    });

    it('should handle empty provider list', () => {
      providerFallbackService.registerFallbackChain('llm', []);
      const currentProvider = providerFallbackService.getCurrentProvider('llm');
      expect(currentProvider).toBeNull();
    });
  });

  describe('getCurrentProvider', () => {
    it('should return null for unregistered type', () => {
      const currentProvider = providerFallbackService.getCurrentProvider('tts');
      expect(currentProvider).toBeNull();
    });

    it('should return first provider initially', () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);
      const currentProvider = providerFallbackService.getCurrentProvider('llm');
      expect(currentProvider?.name).toBe('Provider A');
    });
  });

  describe('checkProviderHealth', () => {
    it('should return true for healthy provider', async () => {
      const provider: ProviderConfig = {
        name: 'Healthy Provider',
        type: 'llm',
        isAvailable: async () => true,
        priority: 10,
      };

      const isHealthy = await providerFallbackService.checkProviderHealth(provider);
      expect(isHealthy).toBe(true);
    });

    it('should return false for unhealthy provider', async () => {
      const provider: ProviderConfig = {
        name: 'Unhealthy Provider',
        type: 'llm',
        isAvailable: async () => false,
        priority: 10,
      };

      const isHealthy = await providerFallbackService.checkProviderHealth(provider);
      expect(isHealthy).toBe(false);
    });

    it('should handle provider check errors', async () => {
      const provider: ProviderConfig = {
        name: 'Error Provider',
        type: 'llm',
        isAvailable: async () => {
          throw new Error('Connection failed');
        },
        priority: 10,
      };

      const isHealthy = await providerFallbackService.checkProviderHealth(provider);
      expect(isHealthy).toBe(false);
    });

    it('should cache health check results', async () => {
      let callCount = 0;
      const provider: ProviderConfig = {
        name: 'Cached Provider',
        type: 'llm',
        isAvailable: async () => {
          callCount++;
          return true;
        },
        priority: 10,
      };

      await providerFallbackService.checkProviderHealth(provider);
      await providerFallbackService.checkProviderHealth(provider);
      await providerFallbackService.checkProviderHealth(provider);

      expect(callCount).toBe(1);
    });
  });

  describe('fallbackToNextProvider', () => {
    it('should fallback to next healthy provider', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => false,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);
      const nextProvider = await providerFallbackService.fallbackToNextProvider('llm');

      expect(nextProvider?.name).toBe('Provider B');
    });

    it('should return null if all providers are unhealthy', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => false,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => false,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);
      const nextProvider = await providerFallbackService.fallbackToNextProvider('llm');

      expect(nextProvider).toBeNull();
    });

    it('should return null for unregistered type', async () => {
      const nextProvider = await providerFallbackService.fallbackToNextProvider('tts');
      expect(nextProvider).toBeNull();
    });
  });

  describe('executeWithFallback', () => {
    it('should execute operation successfully with first provider', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);

      const operation = vi.fn(async (provider: ProviderConfig) => {
        return `Success with ${provider.name}`;
      });

      const result = await providerFallbackService.executeWithFallback('llm', operation);

      expect(result).toBe('Success with Provider A');
      expect(operation).toHaveBeenCalledTimes(1);
    });

    it('should fallback on operation failure', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);

      let callCount = 0;
      const operation = vi.fn(async (provider: ProviderConfig) => {
        callCount++;
        if (provider.name === 'Provider A') {
          throw new Error('Provider A failed');
        }
        return `Success with ${provider.name}`;
      });

      const result = await providerFallbackService.executeWithFallback('llm', operation);

      expect(result).toBe('Success with Provider B');
      expect(callCount).toBe(2);
    });

    it('should throw error if all providers fail', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);

      const operation = vi.fn(async () => {
        throw new Error('Operation failed');
      });

      await expect(
        providerFallbackService.executeWithFallback('llm', operation, 2)
      ).rejects.toThrow('All providers failed');
    });
  });

  describe('resetFallbackChain', () => {
    it('should reset chain to first provider', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => false,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => true,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);
      await providerFallbackService.fallbackToNextProvider('llm');

      let currentProvider = providerFallbackService.getCurrentProvider('llm');
      expect(currentProvider?.name).toBe('Provider B');

      providerFallbackService.resetFallbackChain('llm');

      currentProvider = providerFallbackService.getCurrentProvider('llm');
      expect(currentProvider?.name).toBe('Provider A');
    });
  });

  describe('getProviderHealthStatus', () => {
    it('should return health status for all providers', async () => {
      const providers: ProviderConfig[] = [
        {
          name: 'Provider A',
          type: 'llm',
          isAvailable: async () => true,
          priority: 10,
        },
        {
          name: 'Provider B',
          type: 'llm',
          isAvailable: async () => false,
          priority: 5,
        },
      ];

      providerFallbackService.registerFallbackChain('llm', providers);

      const healthStatus = await providerFallbackService.getProviderHealthStatus();

      expect(healthStatus).toHaveLength(2);
      expect(healthStatus[0]).toMatchObject({
        type: 'llm',
        provider: 'Provider A',
        isHealthy: true,
        priority: 10,
      });
      expect(healthStatus[1]).toMatchObject({
        type: 'llm',
        provider: 'Provider B',
        isHealthy: false,
        priority: 5,
      });
    });
  });
});
