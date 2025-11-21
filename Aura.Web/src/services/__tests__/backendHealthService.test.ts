/**
 * Tests for BackendHealthService
 */

import axios from 'axios';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { BackendHealthService } from '../backendHealthService';

// Mock axios
vi.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

// Mock loggingService
vi.mock('../loggingService', () => ({
  loggingService: {
    info: vi.fn(),
    warn: vi.fn(),
    error: vi.fn(),
  },
}));

describe('BackendHealthService', () => {
  let service: BackendHealthService;

  beforeEach(() => {
    service = new BackendHealthService('http://localhost:5005');
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('constructor', () => {
    it('should use provided baseUrl', () => {
      const customService = new BackendHealthService('http://localhost:8080');
      expect(customService.getBaseUrl()).toBe('http://localhost:8080');
    });

    it('should use default baseUrl if not provided', () => {
      const defaultService = new BackendHealthService();
      expect(defaultService.getBaseUrl()).toBe('http://localhost:5005');
    });
  });

  describe('checkHealth', () => {
    it('should return healthy status when backend responds with ok', async () => {
      mockedAxios.get.mockResolvedValueOnce({
        status: 200,
        data: { status: 'ok' },
      });

      const status = await service.checkHealth({
        maxRetries: 1,
        timeout: 1000,
      });

      expect(status.healthy).toBe(true);
      expect(status.reachable).toBe(true);
      expect(status.error).toBeNull();
      expect(status.responseTime).toBeGreaterThanOrEqual(0);
    });

    it('should return unhealthy status when backend is unreachable', async () => {
      mockedAxios.get.mockRejectedValueOnce({
        code: 'ECONNREFUSED',
        message: 'Connection refused',
      });

      const status = await service.checkHealth({
        maxRetries: 1,
        timeout: 1000,
      });

      expect(status.healthy).toBe(false);
      expect(status.reachable).toBe(false);
      expect(status.error).toBeTruthy();
    });

    it('should retry on failure with exponential backoff', async () => {
      mockedAxios.get
        .mockRejectedValueOnce({ code: 'ECONNREFUSED' })
        .mockRejectedValueOnce({ code: 'ECONNREFUSED' })
        .mockResolvedValueOnce({
          status: 200,
          data: { status: 'ok' },
        });

      const status = await service.checkHealth({
        maxRetries: 3,
        timeout: 1000,
        retryDelay: 10,
        exponentialBackoff: true,
      });

      expect(status.healthy).toBe(true);
      expect(mockedAxios.get).toHaveBeenCalledTimes(3);
    });

    it('should fail after max retries exhausted', async () => {
      mockedAxios.get.mockRejectedValue({ code: 'ECONNREFUSED' });

      const status = await service.checkHealth({
        maxRetries: 3,
        timeout: 1000,
        retryDelay: 10,
      });

      expect(status.healthy).toBe(false);
      expect(mockedAxios.get).toHaveBeenCalledTimes(3);
    });
  });

  describe('quickCheck', () => {
    it('should perform single check without retries', async () => {
      mockedAxios.get.mockResolvedValueOnce({
        status: 200,
        data: { status: 'ok' },
      });

      const status = await service.quickCheck(2000);

      expect(status.healthy).toBe(true);
      expect(mockedAxios.get).toHaveBeenCalledTimes(1);
    });

    it('should fail immediately on error', async () => {
      mockedAxios.get.mockRejectedValueOnce({ code: 'ETIMEDOUT' });

      const status = await service.quickCheck(2000);

      expect(status.healthy).toBe(false);
      expect(mockedAxios.get).toHaveBeenCalledTimes(1);
    });
  });

  describe('waitForHealthy', () => {
    it('should return true when backend becomes healthy', async () => {
      mockedAxios.get.mockRejectedValueOnce({ code: 'ECONNREFUSED' }).mockResolvedValueOnce({
        status: 200,
        data: { status: 'ok' },
      });

      const result = await service.waitForHealthy(5000, 100);

      expect(result).toBe(true);
    });

    it('should return false on timeout', async () => {
      mockedAxios.get.mockRejectedValue({ code: 'ECONNREFUSED' });

      const result = await service.waitForHealthy(200, 50);

      expect(result).toBe(false);
    });

    it('should stop checking once healthy', async () => {
      mockedAxios.get.mockResolvedValue({
        status: 200,
        data: { status: 'ok' },
      });

      const result = await service.waitForHealthy(1000, 100);

      expect(result).toBe(true);
      // Should only check once since it's immediately healthy
      expect(mockedAxios.get).toHaveBeenCalledTimes(1);
    });
  });

  describe('getStatus', () => {
    it('should return cached status', async () => {
      mockedAxios.get.mockResolvedValueOnce({
        status: 200,
        data: { status: 'ok' },
      });

      await service.checkHealth({ maxRetries: 1 });
      const status = service.getStatus();

      expect(status.healthy).toBe(true);
      expect(status.lastChecked).toBeInstanceOf(Date);
    });
  });

  describe('setBaseUrl', () => {
    it('should update base URL', () => {
      service.setBaseUrl('http://localhost:8080');
      expect(service.getBaseUrl()).toBe('http://localhost:8080');
    });
  });
});
