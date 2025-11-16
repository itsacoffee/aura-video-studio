/**
 * Unit tests for providerPingClient
 * Tests the provider ping API client methods added in PR 336
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import apiClient from '../apiClient';
import { providerPingClient } from '../providerPingClient';

// Mock the apiClient
vi.mock('../apiClient', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
  },
  resetCircuitBreaker: vi.fn(),
}));

describe('providerPingClient', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('pingProvider', () => {
    it('should ping a specific provider and return result', async () => {
      const mockResult = {
        attempted: true,
        success: true,
        errorCode: null,
        message: 'Connection successful',
        httpStatus: '200',
        endpoint: 'https://api.openai.com/v1/models',
        responseTimeMs: 150,
        correlationId: 'abc-123',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResult });

      const result = await providerPingClient.pingProvider('openai');

      expect(apiClient.post).toHaveBeenCalledWith(
        '/api/providers/openai/ping',
        {},
        { _skipCircuitBreaker: true }
      );
      expect(result).toEqual(mockResult);
    });

    it('should handle ping failures with error details', async () => {
      const mockResult = {
        attempted: true,
        success: false,
        errorCode: 'AUTH002_ApiKeyInvalid',
        message: 'Invalid API key',
        httpStatus: '401',
        endpoint: 'https://api.openai.com/v1/models',
        responseTimeMs: 100,
        correlationId: 'def-456',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResult });

      const result = await providerPingClient.pingProvider('openai');

      expect(result.success).toBe(false);
      expect(result.errorCode).toBe('AUTH002_ApiKeyInvalid');
      expect(result.correlationId).toBe('def-456');
    });

    it('should skip circuit breaker for ping requests', async () => {
      const mockResult = {
        attempted: true,
        success: true,
        errorCode: null,
        message: null,
        httpStatus: null,
        endpoint: null,
        responseTimeMs: null,
        correlationId: 'ghi-789',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResult });

      await providerPingClient.pingProvider('anthropic');

      const callArgs = vi.mocked(apiClient.post).mock.calls[0];
      expect(callArgs[2]).toHaveProperty('_skipCircuitBreaker', true);
    });
  });

  describe('pingAllProviders', () => {
    it('should ping all configured providers', async () => {
      const mockResult = {
        results: {
          OpenAI: {
            attempted: true,
            success: true,
            errorCode: null,
            message: 'Connected',
            httpStatus: '200',
            endpoint: 'https://api.openai.com',
            responseTimeMs: 120,
            correlationId: 'jkl-012',
          },
          Anthropic: {
            attempted: true,
            success: false,
            errorCode: 'NET001_BackendUnreachable',
            message: 'Connection timeout',
            httpStatus: null,
            endpoint: 'https://api.anthropic.com',
            responseTimeMs: null,
            correlationId: 'mno-345',
          },
        },
        timestamp: '2024-01-15T10:30:00Z',
        correlationId: 'pqr-678',
      };

      vi.mocked(apiClient.get).mockResolvedValue({ data: mockResult });

      const result = await providerPingClient.pingAllProviders();

      expect(apiClient.get).toHaveBeenCalledWith('/api/providers/ping-all', {
        _skipCircuitBreaker: true,
      });
      expect(result.results).toHaveProperty('OpenAI');
      expect(result.results).toHaveProperty('Anthropic');
      expect(result.results.OpenAI.success).toBe(true);
      expect(result.results.Anthropic.success).toBe(false);
    });
  });

  describe('validateProviderDetailed', () => {
    it('should validate provider with detailed information', async () => {
      const mockResult = {
        name: 'OpenAI',
        configured: true,
        reachable: true,
        errorCode: null,
        errorMessage: null,
        howToFix: null,
        lastValidated: '2024-01-15T10:30:00Z',
        category: 'LLM',
        tier: 'Premium',
        success: true,
        message: 'OpenAI is configured and reachable',
        correlationId: 'stu-901',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResult });

      const result = await providerPingClient.validateProviderDetailed('openai');

      expect(apiClient.post).toHaveBeenCalledWith(
        '/api/providers/openai/validate-detailed',
        {},
        { _skipCircuitBreaker: true }
      );
      expect(result.configured).toBe(true);
      expect(result.reachable).toBe(true);
      expect(result.success).toBe(true);
    });

    it('should return detailed error information for failed validation', async () => {
      const mockResult = {
        name: 'ElevenLabs',
        configured: false,
        reachable: false,
        errorCode: 'AUTH001_ApiKeyMissing',
        errorMessage: 'API key not configured',
        howToFix: [
          'Configure API key in Settings → Providers',
          'Obtain API key from ElevenLabs dashboard',
        ],
        lastValidated: null,
        category: 'TTS',
        tier: 'Premium',
        success: false,
        message: 'ElevenLabs is not configured',
        correlationId: 'vwx-234',
      };

      vi.mocked(apiClient.post).mockResolvedValue({ data: mockResult });

      const result = await providerPingClient.validateProviderDetailed('elevenlabs');

      expect(result.configured).toBe(false);
      expect(result.reachable).toBe(false);
      expect(result.errorCode).toBe('AUTH001_ApiKeyMissing');
      expect(result.howToFix).toHaveLength(2);
    });
  });
});
