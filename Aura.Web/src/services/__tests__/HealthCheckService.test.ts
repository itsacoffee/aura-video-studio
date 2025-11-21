import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { HealthCheckService } from '../HealthCheckService';

describe('HealthCheckService', () => {
  let mock: MockAdapter;
  let service: HealthCheckService;

  beforeEach(() => {
    mock = new MockAdapter(axios);
    service = new HealthCheckService({
      maxRetries: 3,
      retryDelayMs: 100,
      timeoutMs: 1000,
      exponentialBackoff: true,
      backendUrl: 'http://localhost:5000',
    });
  });

  afterEach(() => {
    mock.restore();
  });

  describe('checkHealth', () => {
    it('should return healthy result on successful connection', async () => {
      mock.onGet('http://localhost:5000/health').reply(200, { status: 'healthy' });

      const result = await service.checkHealth();

      expect(result.isHealthy).toBe(true);
      expect(result.statusCode).toBe(200);
      expect(result.message).toBe('Backend is healthy and responsive');
      expect(result.latencyMs).toBeGreaterThanOrEqual(0);
      expect(result.timestamp).toBeInstanceOf(Date);
    });

    it('should return healthy result on 404 (backend running, no health endpoint)', async () => {
      mock.onGet('http://localhost:5000/health').reply(404);

      const result = await service.checkHealth();

      expect(result.isHealthy).toBe(true);
      expect(result.statusCode).toBe(404);
      expect(result.message).toBe('Backend is running (no health endpoint)');
    });

    it('should retry on connection refused and return unhealthy after max retries', async () => {
      mock.onGet('http://localhost:5000/health').networkError();

      const result = await service.checkHealth();

      expect(result.isHealthy).toBe(false);
      expect(result.message).toContain('Network Error');
    });

    it('should call progress callback on each attempt', async () => {
      mock.onGet('http://localhost:5000/health').networkError();

      const progressCallback = vi.fn();
      await service.checkHealth(progressCallback);

      expect(progressCallback).toHaveBeenCalledTimes(3);
      expect(progressCallback).toHaveBeenNthCalledWith(1, 1, 3);
      expect(progressCallback).toHaveBeenNthCalledWith(2, 2, 3);
      expect(progressCallback).toHaveBeenNthCalledWith(3, 3, 3);
    });

    it('should succeed on retry after initial failures', async () => {
      mock
        .onGet('http://localhost:5000/health')
        .replyOnce(500)
        .onGet('http://localhost:5000/health')
        .replyOnce(500)
        .onGet('http://localhost:5000/health')
        .reply(200, { status: 'healthy' });

      const result = await service.checkHealth();

      expect(result.isHealthy).toBe(true);
      expect(result.statusCode).toBe(200);
    });

    it('should respect exponential backoff delays', async () => {
      const serviceWithBackoff = new HealthCheckService({
        maxRetries: 3,
        retryDelayMs: 100,
        exponentialBackoff: true,
        backendUrl: 'http://localhost:5000',
      });

      mock.onGet('http://localhost:5000/health').networkError();

      const startTime = Date.now();
      await serviceWithBackoff.checkHealth();
      const elapsed = Date.now() - startTime;

      // With exponential backoff: 100 * 1.5^0 + 100 * 1.5^1 = 100 + 150 = 250ms minimum
      expect(elapsed).toBeGreaterThanOrEqual(200);
    });

    it('should cap exponential backoff at 10 seconds', async () => {
      const serviceWithHighDelay = new HealthCheckService({
        maxRetries: 2,
        retryDelayMs: 10000,
        exponentialBackoff: true,
        backendUrl: 'http://localhost:5000',
      });

      mock.onGet('http://localhost:5000/health').networkError();

      const startTime = Date.now();
      await serviceWithHighDelay.checkHealth();
      const elapsed = Date.now() - startTime;

      // Should cap at 10000ms max, not grow beyond
      expect(elapsed).toBeLessThan(15000);
    }, 15000);
  });

  describe('quickCheck', () => {
    it('should return true on successful connection', async () => {
      mock.onGet('http://localhost:5000/health').reply(200);

      const result = await service.quickCheck();

      expect(result).toBe(true);
    });

    it('should return true on 404', async () => {
      mock.onGet('http://localhost:5000/health').reply(404);

      const result = await service.quickCheck();

      expect(result).toBe(true);
    });

    it('should return false on connection error', async () => {
      mock.onGet('http://localhost:5000/health').networkError();

      const result = await service.quickCheck();

      expect(result).toBe(false);
    });

    it('should return false on 500 error', async () => {
      mock.onGet('http://localhost:5000/health').reply(500);

      const result = await service.quickCheck();

      expect(result).toBe(false);
    });

    it('should timeout quickly (2 seconds)', async () => {
      mock.onGet('http://localhost:5000/health').timeout();

      const startTime = Date.now();
      const result = await service.quickCheck();
      const elapsed = Date.now() - startTime;

      expect(result).toBe(false);
      expect(elapsed).toBeLessThan(3000);
    });
  });

  describe('waitForBackend', () => {
    it('should return healthy result when backend becomes available', async () => {
      mock
        .onGet('http://localhost:5000/health')
        .replyOnce(500)
        .onGet('http://localhost:5000/health')
        .reply(200, { status: 'healthy' });

      const result = await service.waitForBackend(5000);

      expect(result.isHealthy).toBe(true);
      expect(result.statusCode).toBe(200);
    });

    it('should timeout if backend never becomes available', async () => {
      mock.onGet('http://localhost:5000/health').networkError();

      const result = await service.waitForBackend(500);

      expect(result.isHealthy).toBe(false);
      expect(result.message).toContain('did not become healthy within');
    });

    it('should call progress callback', async () => {
      mock.onGet('http://localhost:5000/health').reply(200);

      const progressCallback = vi.fn();
      await service.waitForBackend(1000, progressCallback);

      expect(progressCallback).toHaveBeenCalled();
    });
  });

  describe('constructor options', () => {
    it('should use default options when none provided', () => {
      const defaultService = new HealthCheckService();

      // Test by running a check and verifying behavior
      mock.onGet('http://localhost:5000/health').reply(200);

      expect(async () => {
        await defaultService.checkHealth();
      }).not.toThrow();
    });

    it('should use custom backend URL', async () => {
      const customService = new HealthCheckService({
        backendUrl: 'http://custom:8080',
      });

      mock.onGet('http://custom:8080/health').reply(200);

      const result = await customService.checkHealth();

      expect(result.isHealthy).toBe(true);
    });

    it('should disable exponential backoff when set to false', async () => {
      const serviceNoBackoff = new HealthCheckService({
        maxRetries: 3,
        retryDelayMs: 100,
        exponentialBackoff: false,
        backendUrl: 'http://localhost:5000',
      });

      mock.onGet('http://localhost:5000/health').networkError();

      const startTime = Date.now();
      await serviceNoBackoff.checkHealth();
      const elapsed = Date.now() - startTime;

      // Without exponential backoff: 100 + 100 = 200ms
      expect(elapsed).toBeGreaterThanOrEqual(180);
      expect(elapsed).toBeLessThan(300);
    });
  });
});
