/**
 * Integration tests for API Client Error Handling
 * Tests error parsing and user-friendly message generation
 */

import type { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { describe, it, expect } from 'vitest';
import { parseApiError } from '../../../utils/apiErrorParser';

describe('API Client Error Handling Integration', () => {
  describe('parseApiError - Network Errors', () => {
    it('should parse network connection failures', () => {
      const networkError: Partial<AxiosError> = {
        code: 'ERR_NETWORK',
        message: 'Network Error',
        config: {} as InternalAxiosRequestConfig,
        request: {},
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
      };

      const result = parseApiError(networkError);

      expect(result.type).toBe('network');
      expect(result.message).toContain('connect');
      expect(result.retryable).toBe(true);
    });

    it('should parse timeout errors', () => {
      const timeoutError: Partial<AxiosError> = {
        code: 'ECONNABORTED',
        message: 'timeout exceeded',
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
      };

      const result = parseApiError(timeoutError);

      expect(result.type).toBe('timeout');
      expect(result.message).toContain('too long');
      expect(result.retryable).toBe(true);
    });
  });

  describe('parseApiError - HTTP Status Codes', () => {
    it('should parse 401 Unauthorized errors', () => {
      const unauthorizedError: Partial<AxiosError> = {
        response: {
          status: 401,
          statusText: 'Unauthorized',
          data: { message: 'Invalid API key' },
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed with status code 401',
      };

      const result = parseApiError(unauthorizedError);

      expect(result.type).toBe('auth');
      expect(result.message).toContain('API key');
      expect(result.retryable).toBe(false);
    });

    it('should parse 403 Forbidden errors', () => {
      const forbiddenError: Partial<AxiosError> = {
        response: {
          status: 403,
          statusText: 'Forbidden',
          data: { message: 'Access denied' },
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed with status code 403',
      };

      const result = parseApiError(forbiddenError);

      expect(result.type).toBe('auth');
      expect(result.message).toContain('Access denied');
      expect(result.retryable).toBe(false);
    });

    it('should parse 429 Rate Limit errors with retry-after header', () => {
      const rateLimitError: Partial<AxiosError> = {
        response: {
          status: 429,
          statusText: 'Too Many Requests',
          data: { message: 'Rate limit exceeded' },
          headers: { 'retry-after': '60' },
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed with status code 429',
      };

      const result = parseApiError(rateLimitError);

      expect(result.type).toBe('rateLimit');
      expect(result.message).toContain('Rate limit');
      expect(result.message).toContain('60 seconds');
      expect(result.retryable).toBe(true);
    });

    it('should parse 500 Server errors', () => {
      const serverError: Partial<AxiosError> = {
        response: {
          status: 500,
          statusText: 'Internal Server Error',
          data: { message: 'Server error occurred' },
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed with status code 500',
      };

      const result = parseApiError(serverError);

      expect(result.type).toBe('server');
      expect(result.message).toContain('temporarily unavailable');
      expect(result.retryable).toBe(true);
    });

    it('should parse 400 Bad Request errors', () => {
      const badRequestError: Partial<AxiosError> = {
        response: {
          status: 400,
          statusText: 'Bad Request',
          data: { message: 'Invalid request format' },
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed with status code 400',
      };

      const result = parseApiError(badRequestError);

      expect(result.type).toBe('unknown');
      expect(result.message).toContain('Invalid request format');
      expect(result.retryable).toBe(false);
    });
  });

  describe('parseApiError - Edge Cases', () => {
    it('should handle undefined/null errors', () => {
      const result = parseApiError(undefined);

      expect(result.type).toBe('unknown');
      expect(result.message).toBeDefined();
      expect(result.retryable).toBe(false);
    });

    it('should handle errors without response data', () => {
      const error: Partial<AxiosError> = {
        response: {
          status: 500,
          statusText: 'Internal Server Error',
          data: undefined,
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = parseApiError(error);

      expect(result.type).toBe('server');
      expect(result.message).toBeDefined();
      expect(result.retryable).toBe(true);
    });

    it('should extract custom error messages from response data', () => {
      const error: Partial<AxiosError> = {
        response: {
          status: 400,
          statusText: 'Bad Request',
          data: { message: 'Custom error message from API' },
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = parseApiError(error);

      expect(result.message).toBe('Custom error message from API');
    });
  });

  describe('Retry Logic Determination', () => {
    it('should mark network errors as retryable', () => {
      const networkError: Partial<AxiosError> = {
        code: 'ERR_NETWORK',
        message: 'Network Error',
        config: {} as InternalAxiosRequestConfig,
        request: {},
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
      };

      const result = parseApiError(networkError);
      expect(result.retryable).toBe(true);
    });

    it('should mark timeout errors as retryable', () => {
      const timeoutError: Partial<AxiosError> = {
        code: 'ECONNABORTED',
        message: 'timeout',
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
      };

      const result = parseApiError(timeoutError);
      expect(result.retryable).toBe(true);
    });

    it('should mark server errors (5xx) as retryable', () => {
      const serverError: Partial<AxiosError> = {
        response: {
          status: 503,
          statusText: 'Service Unavailable',
          data: {},
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = parseApiError(serverError);
      expect(result.retryable).toBe(true);
    });

    it('should mark auth errors (401, 403) as non-retryable', () => {
      const authError: Partial<AxiosError> = {
        response: {
          status: 401,
          statusText: 'Unauthorized',
          data: {},
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = parseApiError(authError);
      expect(result.retryable).toBe(false);
    });

    it('should mark validation errors (400) as non-retryable', () => {
      const validationError: Partial<AxiosError> = {
        response: {
          status: 400,
          statusText: 'Bad Request',
          data: {},
          headers: {},
          config: {} as InternalAxiosRequestConfig,
        },
        config: {} as InternalAxiosRequestConfig,
        isAxiosError: true,
        toJSON: () => ({}),
        name: 'AxiosError',
        message: 'Request failed',
      };

      const result = parseApiError(validationError);
      expect(result.retryable).toBe(false);
    });
  });
});
