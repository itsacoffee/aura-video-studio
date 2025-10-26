/**
 * Integration test to demonstrate API client features
 * This test showcases the key capabilities of the enhanced API client
 */

import MockAdapter from 'axios-mock-adapter';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import apiClient, {
  get,
  post,
  resetCircuitBreaker,
  getCircuitBreakerState,
} from '../services/api/apiClient';

let mock: MockAdapter;

describe('API Client Integration', () => {
  beforeEach(() => {
    resetCircuitBreaker();
    mock = new MockAdapter(apiClient);
  });

  afterEach(() => {
    mock.restore();
  });

  it('should demonstrate automatic retry with exponential backoff', async () => {
    let attemptCount = 0;
    const attemptTimes: number[] = [];

    mock.onGet('/api/flaky-endpoint').reply(() => {
      attemptCount++;
      attemptTimes.push(Date.now());

      // Fail first 2 attempts, succeed on 3rd
      if (attemptCount < 3) {
        return [500, { error: 'Server temporarily unavailable' }];
      }
      return [200, { success: true, message: 'Operation completed' }];
    });

    const result = await get<{ success: boolean; message: string }>('/api/flaky-endpoint');

    // Verify success after retries
    expect(result.success).toBe(true);
    expect(attemptCount).toBe(3);

    // Verify exponential backoff timing
    // First retry should be ~1s after initial attempt
    // Second retry should be ~2s after first retry
    if (attemptTimes.length >= 3) {
      const delay1 = attemptTimes[1] - attemptTimes[0];
      const delay2 = attemptTimes[2] - attemptTimes[1];

      // Allow for some timing variance (±200ms)
      expect(delay1).toBeGreaterThanOrEqual(800);
      expect(delay1).toBeLessThanOrEqual(1200);
      expect(delay2).toBeGreaterThanOrEqual(1800);
      expect(delay2).toBeLessThanOrEqual(2200);
    }
  }, 6000); // Reduced timeout: 1s + 2s delays + buffer

  it('should demonstrate circuit breaker preventing cascading failures', async () => {
    // Configure endpoint to always fail
    mock.onGet('/api/failing-service').reply(500, { error: 'Service down' });

    // Make 5 requests to trigger circuit breaker
    const failedRequests = [];
    for (let i = 0; i < 5; i++) {
      try {
        await get('/api/failing-service', { _skipRetry: true });
      } catch (error) {
        failedRequests.push(error);
      }
    }

    // Circuit should now be OPEN
    expect(getCircuitBreakerState()).toBe('OPEN');
    expect(failedRequests.length).toBe(5);

    // Next request should fail immediately without hitting the endpoint
    let circuitBreakerError;
    try {
      await get('/api/failing-service', { _skipRetry: true });
    } catch (error: any) {
      circuitBreakerError = error;
    }

    expect(circuitBreakerError).toBeDefined();
    expect(circuitBreakerError.message).toContain('Circuit breaker');
  });

  it('should demonstrate user-friendly error messages', async () => {
    // Test various error scenarios
    const errorScenarios = [
      {
        status: 404,
        response: { message: 'Project not found' },
        expectedError: 'userMessage',
      },
      {
        status: 400,
        response: { errorCode: 'E300', message: 'Invalid script parameters' },
        expectedError: 'errorCode',
      },
      {
        status: 503,
        response: { message: 'Service unavailable' },
        expectedError: 'userMessage',
      },
    ];

    for (const scenario of errorScenarios) {
      mock.onGet('/api/test').reply(scenario.status, scenario.response);

      try {
        await get('/api/test', { _skipRetry: true });
        throw new Error('Expected error was not thrown');
      } catch (error: any) {
        if (error.message === 'Expected error was not thrown') {
          throw error;
        }
        expect(error.userMessage).toBeDefined();
        expect(error.userMessage).toBeTruthy();

        if (scenario.expectedError === 'errorCode') {
          expect(error.errorCode).toBe('E300');
        }
      }

      // Reset circuit breaker between tests
      resetCircuitBreaker();
    }
  });

  it('should demonstrate successful API workflow', async () => {
    // Mock a typical workflow: create project, add content, update, fetch
    mock.onPost('/api/projects').reply(201, {
      id: 'proj-123',
      name: 'My Video Project',
      createdAt: new Date().toISOString(),
    });

    mock.onGet('/api/projects/proj-123').reply(200, {
      id: 'proj-123',
      name: 'My Video Project',
      clips: [],
    });

    // Create project
    const project = await post<{ id: string; name: string }>('/api/projects', {
      name: 'My Video Project',
    });

    expect(project.id).toBe('proj-123');

    // Fetch project details
    const details = await get<{ id: string; name: string; clips: any[] }>(
      `/api/projects/${project.id}`
    );

    expect(details.id).toBe(project.id);
    expect(details.clips).toEqual([]);
  });

  it('should not retry on client errors (4xx)', async () => {
    let attempts = 0;

    mock.onPost('/api/projects').reply(() => {
      attempts++;
      return [400, { message: 'Invalid project data' }];
    });

    try {
      await post('/api/projects', { name: '' });
      throw new Error('Expected error was not thrown');
    } catch (error: any) {
      if (error.message === 'Expected error was not thrown') {
        throw error;
      }
      // Should only attempt once (no retries for 4xx errors)
      expect(attempts).toBe(1);
      expect(error.response.status).toBe(400);
    }
  });
});
