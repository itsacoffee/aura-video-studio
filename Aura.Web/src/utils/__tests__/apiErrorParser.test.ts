import { AxiosError } from 'axios';
import { describe, expect, it } from 'vitest';
import { parseApiError } from '../apiErrorParser';

describe('apiErrorParser', () => {
  it('should handle unknown errors', () => {
    const result = parseApiError(null);

    expect(result.type).toBe('unknown');
    expect(result.message).toBe('An unexpected error occurred');
    expect(result.retryable).toBe(false);
  });

  it('should parse network errors', () => {
    const error = {
      code: 'ERR_NETWORK',
      message: 'Network Error',
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('network');
    expect(result.message).toContain('connect');
    expect(result.retryable).toBe(true);
  });

  it('should parse timeout errors', () => {
    const error = {
      code: 'ECONNABORTED',
      message: 'timeout of 5000ms exceeded',
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('timeout');
    expect(result.message).toContain('too long');
    expect(result.retryable).toBe(true);
  });

  it('should parse 401 authentication errors', () => {
    const error = {
      response: {
        status: 401,
        statusText: 'Unauthorized',
        data: { message: 'Invalid API key' },
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('auth');
    expect(result.message).toContain('API key');
    expect(result.message).toContain('expired');
    expect(result.retryable).toBe(false);
  });

  it('should parse 403 forbidden errors', () => {
    const error = {
      response: {
        status: 403,
        statusText: 'Forbidden',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('auth');
    expect(result.message).toContain('Access denied');
    expect(result.retryable).toBe(false);
  });

  it('should parse 429 rate limit errors', () => {
    const error = {
      response: {
        status: 429,
        statusText: 'Too Many Requests',
        data: { message: 'Rate limit exceeded' },
        headers: { 'retry-after': '60' },
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('rateLimit');
    expect(result.message).toContain('Rate limit');
    expect(result.message).toContain('60 seconds');
    expect(result.retryable).toBe(true);
  });

  it('should parse rate limit errors without retry-after header', () => {
    const error = {
      response: {
        status: 429,
        statusText: 'Too Many Requests',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('rateLimit');
    expect(result.message).toContain('few minutes');
    expect(result.retryable).toBe(true);
  });

  it('should parse 500 server errors', () => {
    const error = {
      response: {
        status: 500,
        statusText: 'Internal Server Error',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('server');
    expect(result.message).toContain('temporarily unavailable');
    expect(result.details).toContain('500');
    expect(result.retryable).toBe(true);
  });

  it('should parse 503 service unavailable errors', () => {
    const error = {
      response: {
        status: 503,
        statusText: 'Service Unavailable',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('server');
    expect(result.message).toContain('temporarily unavailable');
    expect(result.retryable).toBe(true);
  });

  it('should parse 400 bad request errors', () => {
    const error = {
      response: {
        status: 400,
        statusText: 'Bad Request',
        data: { message: 'Invalid input parameters' },
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('unknown');
    expect(result.message).toBe('Invalid input parameters');
    expect(result.retryable).toBe(false);
  });

  it('should parse 404 not found errors', () => {
    const error = {
      response: {
        status: 404,
        statusText: 'Not Found',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.type).toBe('unknown');
    expect(result.message).toContain('not found');
    expect(result.retryable).toBe(false);
  });

  it('should handle generic Error objects', () => {
    const error = new Error('Something went wrong');

    const result = parseApiError(error);

    expect(result.type).toBe('unknown');
    expect(result.message).toBe('Something went wrong');
    expect(result.retryable).toBe(false);
  });

  it('should mark 5xx errors as retryable', () => {
    const error = {
      response: {
        status: 502,
        statusText: 'Bad Gateway',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.retryable).toBe(true);
  });

  it('should mark 4xx errors (except auth) as not retryable', () => {
    const error = {
      response: {
        status: 400,
        statusText: 'Bad Request',
        data: {},
        headers: {},
        config: {} as AxiosError['config'],
      },
      isAxiosError: true,
    } as AxiosError;

    const result = parseApiError(error);

    expect(result.retryable).toBe(false);
  });
});
