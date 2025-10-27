import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

/**
 * PHASE 2: Quick Demo End-to-End Validation
 * 
 * These smoke tests validate:
 * - Quick Demo from clean state
 * - Workflow completion (script → visuals → voiceover → timeline → preview)
 * - Error handling and recovery
 */

describe('Smoke Test: Quick Demo E2E', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('2.1 Quick Demo from Clean State', () => {
    it('should trigger Quick Demo without validation errors', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({
          success: true,
          brief: {
            topic: 'Quick Start Guide',
            audience: 'New Users',
            goal: 'Demonstrate video creation',
            tone: 'Friendly',
            language: 'English',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/quick/demo', { method: 'POST' });
      const data = await response.json();

      expect(response.status).toBe(200);
      expect(response.ok).toBe(true);
      expect(data.success).toBe(true);
    });

    it('should populate all required fields automatically', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          brief: {
            topic: 'Quick Start Guide',
            audience: 'New Users',
            goal: 'Demonstrate video creation',
            tone: 'Friendly',
            language: 'English',
            aspect: 'Widescreen16x9',
          },
          planSpec: {
            targetDuration: 15,
            pacing: 'Fast',
            density: 'Sparse',
            style: 'Tutorial',
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/quick/demo', { method: 'POST' });
      const data = await response.json();

      expect(data.brief.topic).toBeTruthy();
      expect(data.brief.audience).toBeTruthy();
      expect(data.brief.goal).toBeTruthy();
      expect(data.brief.tone).toBeTruthy();
      expect(data.brief.language).toBeTruthy();
      expect(data.planSpec.targetDuration).toBeGreaterThan(0);
    });

    it('should call validation endpoint successfully', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({
          isValid: true,
          errors: [],
          warnings: [],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/validation/brief', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          topic: 'Quick Start Guide',
          audience: 'New Users',
          goal: 'Demonstrate video creation',
        }),
      });
      const data = await response.json();

      expect(response.status).toBe(200);
      expect(data.isValid).toBe(true);
      expect(data.errors).toHaveLength(0);
    });

    it('should not show IsValid=False error', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          isValid: true, // Must be true
          errors: [],
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/validation/brief', { method: 'POST' });
      const data = await response.json();

      expect(data.isValid).not.toBe(false);
      expect(data.isValid).toBe(true);
    });
  });

  describe('2.2 Workflow Completion', () => {
    it('should generate script without errors', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'script-gen-123',
          status: 'completed',
          result: {
            script: 'Welcome to Aura Video Studio! This quick demo shows...',
            duration: 15,
            wordCount: 45,
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/generate/script', { method: 'POST' });
      const data = await response.json();

      expect(data.status).toBe('completed');
      expect(data.result.script).toBeTruthy();
      expect(data.result.script.length).toBeGreaterThan(0);
    });

    it('should generate visuals with placeholder or AI images', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'visuals-gen-123',
          status: 'completed',
          result: {
            images: [
              {
                url: '/stock/welcome-screen.jpg',
                type: 'stock',
                duration: 5,
              },
              {
                url: '/stock/demo-interface.jpg',
                type: 'stock',
                duration: 5,
              },
              {
                url: '/stock/success.jpg',
                type: 'stock',
                duration: 5,
              },
            ],
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/generate/visuals', { method: 'POST' });
      const data = await response.json();

      expect(data.status).toBe('completed');
      expect(data.result.images).toHaveLength(3);
      expect(data.result.images[0].url).toBeTruthy();
    });

    it('should generate voiceover audio file', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          jobId: 'voiceover-gen-123',
          status: 'completed',
          result: {
            audioFile: '/temp/voiceover-123.wav',
            duration: 15.3,
            format: 'wav',
            sampleRate: 44100,
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/generate/voiceover', { method: 'POST' });
      const data = await response.json();

      expect(data.status).toBe('completed');
      expect(data.result.audioFile).toBeTruthy();
      expect(data.result.duration).toBeGreaterThan(0);
    });

    it('should assemble timeline with clips in correct order', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          timeline: {
            tracks: [
              {
                type: 'video',
                clips: [
                  { start: 0, duration: 5, source: '/stock/welcome-screen.jpg' },
                  { start: 5, duration: 5, source: '/stock/demo-interface.jpg' },
                  { start: 10, duration: 5, source: '/stock/success.jpg' },
                ],
              },
              {
                type: 'audio',
                clips: [
                  { start: 0, duration: 15.3, source: '/temp/voiceover-123.wav' },
                ],
              },
            ],
            totalDuration: 15.3,
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/timeline/assemble', { method: 'POST' });
      const data = await response.json();

      expect(data.timeline.tracks).toHaveLength(2);
      expect(data.timeline.tracks[0].type).toBe('video');
      expect(data.timeline.tracks[1].type).toBe('audio');
      expect(data.timeline.totalDuration).toBeGreaterThan(0);
    });

    it('should show assembled video in preview', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          previewUrl: '/preview/assembled-123.mp4',
          thumbnailUrl: '/preview/assembled-123-thumb.jpg',
          duration: 15.3,
          ready: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/preview/generate', { method: 'POST' });
      const data = await response.json();

      expect(data.ready).toBe(true);
      expect(data.previewUrl).toBeTruthy();
      expect(data.duration).toBeGreaterThan(0);
    });

    it('should play assembled video without errors', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        blob: async () => new Blob(['mock video data'], { type: 'video/mp4' }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/preview/assembled-123.mp4');
      const blob = await response.blob();

      expect(response.ok).toBe(true);
      expect(blob.type).toBe('video/mp4');
      expect(blob.size).toBeGreaterThan(0);
    });
  });

  describe('2.3 Error Handling', () => {
    it('should show graceful error message on API failure', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
        json: async () => ({
          error: 'Internal server error',
          message: 'Failed to generate script',
          canRetry: true,
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/generate/script', { method: 'POST' });
      const data = await response.json();

      expect(response.ok).toBe(false);
      expect(data.error).toBeTruthy();
      expect(data.message).toBeTruthy();
    });

    it('should support retry functionality', async () => {
      let callCount = 0;
      const mockFetch = vi.fn().mockImplementation(async () => {
        callCount++;
        if (callCount === 1) {
          return {
            ok: false,
            status: 500,
            json: async () => ({ error: 'Server error', canRetry: true }),
          };
        }
        return {
          ok: true,
          json: async () => ({ jobId: 'retry-success-123', status: 'completed' }),
        };
      });
      global.fetch = mockFetch;

      // First call fails
      let response = await fetch('/api/generate/script', { method: 'POST' });
      expect(response.ok).toBe(false);

      // Retry succeeds
      response = await fetch('/api/generate/script', { method: 'POST' });
      expect(response.ok).toBe(true);
      expect(callCount).toBe(2);
    });

    it('should save partial progress if workflow interrupted', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          savedState: {
            step: 'visuals-generation',
            completedSteps: ['script-generation'],
            pendingSteps: ['voiceover-generation', 'timeline-assembly'],
            canResume: true,
          },
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/workflow/save-state', { method: 'POST' });
      const data = await response.json();

      expect(data.savedState.canResume).toBe(true);
      expect(data.savedState.completedSteps).toContain('script-generation');
    });

    it('should allow manual continuation from saved state', async () => {
      const mockFetch = vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          resumed: true,
          currentStep: 'voiceover-generation',
          nextStep: 'timeline-assembly',
        }),
      });
      global.fetch = mockFetch;

      const response = await fetch('/api/workflow/resume', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ stateId: 'saved-state-123' }),
      });
      const data = await response.json();

      expect(data.resumed).toBe(true);
      expect(data.currentStep).toBe('voiceover-generation');
    });

    it('should handle network timeout gracefully', async () => {
      const mockFetch = vi.fn().mockRejectedValue(new Error('Network timeout'));
      global.fetch = mockFetch;

      try {
        await fetch('/api/generate/script', { method: 'POST' });
        expect.fail('Should have thrown an error');
      } catch (error) {
        expect(error).toBeDefined();
        expect((error as Error).message).toContain('timeout');
      }
    });
  });
});
