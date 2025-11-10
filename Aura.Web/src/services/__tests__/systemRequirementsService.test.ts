import { describe, it, expect, vi, beforeEach } from 'vitest';
import { checkSystemRequirements, getSystemRecommendations } from '../systemRequirementsService';
import type { SystemRequirements } from '../systemRequirementsService';

describe('systemRequirementsService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });

  describe('checkSystemRequirements', () => {
    it('should check all system requirements', async () => {
      // Mock API responses
      global.fetch = vi.fn((url: string | URL | Request) => {
        const urlString = url.toString();
        
        if (urlString.includes('/api/system/disk-space')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ availableGB: 100, totalGB: 500 }),
          } as Response);
        }
        
        if (urlString.includes('/api/system/gpu')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({
              detected: true,
              vendor: 'NVIDIA',
              model: 'GeForce RTX 3060',
              memoryMB: 6144,
              hardwareAcceleration: true,
              videoEncoding: true,
              videoDecoding: true,
            }),
          } as Response);
        }
        
        if (urlString.includes('/api/system/memory')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ totalGB: 16, availableGB: 10 }),
          } as Response);
        }
        
        return Promise.reject(new Error('Not found'));
      }) as typeof fetch;

      const requirements = await checkSystemRequirements();

      expect(requirements.diskSpace.status).toBe('pass');
      expect(requirements.gpu.detected).toBe(true);
      expect(requirements.gpu.vendor).toBe('NVIDIA');
      expect(requirements.memory.total).toBe(16);
      expect(requirements.overall).toBe('pass');
    });

    it('should handle low disk space', async () => {
      global.fetch = vi.fn((url: string | URL | Request) => {
        const urlString = url.toString();
        
        if (urlString.includes('/api/system/disk-space')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({ availableGB: 5, totalGB: 100 }),
          } as Response);
        }
        
        return Promise.reject(new Error('Not found'));
      }) as typeof fetch;

      const requirements = await checkSystemRequirements();

      expect(requirements.diskSpace.status).toBe('fail');
      expect(requirements.diskSpace.warnings.length).toBeGreaterThan(0);
      expect(requirements.overall).toBe('fail');
    });

    it('should warn about insufficient GPU', async () => {
      global.fetch = vi.fn((url: string | URL | Request) => {
        const urlString = url.toString();
        
        if (urlString.includes('/api/system/gpu')) {
          return Promise.resolve({
            ok: true,
            json: () => Promise.resolve({
              detected: false,
              vendor: 'None',
              model: 'None',
              memoryMB: 0,
              hardwareAcceleration: false,
              videoEncoding: false,
              videoDecoding: false,
            }),
          } as Response);
        }
        
        return Promise.reject(new Error('Not found'));
      }) as typeof fetch;

      const requirements = await checkSystemRequirements();

      expect(requirements.gpu.detected).toBe(false);
      expect(requirements.gpu.status).toBe('warning');
      expect(requirements.gpu.recommendations.length).toBeGreaterThan(0);
    });
  });

  describe('getSystemRecommendations', () => {
    it('should provide recommendations based on requirements', () => {
      const requirements: SystemRequirements = {
        diskSpace: {
          available: 5,
          total: 100,
          percentage: 5,
          status: 'fail',
          warnings: ['Not enough disk space'],
        },
        gpu: {
          detected: false,
          capabilities: {
            hardwareAcceleration: false,
            videoEncoding: false,
            videoDecoding: false,
          },
          status: 'warning',
          recommendations: ['No GPU detected'],
        },
        memory: {
          total: 16,
          available: 10,
          percentage: 62.5,
          status: 'pass',
          warnings: [],
        },
        os: {
          platform: 'Windows',
          version: '10',
          architecture: 'x64',
          compatible: true,
        },
        overall: 'fail',
      };

      const recommendations = getSystemRecommendations(requirements);

      expect(recommendations.length).toBeGreaterThan(0);
      expect(recommendations.some((r) => r.includes('disk space'))).toBe(true);
      expect(recommendations.some((r) => r.includes('GPU'))).toBe(true);
    });

    it('should not provide recommendations for passing requirements', () => {
      const requirements: SystemRequirements = {
        diskSpace: {
          available: 200,
          total: 500,
          percentage: 40,
          status: 'pass',
          warnings: [],
        },
        gpu: {
          detected: true,
          vendor: 'NVIDIA',
          model: 'RTX 3060',
          memory: 6144,
          capabilities: {
            hardwareAcceleration: true,
            videoEncoding: true,
            videoDecoding: true,
          },
          status: 'pass',
          recommendations: ['NVIDIA GPU detected. Enable NVENC'],
        },
        memory: {
          total: 16,
          available: 10,
          percentage: 62.5,
          status: 'pass',
          warnings: [],
        },
        os: {
          platform: 'Windows',
          version: '10',
          architecture: 'x64',
          compatible: true,
        },
        overall: 'pass',
      };

      const recommendations = getSystemRecommendations(requirements);

      // Should have at least the GPU recommendation
      expect(recommendations.length).toBeGreaterThan(0);
      expect(recommendations.some((r) => r.includes('NVENC'))).toBe(true);
    });
  });
});
