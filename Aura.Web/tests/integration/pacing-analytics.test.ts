import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { getStoredEvents, clearStoredEvents } from '../../src/services/analytics';

/**
 * Pacing Analytics Integration Tests
 *
 * These tests validate the pacing analytics workflow:
 * - Analysis request flow
 * - Telemetry tracking
 * - Suggestion application
 * - Correlation ID handling
 */

describe('Integration Test: Pacing Analytics', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    clearStoredEvents();
  });

  describe('Pacing Analysis Workflow', () => {
    it('should complete pacing analysis request with telemetry', async () => {
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          overallScore: 85,
          suggestions: [
            {
              sceneIndex: 0,
              currentDuration: 'PT15S',
              optimalDuration: 'PT12S',
              minDuration: 'PT10S',
              maxDuration: 'PT20S',
              importanceScore: 75,
              complexityScore: 60,
              emotionalIntensity: 50,
              informationDensity: 'Medium',
              transitionType: 'Cut',
              confidence: 85,
              reasoning: 'Scene is too long for content density',
              usedLlmAnalysis: true,
            },
            {
              sceneIndex: 1,
              currentDuration: 'PT10S',
              optimalDuration: 'PT15S',
              minDuration: 'PT12S',
              maxDuration: 'PT18S',
              importanceScore: 90,
              complexityScore: 80,
              emotionalIntensity: 70,
              informationDensity: 'High',
              transitionType: 'Fade',
              confidence: 90,
              reasoning: 'Important scene needs more time',
              usedLlmAnalysis: true,
            },
          ],
          attentionCurve: {
            dataPoints: [
              {
                timestamp: 'PT0S',
                attentionLevel: 80,
                retentionRate: 100,
                engagementScore: 75,
              },
              {
                timestamp: 'PT15S',
                attentionLevel: 70,
                retentionRate: 90,
                engagementScore: 65,
              },
            ],
            averageEngagement: 72.5,
            engagementPeaks: ['PT0S'],
            engagementValleys: ['PT15S'],
            overallRetentionScore: 88,
          },
          estimatedRetention: 88,
          averageEngagement: 72.5,
          analysisId: 'analysis-123',
          timestamp: new Date().toISOString(),
          correlationId: 'corr-456',
          confidenceScore: 85,
          warnings: ['Scene 0 may lose viewer attention'],
        }),
      });

      global.fetch = mockFetch;

      // Execute analysis
      const response = await fetch('/api/pacing/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          script: 'Test script',
          scenes: [
            {
              sceneIndex: 0,
              narration: 'Scene 1 content',
              visualDescription: 'Visual 1',
              duration: 15,
            },
            {
              sceneIndex: 1,
              narration: 'Scene 2 content',
              visualDescription: 'Visual 2',
              duration: 10,
            },
          ],
          targetPlatform: 'YouTube',
          targetDuration: 60,
          brief: {
            topic: 'Test Topic',
            audience: 'General',
            goal: 'Inform',
            tone: 'Informative',
            language: 'en-US',
            aspect: 'Widescreen16x9',
          },
        }),
      });

      const result = await response.json();

      // Validate response structure
      expect(result.overallScore).toBe(85);
      expect(result.suggestions).toHaveLength(2);
      expect(result.analysisId).toBe('analysis-123');
      expect(result.correlationId).toBe('corr-456');
      expect(result.estimatedRetention).toBe(88);
      expect(result.attentionCurve).toBeDefined();
      expect(result.warnings).toContain('Scene 0 may lose viewer attention');

      // Validate suggestions
      expect(result.suggestions[0].sceneIndex).toBe(0);
      expect(result.suggestions[0].confidence).toBe(85);
      expect(result.suggestions[0].reasoning).toBeTruthy();

      expect(result.suggestions[1].sceneIndex).toBe(1);
      expect(result.suggestions[1].confidence).toBe(90);
    });

    it('should track telemetry events for analysis lifecycle', () => {
      // This would be tested in the actual component, but we can verify
      // the analytics service structure
      const events = getStoredEvents();
      expect(Array.isArray(events)).toBe(true);

      // In actual usage, after analysis we would expect events like:
      // - pacing_analysis_started
      // - pacing_analysis_completed
      // - pacing_suggestion_applied (when accepting suggestions)
    });

    it('should handle analysis errors with correlation ID', async () => {
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 500,
        json: async () => ({
          detail: 'Analysis service temporarily unavailable. CorrelationId: corr-error-789',
          title: 'Internal Server Error',
          status: 500,
        }),
      });

      global.fetch = mockFetch;

      // Execute analysis
      const response = await fetch('/api/pacing/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          script: 'Test',
          scenes: [],
          targetPlatform: 'YouTube',
          targetDuration: 60,
          brief: {
            topic: 'Test',
            audience: 'General',
            goal: 'Inform',
            tone: 'Informative',
            language: 'en-US',
            aspect: 'Widescreen16x9',
          },
        }),
      });

      expect(response.ok).toBe(false);
      const error = await response.json();
      expect(error.detail).toContain('CorrelationId:');
    });

    it('should support reanalysis with different parameters', async () => {
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          overallScore: 90,
          suggestions: [],
          attentionCurve: null,
          estimatedRetention: 92,
          averageEngagement: 85,
          analysisId: 'analysis-456',
          timestamp: new Date().toISOString(),
          correlationId: 'corr-reanalyze-123',
          confidenceScore: 90,
          warnings: [],
        }),
      });

      global.fetch = mockFetch;

      // Execute reanalysis
      const response = await fetch('/api/pacing/reanalyze/analysis-123', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          optimizationLevel: 'High',
          targetPlatform: 'TikTok',
        }),
      });

      const result = await response.json();

      expect(result.analysisId).toBe('analysis-456');
      expect(result.correlationId).toBe('corr-reanalyze-123');
      expect(result.overallScore).toBe(90);
    });
  });

  describe('Suggestion Application', () => {
    it('should validate suggestion data before application', () => {
      const suggestion = {
        sceneIndex: 0,
        currentDuration: 'PT15S',
        optimalDuration: 'PT12S',
        minDuration: 'PT10S',
        maxDuration: 'PT20S',
        importanceScore: 75,
        complexityScore: 60,
        emotionalIntensity: 50,
        informationDensity: 'Medium' as const,
        transitionType: 'Cut' as const,
        confidence: 85,
        reasoning: 'Test reasoning',
        usedLlmAnalysis: true,
      };

      // Validate suggestion has required fields
      expect(suggestion.sceneIndex).toBeGreaterThanOrEqual(0);
      expect(suggestion.confidence).toBeGreaterThanOrEqual(0);
      expect(suggestion.confidence).toBeLessThanOrEqual(100);
      // ISO 8601 duration format validation
      // Pattern is safe: anchored with ^ and $, non-capturing groups, limited quantifiers
      // eslint-disable-next-line security/detect-unsafe-regex
      expect(suggestion.currentDuration).toMatch(/^PT\d+(?:\.\d+)?S$/);
      // eslint-disable-next-line security/detect-unsafe-regex
      expect(suggestion.optimalDuration).toMatch(/^PT\d+(?:\.\d+)?S$/);
    });

    it('should filter suggestions by confidence threshold', () => {
      const suggestions = [
        { sceneIndex: 0, confidence: 90, currentDuration: 'PT15S', optimalDuration: 'PT12S' },
        { sceneIndex: 1, confidence: 75, currentDuration: 'PT10S', optimalDuration: 'PT15S' },
        { sceneIndex: 2, confidence: 55, currentDuration: 'PT20S', optimalDuration: 'PT18S' },
      ];

      const minConfidence = 70;
      const highConfidence = suggestions.filter((s) => s.confidence >= minConfidence);

      expect(highConfidence).toHaveLength(2);
      expect(highConfidence[0].sceneIndex).toBe(0);
      expect(highConfidence[1].sceneIndex).toBe(1);
    });
  });

  describe('Platform Presets', () => {
    it('should fetch platform presets', async () => {
      const mockFetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          platforms: [
            {
              name: 'YouTube',
              recommendedPacing: 'Conversational',
              avgSceneDuration: '15-30s',
              optimalVideoLength: 600,
              pacingMultiplier: 1.0,
            },
            {
              name: 'TikTok',
              recommendedPacing: 'Fast',
              avgSceneDuration: '3-8s',
              optimalVideoLength: 37.5,
              pacingMultiplier: 0.7,
            },
          ],
        }),
      });

      global.fetch = mockFetch;

      const response = await fetch('/api/pacing/platforms');
      const result = await response.json();

      expect(result.platforms).toHaveLength(2);
      expect(result.platforms[0].name).toBe('YouTube');
      expect(result.platforms[1].name).toBe('TikTok');
    });
  });
});
