import { describe, it, expect, vi } from 'vitest';
import { parseApiError, ProblemDetails } from '../utils/apiErrorHandler';

describe('API Error Handler', () => {
  it('should parse ProblemDetails from Response object', async () => {
    const mockResponse = new Response(
      JSON.stringify({
        title: 'Script Generation Failed',
        detail: 'The LLM provider returned an error',
        status: 500,
        type: 'https://docs.aura.studio/errors/E300',
        correlationId: 'abc123',
      }),
      {
        status: 500,
        statusText: 'Internal Server Error',
        headers: {
          'content-type': 'application/json',
          'X-Correlation-ID': 'abc123',
        },
      }
    );

    const result = await parseApiError(mockResponse);

    expect(result.title).toBe('Script Generation Failed');
    expect(result.message).toBe('The LLM provider returned an error');
    expect(result.correlationId).toBe('abc123');
    expect(result.errorCode).toBe('E300');
  });

  it('should extract error code from type URI', async () => {
    const mockResponse = new Response(
      JSON.stringify({
        title: 'Invalid Plan',
        detail: 'Duration out of range',
        type: 'https://docs.aura.studio/errors/E304',
      }),
      {
        status: 400,
        headers: {
          'content-type': 'application/json',
        },
      }
    );

    const result = await parseApiError(mockResponse);

    expect(result.errorCode).toBe('E304');
  });

  it('should extract correlationId from header if not in body', async () => {
    const mockResponse = new Response(
      JSON.stringify({
        title: 'Error',
        detail: 'Something went wrong',
      }),
      {
        status: 500,
        headers: {
          'content-type': 'application/json',
          'X-Correlation-ID': 'xyz789',
        },
      }
    );

    const result = await parseApiError(mockResponse);

    expect(result.correlationId).toBe('xyz789');
  });

  it('should handle non-JSON error responses', async () => {
    const mockResponse = new Response('Internal Server Error', {
      status: 500,
      statusText: 'Internal Server Error',
      headers: {
        'content-type': 'text/plain',
      },
    });

    const result = await parseApiError(mockResponse);

    expect(result.title).toBe('Error 500');
    expect(result.message).toContain('Internal Server Error');
  });

  it('should handle ProblemDetails objects directly', async () => {
    const problemDetails: ProblemDetails = {
      title: 'Authentication Failed',
      detail: 'Invalid API key',
      status: 401,
      correlationId: 'test123',
      errorCode: 'E306',
    };

    const result = await parseApiError(problemDetails);

    expect(result.title).toBe('Authentication Failed');
    expect(result.message).toBe('Invalid API key');
    expect(result.correlationId).toBe('test123');
    expect(result.errorCode).toBe('E306');
  });

  it('should handle Error objects', async () => {
    const error = new Error('Network error');

    const result = await parseApiError(error);

    expect(result.title).toBe('Error');
    expect(result.message).toBe('Network error');
  });

  it('should provide fallback for unknown error types', async () => {
    const unknownError = 'Something went wrong';

    const result = await parseApiError(unknownError);

    expect(result.title).toBe('Error');
    expect(result.message).toBe('Something went wrong');
  });
});
