/**
 * Tests for telemetry adapter functions
 */

import { describe, it, expect } from 'vitest';
import { adaptTelemetryToRunCost, generateDiagnosticsSummary } from '../telemetryAdapter';
import type { RunTelemetryCollection } from '@/types/telemetry';

describe('telemetryAdapter', () => {
  describe('adaptTelemetryToRunCost', () => {
    it('should convert telemetry to run cost report format', () => {
      const telemetry: RunTelemetryCollection = {
        version: '1.0',
        job_id: 'test-job-123',
        correlation_id: 'corr-456',
        collection_started_at: '2024-01-01T10:00:00Z',
        collection_ended_at: '2024-01-01T10:05:00Z',
        records: [
          {
            version: '1.0',
            job_id: 'test-job-123',
            correlation_id: 'corr-456',
            stage: 'script',
            model_id: 'gpt-4',
            provider: 'OpenAI',
            tokens_in: 100,
            tokens_out: 200,
            cache_hit: false,
            retries: 0,
            latency_ms: 2000,
            cost_estimate: 0.05,
            currency: 'USD',
            result_status: 'ok',
            started_at: '2024-01-01T10:00:00Z',
            ended_at: '2024-01-01T10:00:02Z',
          },
          {
            version: '1.0',
            job_id: 'test-job-123',
            correlation_id: 'corr-456',
            stage: 'tts',
            provider: 'ElevenLabs',
            cache_hit: true,
            retries: 0,
            latency_ms: 3000,
            cost_estimate: 0.03,
            currency: 'USD',
            result_status: 'ok',
            started_at: '2024-01-01T10:00:02Z',
            ended_at: '2024-01-01T10:00:05Z',
          },
        ],
        summary: {
          total_operations: 2,
          successful_operations: 2,
          failed_operations: 0,
          total_cost: 0.08,
          currency: 'USD',
          total_latency_ms: 5000,
          total_tokens_in: 100,
          total_tokens_out: 200,
          cache_hits: 1,
          total_retries: 0,
          cost_by_stage: {
            script: 0.05,
            tts: 0.03,
          },
          operations_by_provider: {
            OpenAI: 1,
            ElevenLabs: 1,
          },
        },
      };

      const result = adaptTelemetryToRunCost(telemetry);

      expect(result.jobId).toBe('test-job-123');
      expect(result.totalCost).toBe(0.08);
      expect(result.currency).toBe('USD');
      expect(result.durationSeconds).toBe(300);

      expect(result.costByStage).toHaveProperty('script');
      expect(result.costByStage.script.cost).toBe(0.05);
      expect(result.costByStage.script.operationCount).toBe(1);

      expect(result.costByProvider).toHaveProperty('OpenAI');
      expect(result.costByProvider.OpenAI).toBe(0.05);

      expect(result.tokenStats).toBeDefined();
      expect(result.tokenStats?.totalTokens).toBe(300);
      expect(result.tokenStats?.cacheHitRate).toBe(50);

      expect(result.operations).toHaveLength(2);
    });

    it('should generate optimization suggestions for no cache hits', () => {
      const telemetry: RunTelemetryCollection = {
        version: '1.0',
        job_id: 'test-job-123',
        correlation_id: 'corr-456',
        collection_started_at: '2024-01-01T10:00:00Z',
        collection_ended_at: '2024-01-01T10:05:00Z',
        records: [],
        summary: {
          total_operations: 5,
          successful_operations: 5,
          failed_operations: 0,
          total_cost: 1.0,
          currency: 'USD',
          total_latency_ms: 10000,
          total_tokens_in: 500,
          total_tokens_out: 1000,
          cache_hits: 0,
          total_retries: 0,
        },
      };

      const result = adaptTelemetryToRunCost(telemetry);

      expect(result.optimizationSuggestions.length).toBeGreaterThan(0);
      expect(result.optimizationSuggestions[0].category).toBe('Caching');
    });

    it('should generate suggestions for high retry count', () => {
      const telemetry: RunTelemetryCollection = {
        version: '1.0',
        job_id: 'test-job-123',
        correlation_id: 'corr-456',
        collection_started_at: '2024-01-01T10:00:00Z',
        collection_ended_at: '2024-01-01T10:05:00Z',
        records: [],
        summary: {
          total_operations: 5,
          successful_operations: 5,
          failed_operations: 0,
          total_cost: 1.0,
          currency: 'USD',
          total_latency_ms: 10000,
          total_tokens_in: 500,
          total_tokens_out: 1000,
          cache_hits: 2,
          total_retries: 10,
        },
      };

      const result = adaptTelemetryToRunCost(telemetry);

      const reliabilitySuggestion = result.optimizationSuggestions.find(
        (s) => s.category === 'Reliability'
      );
      expect(reliabilitySuggestion).toBeDefined();
    });
  });

  describe('generateDiagnosticsSummary', () => {
    it('should generate diagnostics summary from telemetry', () => {
      const telemetry: RunTelemetryCollection = {
        version: '1.0',
        job_id: 'test-job-123',
        correlation_id: 'corr-456',
        collection_started_at: '2024-01-01T10:00:00Z',
        records: [
          {
            version: '1.0',
            job_id: 'test-job-123',
            correlation_id: 'corr-456',
            stage: 'script',
            provider: 'OpenAI',
            retries: 0,
            latency_ms: 2000,
            result_status: 'ok',
            started_at: '2024-01-01T10:00:00Z',
            ended_at: '2024-01-01T10:00:02Z',
          },
          {
            version: '1.0',
            job_id: 'test-job-123',
            correlation_id: 'corr-456',
            stage: 'tts',
            provider: 'ElevenLabs',
            retries: 1,
            latency_ms: 3000,
            result_status: 'error',
            error_code: 'TTS_FAILED',
            message: 'Failed to synthesize audio',
            started_at: '2024-01-01T10:00:02Z',
            ended_at: '2024-01-01T10:00:05Z',
          },
          {
            version: '1.0',
            job_id: 'test-job-123',
            correlation_id: 'corr-456',
            stage: 'render',
            provider: 'FFmpeg',
            retries: 0,
            latency_ms: 1000,
            result_status: 'warn',
            message: 'Low quality detected',
            started_at: '2024-01-01T10:00:05Z',
            ended_at: '2024-01-01T10:00:06Z',
          },
        ],
        summary: {
          total_operations: 3,
          successful_operations: 1,
          failed_operations: 1,
          total_cost: 0.1,
          currency: 'USD',
          total_latency_ms: 6000,
          total_tokens_in: 100,
          total_tokens_out: 200,
          cache_hits: 0,
          total_retries: 1,
        },
      };

      const result = generateDiagnosticsSummary(telemetry);

      expect(result.totalOperations).toBe(3);
      expect(result.successfulOperations).toBe(1);
      expect(result.failedOperations).toBe(1);
      expect(result.warningCount).toBe(1);
      expect(result.totalRetries).toBe(1);
      expect(result.averageLatency).toBe(2000);

      expect(result.failureDetails).toHaveLength(1);
      expect(result.failureDetails[0].stage).toBe('tts');
      expect(result.failureDetails[0].errorCode).toBe('TTS_FAILED');

      expect(result.warningDetails).toHaveLength(1);
      expect(result.warningDetails[0].stage).toBe('render');
    });
  });
});
