import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  checkOllamaStatus,
  getRecommendedModels,
  getInstallGuide,
  getModelRecommendationsForSystem,
  canRunModel,
} from '../ollamaSetupService';

// Mock the ollamaClient
vi.mock('../api/ollamaClient', () => ({
  ollamaClient: {
    getStatus: vi.fn(),
    getModels: vi.fn(),
    start: vi.fn(),
  },
}));

import { ollamaClient } from '../api/ollamaClient';

describe('ollamaSetupService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('checkOllamaStatus', () => {
    it('should return installed and running status with models', async () => {
      vi.mocked(ollamaClient.getStatus).mockResolvedValue({
        installed: true,
        running: true,
        version: '0.1.0',
        installPath: '/usr/local/bin/ollama',
      });

      vi.mocked(ollamaClient.getModels).mockResolvedValue({
        models: [
          { name: 'llama3.1:8b', size: '4.7 GB', modifiedAt: '2024-01-01' },
          { name: 'mistral:7b', size: '4.1 GB', modifiedAt: '2024-01-01' },
        ],
      });

      const status = await checkOllamaStatus();

      expect(status.installed).toBe(true);
      expect(status.running).toBe(true);
      expect(status.modelsInstalled).toHaveLength(2);
      expect(status.modelsInstalled).toContain('llama3.1:8b');
    });

    it('should return not installed when Ollama is not found', async () => {
      vi.mocked(ollamaClient.getStatus).mockRejectedValue(new Error('Not found'));

      const status = await checkOllamaStatus();

      expect(status.installed).toBe(false);
      expect(status.running).toBe(false);
      expect(status.modelsInstalled).toHaveLength(0);
    });
  });

  describe('getRecommendedModels', () => {
    it('should return a list of recommended models', () => {
      const models = getRecommendedModels();

      expect(models.length).toBeGreaterThan(0);
      expect(models.some((m) => m.recommended)).toBe(true);
      expect(models.every((m) => m.name && m.displayName && m.size)).toBe(true);
    });
  });

  describe('getInstallGuide', () => {
    it('should return Windows install guide on Windows platform', () => {
      Object.defineProperty(navigator, 'platform', {
        value: 'Win32',
        writable: true,
        configurable: true,
      });

      const guide = getInstallGuide();

      expect(guide.platform).toBe('Windows');
      expect(guide.downloadUrl).toContain('windows');
      expect(guide.steps.length).toBeGreaterThan(0);
    });

    it('should return macOS install guide on Mac platform', () => {
      Object.defineProperty(navigator, 'platform', {
        value: 'MacIntel',
        writable: true,
        configurable: true,
      });

      const guide = getInstallGuide();

      expect(guide.platform).toBe('macOS');
      expect(guide.downloadUrl).toContain('mac');
      expect(guide.steps.length).toBeGreaterThan(0);
    });

    it('should return Linux install guide on Linux platform', () => {
      Object.defineProperty(navigator, 'platform', {
        value: 'Linux x86_64',
        writable: true,
        configurable: true,
      });

      const guide = getInstallGuide();

      expect(guide.platform).toBe('Linux');
      expect(guide.downloadUrl).toContain('linux');
      expect(guide.steps.length).toBeGreaterThan(0);
    });
  });

  describe('getModelRecommendationsForSystem', () => {
    it('should recommend models based on available resources', () => {
      const availableMemoryGB = 16;
      const availableDiskGB = 100;

      const models = getModelRecommendationsForSystem(availableMemoryGB, availableDiskGB);

      expect(models.length).toBeGreaterThan(0);
      // All recommended models should fit in available resources
      models.forEach((model) => {
        const modelSizeGB = model.sizeBytes / (1024 * 1024 * 1024);
        expect(modelSizeGB).toBeLessThanOrEqual(availableDiskGB);
      });
    });

    it('should not recommend large models for systems with low memory', () => {
      const availableMemoryGB = 4;
      const availableDiskGB = 50;

      const models = getModelRecommendationsForSystem(availableMemoryGB, availableDiskGB);

      // Should not include 70B model
      expect(models.every((m) => !m.name.includes('70b'))).toBe(true);
    });
  });

  describe('canRunModel', () => {
    it('should return true if system has enough memory', () => {
      const model = {
        name: 'llama3.2:3b',
        displayName: 'Llama 3.2 (3B)',
        size: '2.0 GB',
        sizeBytes: 2 * 1024 * 1024 * 1024,
        description: 'Test model',
        recommended: true,
      };

      const canRun = canRunModel(model, 8);
      expect(canRun).toBe(true);
    });

    it('should return false if system lacks memory', () => {
      const model = {
        name: 'llama3.1:70b',
        displayName: 'Llama 3.1 (70B)',
        size: '40 GB',
        sizeBytes: 40 * 1024 * 1024 * 1024,
        description: 'Test model',
        recommended: false,
      };

      const canRun = canRunModel(model, 8);
      expect(canRun).toBe(false);
    });
  });
});
