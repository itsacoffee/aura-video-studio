/**
 * Tests for wizardService
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import * as apiClient from '../api/apiClient';
import * as ollamaService from '../api/ollamaService';
import {
  storeBrief,
  fetchAvailableVoices,
  fetchAvailableStyles,
  generateScript,
  generatePreview,
  startFinalRendering,
  saveWizardState,
  loadWizardState,
  generateScriptWithProgress,
  type WizardBriefData,
  type WizardStyleData,
  type WizardScriptData,
} from '../wizardService';

// Mock the apiClient module
vi.mock('../api/apiClient', () => ({
  post: vi.fn(),
  get: vi.fn(),
  postWithTimeout: vi.fn(),
}));

// Mock loggingService
vi.mock('../loggingService', () => ({
  loggingService: {
    info: vi.fn(),
    debug: vi.fn(),
    error: vi.fn(),
  },
}));

describe('wizardService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockBriefData: WizardBriefData = {
    topic: 'Introduction to AI',
    audience: 'Students',
    goal: 'Educate',
    tone: 'Professional',
    language: 'English',
    duration: 60,
    videoType: 'educational',
  };

  const mockStyleData: WizardStyleData = {
    voiceProvider: 'ElevenLabs',
    voiceName: 'Rachel',
    visualStyle: 'modern',
    musicGenre: 'ambient',
    musicEnabled: true,
  };

  const mockScriptData: WizardScriptData = {
    generatedScript: 'Test script content',
    scenes: [
      {
        id: 'scene-1',
        text: 'Welcome to our video',
        duration: 5,
        visualDescription: 'Opening title',
      },
    ],
    totalDuration: 60,
  };

  describe('storeBrief', () => {
    it('should store brief data successfully', async () => {
      const mockResponse = {
        briefId: 'brief-123',
        correlationId: 'corr-456',
        savedAt: '2024-01-01T00:00:00Z',
      };

      vi.mocked(apiClient.post).mockResolvedValueOnce(mockResponse);

      const result = await storeBrief(mockBriefData);

      expect(apiClient.post).toHaveBeenCalledWith('/api/wizard/brief', mockBriefData, undefined);
      expect(result.briefId).toBe('brief-123');
      expect(result.correlationId).toBe('corr-456');
    });

    it('should handle errors when storing brief', async () => {
      const error = new Error('Network error');
      vi.mocked(apiClient.post).mockRejectedValueOnce(error);

      await expect(storeBrief(mockBriefData)).rejects.toThrow('Network error');
    });
  });

  describe('fetchAvailableVoices', () => {
    it('should fetch voices without provider filter', async () => {
      const mockResponse = {
        voices: [
          {
            id: 'voice-1',
            name: 'Rachel',
            provider: 'ElevenLabs',
            language: 'English',
            gender: 'female',
          },
          {
            id: 'voice-2',
            name: 'Adam',
            provider: 'ElevenLabs',
            language: 'English',
            gender: 'male',
          },
        ],
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce(mockResponse);

      const result = await fetchAvailableVoices();

      expect(apiClient.get).toHaveBeenCalledWith('/api/voices', undefined);
      expect(result.voices).toHaveLength(2);
    });

    it('should fetch voices with provider filter', async () => {
      const mockResponse = {
        voices: [
          {
            id: 'voice-1',
            name: 'Rachel',
            provider: 'ElevenLabs',
            language: 'English',
            gender: 'female',
          },
        ],
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce(mockResponse);

      const result = await fetchAvailableVoices('ElevenLabs');

      expect(apiClient.get).toHaveBeenCalledWith('/api/voices?provider=ElevenLabs', undefined);
      expect(result.voices).toHaveLength(1);
    });

    it('should return empty array on error', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'));

      const result = await fetchAvailableVoices();

      expect(result.voices).toEqual([]);
    });
  });

  describe('fetchAvailableStyles', () => {
    it('should fetch visual styles successfully', async () => {
      const mockResponse = {
        styles: [
          {
            id: 'style-1',
            name: 'Modern',
            description: 'Clean and contemporary',
          },
          {
            id: 'style-2',
            name: 'Retro',
            description: 'Vintage aesthetic',
          },
        ],
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce(mockResponse);

      const result = await fetchAvailableStyles();

      expect(apiClient.get).toHaveBeenCalledWith('/api/styles', undefined);
      expect(result.styles).toHaveLength(2);
    });

    it('should return empty array on error', async () => {
      vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'));

      const result = await fetchAvailableStyles();

      expect(result.styles).toEqual([]);
    });
  });

  describe('generateScript', () => {
    it('should generate script successfully', async () => {
      const mockResponse = {
        jobId: 'job-123',
        script: 'Generated script content',
        scenes: [
          {
            id: 'scene-1',
            text: 'Scene text',
            duration: 10,
            visualDescription: 'Visual description',
          },
        ],
        totalDuration: 60,
        correlationId: 'corr-789',
      };

      vi.mocked(apiClient.postWithTimeout).mockResolvedValueOnce(mockResponse);

      const result = await generateScript(mockBriefData, mockStyleData);

      expect(apiClient.postWithTimeout).toHaveBeenCalledWith(
        '/api/wizard/generate-script',
        {
          brief: mockBriefData,
          style: mockStyleData,
        },
        300000, // 5 minute timeout for Ollama/local models
        undefined
      );
      expect(result.jobId).toBe('job-123');
      expect(result.scenes).toHaveLength(1);
    });

    it('should handle script generation errors', async () => {
      const error = new Error('Script generation failed');
      vi.mocked(apiClient.postWithTimeout).mockRejectedValueOnce(error);

      await expect(generateScript(mockBriefData, mockStyleData)).rejects.toThrow(
        'Script generation failed'
      );
    });
  });

  describe('generatePreview', () => {
    it('should generate preview successfully', async () => {
      const mockPreviewConfig = {
        resolution: '720p',
        quality: 'medium',
        previewDuration: 30,
      };

      const mockResponse = {
        previewId: 'preview-123',
        previewUrl: '/previews/preview-123.mp4',
        status: 'generating',
        correlationId: 'corr-999',
      };

      vi.mocked(apiClient.postWithTimeout).mockResolvedValueOnce(mockResponse);

      const result = await generatePreview(
        mockBriefData,
        mockStyleData,
        mockScriptData,
        mockPreviewConfig
      );

      expect(apiClient.postWithTimeout).toHaveBeenCalledWith(
        '/api/wizard/generate-preview',
        {
          brief: mockBriefData,
          style: mockStyleData,
          script: mockScriptData,
          previewConfig: mockPreviewConfig,
        },
        180000,
        undefined
      );
      expect(result.previewId).toBe('preview-123');
    });
  });

  describe('startFinalRendering', () => {
    it('should start final rendering successfully', async () => {
      const mockExportConfig = {
        resolution: '1080p',
        fps: 30,
        codec: 'H264',
        quality: 75,
        includeSubs: true,
        outputFormat: 'mp4',
      };

      const mockResponse = {
        jobId: 'job-final-123',
        correlationId: 'corr-final',
        status: 'queued',
      };

      vi.mocked(apiClient.postWithTimeout).mockResolvedValueOnce(mockResponse);

      const result = await startFinalRendering(
        mockBriefData,
        mockStyleData,
        mockScriptData,
        mockExportConfig
      );

      expect(result.jobId).toBe('job-final-123');
      expect(result.correlationId).toBe('corr-final');
    });

    it('should map wizard data to job request format correctly', async () => {
      const mockExportConfig = {
        resolution: '1920x1080',
        fps: 60,
        codec: 'HEVC',
        quality: 90,
        includeSubs: false,
        outputFormat: 'mkv',
      };

      const mockResponse = {
        jobId: 'job-123',
        correlationId: 'corr-123',
        status: 'queued',
      };

      vi.mocked(apiClient.postWithTimeout).mockResolvedValueOnce(mockResponse);

      await startFinalRendering(mockBriefData, mockStyleData, mockScriptData, mockExportConfig);

      const callArgs = vi.mocked(apiClient.postWithTimeout).mock.calls[0];
      const jobRequest = callArgs[1];

      expect(jobRequest).toMatchObject({
        brief: {
          topic: mockBriefData.topic,
          audience: mockBriefData.audience,
          goal: mockBriefData.goal,
          tone: mockBriefData.tone,
          language: mockBriefData.language,
        },
        planSpec: {
          style: mockStyleData.visualStyle,
        },
        voiceSpec: {
          voiceName: mockStyleData.voiceName,
        },
        renderSpec: {
          fps: mockExportConfig.fps,
          codec: mockExportConfig.codec,
          container: mockExportConfig.outputFormat,
        },
      });
    });
  });

  describe('saveWizardState', () => {
    it('should save wizard state successfully', async () => {
      const wizardData = {
        brief: mockBriefData,
        style: mockStyleData,
        script: mockScriptData,
        currentStep: 3,
      };

      const mockResponse = {
        wizardId: 'wizard-123',
      };

      vi.mocked(apiClient.post).mockResolvedValueOnce(mockResponse);

      const result = await saveWizardState(wizardData);

      expect(apiClient.post).toHaveBeenCalledWith('/api/wizard/save-state', wizardData, undefined);
      expect(result.wizardId).toBe('wizard-123');
    });
  });

  describe('loadWizardState', () => {
    it('should load wizard state successfully', async () => {
      const mockResponse = {
        brief: mockBriefData,
        style: mockStyleData,
        script: mockScriptData,
        currentStep: 3,
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce(mockResponse);

      const result = await loadWizardState('wizard-123');

      expect(apiClient.get).toHaveBeenCalledWith('/api/wizard/load-state/wizard-123', undefined);
      expect(result.currentStep).toBe(3);
      expect(result.brief.topic).toBe('Introduction to AI');
    });
  });

  describe('generateScriptWithProgress', () => {
    beforeEach(() => {
      vi.clearAllMocks();
      // Mock ollamaService.streamGeneration
      vi.mock('../api/ollamaService', () => ({
        streamGeneration: vi.fn(),
      }));
    });

    it('should stream script generation with progress events', async () => {
      const events: ollamaService.StreamingScriptEvent[] = [];
      const mockAccumulatedContent =
        'Scene 1: Welcome to our video.\n\nScene 2: Let us explore the topic.';

      // Mock the streamGeneration function
      vi.mocked(ollamaService.streamGeneration).mockImplementation(async (_request, onProgress) => {
        // Simulate init event
        onProgress({
          eventType: 'init',
          providerName: 'Ollama',
          isLocal: true,
          expectedFirstTokenMs: 1000,
          expectedTokensPerSec: 20,
          costPer1KTokens: null,
          supportsStreaming: true,
        });

        // Simulate chunk events
        onProgress({
          eventType: 'chunk',
          content: 'Scene 1: ',
          accumulatedContent: 'Scene 1: ',
          tokenIndex: 1,
        });

        onProgress({
          eventType: 'chunk',
          content: 'Welcome to our video.',
          accumulatedContent: 'Scene 1: Welcome to our video.',
          tokenIndex: 2,
        });

        // Simulate complete event
        onProgress({
          eventType: 'complete',
          content: '',
          accumulatedContent: mockAccumulatedContent,
          tokenCount: 15,
          metadata: {
            totalTokens: 15,
            estimatedCost: null,
            tokensPerSecond: 20,
            isLocalModel: true,
            modelName: 'llama2',
            timeToFirstTokenMs: 500,
            totalDurationMs: 750,
            finishReason: 'stop',
          },
        });

        return mockAccumulatedContent;
      });

      const onProgress = (event: ollamaService.StreamingScriptEvent) => events.push(event);

      const result = await generateScriptWithProgress(mockBriefData, mockStyleData, onProgress);

      expect(events.length).toBeGreaterThan(0);
      expect(events[events.length - 1].eventType).toBe('complete');
      expect(result.script).toBe(mockAccumulatedContent);
      expect(result.scenes.length).toBeGreaterThan(0);
      expect(result.jobId).toBeDefined();
    });

    it('should handle error events from streaming', async () => {
      const events: ollamaService.StreamingScriptEvent[] = [];

      // Mock streamGeneration to throw an error
      vi.mocked(ollamaService.streamGeneration).mockRejectedValueOnce(new Error('Stream failed'));

      const onProgress = (event: ollamaService.StreamingScriptEvent) => events.push(event);

      await expect(
        generateScriptWithProgress(mockBriefData, mockStyleData, onProgress)
      ).rejects.toThrow('Stream failed');
    });

    it('should throw error when no script content received', async () => {
      // Mock streamGeneration to complete without any content
      vi.mocked(ollamaService.streamGeneration).mockImplementation(async () => {
        return '';
      });

      const onProgress = vi.fn();

      await expect(
        generateScriptWithProgress(mockBriefData, mockStyleData, onProgress)
      ).rejects.toThrow('No script content received from streaming generation');
    });
  });
});
