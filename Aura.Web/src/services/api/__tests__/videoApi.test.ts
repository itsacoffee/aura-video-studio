/**
 * Unit tests for Video API methods
 */

import axios from 'axios';
import MockAdapter from 'axios-mock-adapter';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as apiClient from '../apiClient';
import * as videoApi from '../videoApi';
import type { VideoGenerationRequest, VideoGenerationResponse, VideoStatus } from '../videoApi';

describe('videoApi', () => {
  let mockAxios: MockAdapter;

  beforeEach(() => {
    mockAxios = new MockAdapter(axios);

    // Reset circuit breaker before each test
    apiClient.resetCircuitBreaker();

    // Mock localStorage for circuit breaker
    const localStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
      clear: vi.fn(),
      length: 0,
      key: vi.fn(),
    };
    vi.stubGlobal('localStorage', localStorageMock);

    // Mock sessionStorage for correlation IDs
    const sessionStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      removeItem: vi.fn(),
      clear: vi.fn(),
      length: 0,
      key: vi.fn(),
    };
    vi.stubGlobal('sessionStorage', sessionStorageMock);

    // Mock crypto.randomUUID
    vi.stubGlobal('crypto', {
      randomUUID: () => 'test-correlation-id',
    });
  });

  afterEach(() => {
    mockAxios.reset();
    apiClient.resetCircuitBreaker();
    vi.unstubAllGlobals();
  });

  describe('generateVideo', () => {
    it('should successfully create a video generation job', async () => {
      const request: VideoGenerationRequest = {
        brief: {
          topic: 'Test Topic',
          audience: 'General',
          goal: 'Educate',
          tone: 'Professional',
          language: 'en',
          aspect: 'Widescreen16x9',
        },
        planSpec: {
          targetDuration: '00:01:00',
          pacing: 'Conversational',
          density: 'Balanced',
          style: 'Modern',
        },
        voiceSpec: {
          voiceName: 'David',
          rate: 1.0,
          pitch: 1.0,
          pause: 'Natural',
        },
        renderSpec: {
          res: '1080p',
          container: 'mp4',
          videoBitrateK: 5000,
          audioBitrateK: 192,
          fps: 30,
          codec: 'h264',
          qualityLevel: 'High',
          enableSceneCut: true,
        },
      };

      const response: VideoGenerationResponse = {
        jobId: 'job-123',
        status: 'Queued',
        stage: 'Initializing',
        correlationId: 'test-correlation-id',
      };

      mockAxios.onPost('/api/jobs').reply(200, response);

      const result = await videoApi.generateVideo(request);

      expect(result).toEqual(response);
      expect(result.jobId).toBe('job-123');
    });

    it('should handle validation errors', async () => {
      const request: VideoGenerationRequest = {
        brief: {
          topic: '',
          audience: 'General',
          goal: 'Educate',
          tone: 'Professional',
          language: 'en',
          aspect: 'Widescreen16x9',
        },
        planSpec: {
          targetDuration: '00:01:00',
          pacing: 'Conversational',
          density: 'Balanced',
          style: 'Modern',
        },
        voiceSpec: {
          voiceName: 'David',
          rate: 1.0,
          pitch: 1.0,
          pause: 'Natural',
        },
        renderSpec: {
          res: '1080p',
          container: 'mp4',
          videoBitrateK: 5000,
          audioBitrateK: 192,
          fps: 30,
          codec: 'h264',
          qualityLevel: 'High',
          enableSceneCut: true,
        },
      };

      mockAxios.onPost('/api/jobs').reply(400, {
        title: 'Invalid Request',
        detail: 'Topic is required',
        status: 400,
      });

      await expect(videoApi.generateVideo(request)).rejects.toThrow();
    });
  });

  describe('getVideoStatus', () => {
    it('should fetch video generation status', async () => {
      const status: VideoStatus = {
        id: 'job-123',
        status: 'Running',
        stage: 'Script Generation',
        percent: 25,
        artifacts: [],
        logs: [],
        startedAt: '2024-01-01T00:00:00Z',
      };

      mockAxios.onGet('/api/jobs/job-123').reply(200, status);

      const result = await videoApi.getVideoStatus('job-123');

      expect(result).toEqual(status);
      expect(result.status).toBe('Running');
      expect(result.percent).toBe(25);
    });

    it('should handle 404 for non-existent job', async () => {
      mockAxios.onGet('/api/jobs/non-existent').reply(404, {
        title: 'Not Found',
        detail: 'Job not found',
        status: 404,
      });

      await expect(videoApi.getVideoStatus('non-existent')).rejects.toThrow();
    });
  });

  describe('cancelVideoGeneration', () => {
    it('should successfully cancel a job', async () => {
      mockAxios.onPost('/api/jobs/job-123/cancel').reply(200);

      await expect(videoApi.cancelVideoGeneration('job-123')).resolves.not.toThrow();
    });

    it('should handle errors when cancelling', async () => {
      mockAxios.onPost('/api/jobs/job-123/cancel').reply(500, {
        title: 'Internal Server Error',
        detail: 'Failed to cancel job',
        status: 500,
      });

      await expect(videoApi.cancelVideoGeneration('job-123')).rejects.toThrow();
    });
  });

  describe('listJobs', () => {
    it('should list all jobs', async () => {
      const jobs: VideoStatus[] = [
        {
          id: 'job-1',
          status: 'Done',
          stage: 'Completed',
          percent: 100,
          artifacts: [],
          logs: [],
          startedAt: '2024-01-01T00:00:00Z',
          finishedAt: '2024-01-01T00:05:00Z',
        },
        {
          id: 'job-2',
          status: 'Running',
          stage: 'Rendering',
          percent: 75,
          artifacts: [],
          logs: [],
          startedAt: '2024-01-01T00:10:00Z',
        },
      ];

      mockAxios.onGet('/api/jobs').reply(200, { jobs });

      const result = await videoApi.listJobs();

      expect(result.jobs).toHaveLength(2);
      expect(result.jobs[0].id).toBe('job-1');
      expect(result.jobs[1].id).toBe('job-2');
    });
  });

  describe('streamProgress', () => {
    it('should create EventSource for progress updates', () => {
      const onProgress = vi.fn();

      // Mock EventSource
      const mockEventSource = {
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        close: vi.fn(),
        onerror: null,
      } as unknown as EventSource;

      vi.stubGlobal(
        'EventSource',
        vi.fn(() => mockEventSource)
      );

      const eventSource = videoApi.streamProgress('job-123', onProgress);

      expect(eventSource).toBeDefined();
      expect(mockEventSource.addEventListener).toHaveBeenCalled();
    });
  });
});
