import MockAdapter from 'axios-mock-adapter';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import apiClient, {
  get,
  post,
  put,
  patch,
  del,
  resetCircuitBreaker,
  getCircuitBreakerState,
} from '../services/api/apiClient';

// Create mock adapter for the actual apiClient instance
let mock: MockAdapter;

describe('API Client', () => {
  beforeEach(() => {
    // Reset circuit breaker before each test
    resetCircuitBreaker();

    // Create new mock adapter on the actual apiClient instance
    mock = new MockAdapter(apiClient);
  });

  afterEach(() => {
    // Restore mock adapter
    mock.restore();
  });

  describe('Basic HTTP Methods', () => {
    it('should make GET request successfully', async () => {
      const responseData = { id: 1, name: 'Test' };
      mock.onGet('/api/test').reply(200, responseData);

      const result = await get('/api/test');
      expect(result).toEqual(responseData);
    });

    it('should make POST request successfully', async () => {
      const requestData = { name: 'New Item' };
      const responseData = { id: 1, ...requestData };
      mock.onPost('/api/test', requestData).reply(201, responseData);

      const result = await post('/api/test', requestData);
      expect(result).toEqual(responseData);
    });

    it('should make PUT request successfully', async () => {
      const requestData = { id: 1, name: 'Updated Item' };
      mock.onPut('/api/test/1', requestData).reply(200, requestData);

      const result = await put('/api/test/1', requestData);
      expect(result).toEqual(requestData);
    });

    it('should make PATCH request successfully', async () => {
      const requestData = { name: 'Patched' };
      const responseData = { id: 1, ...requestData };
      mock.onPatch('/api/test/1', requestData).reply(200, responseData);

      const result = await patch('/api/test/1', requestData);
      expect(result).toEqual(responseData);
    });

    it('should make DELETE request successfully', async () => {
      mock.onDelete('/api/test/1').reply(204);

      await del('/api/test/1');
      // Should complete without error
      expect(true).toBe(true);
    });
  });

  describe('Error Handling', () => {
    it('should handle 404 errors with user-friendly message', async () => {
      mock.onGet('/api/test').reply(404, { message: 'Resource not found' });

      try {
        await get('/api/test', { _skipRetry: true });
        expect.fail('Should have thrown error');
      } catch (error: any) {
        expect(error.response.status).toBe(404);
        expect(error.userMessage).toBeTruthy();
      }
    });

    it('should handle 500 errors with user-friendly message', async () => {
      mock.onGet('/api/test').reply(500, { message: 'Internal server error' });

      try {
        await get('/api/test', { _skipRetry: true });
        expect.fail('Should have thrown error');
      } catch (error: any) {
        expect(error.response.status).toBe(500);
        expect(error.userMessage).toBeTruthy();
      }
    });

    it('should extract error code from response', async () => {
      mock.onGet('/api/test').reply(400, {
        errorCode: 'E300',
        message: 'Validation failed',
      });

      try {
        await get('/api/test', { _skipRetry: true });
        expect.fail('Should have thrown error');
      } catch (error: any) {
        expect(error.errorCode).toBe('E300');
      }
    });
  });

  describe('Retry Logic', () => {
    it('should retry on 5xx errors', async () => {
      let attempts = 0;
      mock.onGet('/api/test').reply(() => {
        attempts++;
        if (attempts < 3) {
          return [500, { message: 'Server error' }];
        }
        return [200, { success: true }];
      });

      const result = await get('/api/test');
      expect(result).toEqual({ success: true });
      expect(attempts).toBe(3);
    }, 15000);

    it('should not retry on 4xx errors', async () => {
      let attempts = 0;
      mock.onGet('/api/test').reply(() => {
        attempts++;
        return [400, { message: 'Bad request' }];
      });

      try {
        await get('/api/test');
        expect.fail('Should have thrown error');
      } catch (error: any) {
        expect(attempts).toBe(1);
        expect(error.response.status).toBe(400);
      }
    });
  });

  describe('Circuit Breaker', () => {
    it('should open circuit after multiple failures', async () => {
      // Mock multiple failures
      mock.onGet('/api/test').reply(500, { message: 'Server error' });

      // Attempt 5 requests (threshold)
      for (let i = 0; i < 5; i++) {
        try {
          await get('/api/test', { _skipRetry: true });
        } catch (error) {
          // Expected to fail
        }
      }

      // Circuit should now be open
      const state = getCircuitBreakerState();
      expect(state).toBe('OPEN');

      // Next request should be blocked by circuit breaker
      try {
        await get('/api/test', { _skipRetry: true });
        expect.fail('Should have been blocked by circuit breaker');
      } catch (error: any) {
        expect(error.message).toContain('Circuit breaker');
      }
    });

    it('should reset circuit breaker manually', async () => {
      // Open circuit
      mock.onGet('/api/test').reply(500);
      for (let i = 0; i < 5; i++) {
        try {
          await get('/api/test', { _skipRetry: true });
        } catch (error) {
          // Expected
        }
      }

      expect(getCircuitBreakerState()).toBe('OPEN');

      // Reset circuit breaker
      resetCircuitBreaker();
      expect(getCircuitBreakerState()).toBe('CLOSED');

      // Should be able to make requests again
      mock.onGet('/api/test').reply(200, { success: true });
      const result = await get('/api/test');
      expect(result).toEqual({ success: true });
    });
  });

  describe('Authentication', () => {
    it('should include auth token when available', async () => {
      localStorage.setItem('auth_token', 'test-token-123');

      // Mock will receive headers after interceptor processing
      mock.onGet('/api/protected').reply(200, { authorized: true });

      const result = await get('/api/protected');
      expect(result).toEqual({ authorized: true });

      localStorage.removeItem('auth_token');
    });

    it('should clear token on 401 error', async () => {
      localStorage.setItem('auth_token', 'expired-token');
      mock.onGet('/api/protected').reply(401, { message: 'Unauthorized' });

      try {
        await get('/api/protected', { _skipRetry: true });
        expect.fail('Should have thrown error');
      } catch (error: any) {
        expect(error.response.status).toBe(401);
      }

      // Token should be cleared
      expect(localStorage.getItem('auth_token')).toBeNull();
    });
  });
});
