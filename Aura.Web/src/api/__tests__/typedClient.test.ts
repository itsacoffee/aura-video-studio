/**
 * Unit tests for TypedApiClient
 */

import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TypedApiClient, ApiError } from '../typedClient';

describe('TypedApiClient', () => {
  let client: TypedApiClient;
  let mockAxios: MockAdapter;

  beforeEach(() => {
    client = new TypedApiClient({
      baseURL: 'http://test-api.local',
      timeout: 5000,
      retryAttempts: 3,
      retryDelay: 100,
    });

    // Mock localStorage for circuit breaker persistence
    const localStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
      clear: vi.fn(),
      length: 0,
      key: vi.fn(),
    };
    vi.stubGlobal('localStorage', localStorageMock);

    // Mock crypto.randomUUID for correlation IDs
    vi.stubGlobal('crypto', {
      randomUUID: () => 'test-correlation-id',
    });

    mockAxios = new MockAdapter(
      (client as unknown as { axiosInstance: typeof axios }).axiosInstance
    );
  });

  afterEach(() => {
    mockAxios.reset();
    vi.unstubAllGlobals();
  });

  describe('GET requests', () => {
    it('should successfully fetch data', async () => {
      const responseData = { id: '1', name: 'Test' };
      mockAxios.onGet('/test').reply(200, responseData);

      const result = await client.get<typeof responseData>('/test');

      expect(result).toEqual(responseData);
    });

    it('should include correlation ID in requests', async () => {
      mockAxios.onGet('/test').reply((config) => {
        expect(config.headers?.['X-Correlation-ID']).toBe('test-correlation-id');
        return [200, { success: true }];
      });

      await client.get('/test');
    });

    it('should not retry 4xx errors', async () => {
      let attemptCount = 0;
      mockAxios.onGet('/test').reply(() => {
        attemptCount++;
        return [404, { error: 'Not Found' }];
      });

      try {
        await client.get('/test');
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        expect((error as ApiError).status).toBe(404);
        expect(attemptCount).toBe(1); // Should not retry
      }
    });

    it('should throw ApiError with proper details', async () => {
      mockAxios.onGet('/test').reply(400, {
        code: 'VALIDATION_ERROR',
        detail: 'Invalid request',
      });

      try {
        await client.get('/test');
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        const apiError = error as ApiError;
        expect(apiError.status).toBe(400);
        expect(apiError.code).toBe('VALIDATION_ERROR');
        expect(apiError.details).toBe('Invalid request');
      }
    });
  });

  describe('POST requests', () => {
    it('should successfully post data', async () => {
      const requestData = { name: 'New Item' };
      const responseData = { id: '123', ...requestData };

      mockAxios.onPost('/items', requestData).reply(201, responseData);

      const result = await client.post<typeof responseData>('/items', requestData);

      expect(result).toEqual(responseData);
    });

    it('should handle empty response', async () => {
      mockAxios.onPost('/items').reply(204);

      const result = await client.post('/items', { name: 'Test' });

      expect(result).toBeUndefined();
    });
  });

  describe('PUT requests', () => {
    it('should successfully update data', async () => {
      const requestData = { name: 'Updated Item' };
      const responseData = { id: '123', ...requestData };

      mockAxios.onPut('/items/123', requestData).reply(200, responseData);

      const result = await client.put<typeof responseData>('/items/123', requestData);

      expect(result).toEqual(responseData);
    });
  });

  describe('DELETE requests', () => {
    it('should successfully delete data', async () => {
      mockAxios.onDelete('/items/123').reply(204);

      const result = await client.delete('/items/123');

      expect(result).toBeUndefined();
    });
  });

  describe('Error Handling', () => {
    it('should convert axios errors to ApiError', async () => {
      mockAxios.onGet('/test').reply(500, { error: 'Server Error' });

      try {
        await client.get('/test');
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        expect((error as ApiError).status).toBe(500);
      }
    });
  });

  describe('Request Configuration', () => {
    it('should support custom headers', async () => {
      mockAxios.onGet('/test').reply((config) => {
        expect(config.headers?.['X-Custom-Header']).toBe('custom-value');
        return [200, { success: true }];
      });

      await client.get('/test', {
        headers: { 'X-Custom-Header': 'custom-value' },
      });
    });

    it('should support query parameters', async () => {
      mockAxios.onGet('/test').reply((config) => {
        expect(config.params).toEqual({ page: 1, limit: 10 });
        return [200, { data: [] }];
      });

      await client.get('/test', {
        params: { page: 1, limit: 10 },
      });
    });
  });
});
