/**
 * Adapter to convert RunTelemetry v1 data to legacy cost tracking format
 * This allows Cost Analytics and Diagnostics to consume the unified telemetry contract
 */

import type {
  RunCostReport,
  StageCostBreakdown,
  OperationCostDetail,
  TokenUsageStatistics,
  CostOptimizationSuggestion,
} from '@/state/costTracking';
import type { RunTelemetryCollection } from '@/types/telemetry';

/**
 * Convert RunTelemetryCollection to RunCostReport format
 */
export function adaptTelemetryToRunCost(telemetry: RunTelemetryCollection): RunCostReport {
  const summary = telemetry.summary;
  const records = telemetry.records;

  const costByStage: Record<string, StageCostBreakdown> = {};

  if (summary?.cost_by_stage) {
    Object.entries(summary.cost_by_stage).forEach(([stage, cost]) => {
      const stageRecords = records.filter((r) => r.stage.toLowerCase() === stage.toLowerCase());
      const totalDuration = stageRecords.reduce((sum, r) => sum + r.latency_ms, 0);

      costByStage[stage] = {
        stageName: stage,
        cost: cost,
        percentageOfTotal: summary.total_cost > 0 ? (cost / summary.total_cost) * 100 : 0,
        durationSeconds: totalDuration / 1000,
        operationCount: stageRecords.length,
        providerName: stageRecords[0]?.provider || undefined,
      };
    });
  }

  const operations: OperationCostDetail[] = records.map((record) => ({
    timestamp: record.started_at,
    operationType: record.stage,
    providerName: record.provider || 'Unknown',
    cost: record.cost_estimate || 0,
    durationMs: record.latency_ms,
    tokensUsed: (record.tokens_in || 0) + (record.tokens_out || 0),
    cacheHit: record.cache_hit || false,
  }));

  const tokenStats: TokenUsageStatistics | undefined = summary
    ? {
        totalInputTokens: summary.total_tokens_in,
        totalOutputTokens: summary.total_tokens_out,
        totalTokens: summary.total_tokens_in + summary.total_tokens_out,
        operationCount: summary.total_operations,
        cacheHits: summary.cache_hits,
        cacheHitRate:
          summary.total_operations > 0 ? (summary.cache_hits / summary.total_operations) * 100 : 0,
        averageTokensPerOperation:
          summary.total_operations > 0
            ? (summary.total_tokens_in + summary.total_tokens_out) / summary.total_operations
            : 0,
        averageResponseTimeMs:
          summary.total_operations > 0 ? summary.total_latency_ms / summary.total_operations : 0,
        totalCost: summary.total_cost,
        costSavedByCache: 0,
      }
    : undefined;

  const optimizationSuggestions: CostOptimizationSuggestion[] = [];

  if (summary && summary.cache_hits === 0 && summary.total_operations > 0) {
    optimizationSuggestions.push({
      category: 'Caching',
      suggestion: 'Enable LLM response caching to reduce costs on repeated queries',
      estimatedSavings: summary.total_cost * 0.3,
      qualityImpact: 'None',
    });
  }

  if (summary && summary.total_retries > 5) {
    optimizationSuggestions.push({
      category: 'Reliability',
      suggestion: 'High retry count detected. Review provider reliability and error handling',
      estimatedSavings: summary.total_cost * 0.1,
      qualityImpact: 'None',
    });
  }

  const totalDurationSeconds =
    telemetry.collection_ended_at && telemetry.collection_started_at
      ? (new Date(telemetry.collection_ended_at).getTime() -
          new Date(telemetry.collection_started_at).getTime()) /
        1000
      : 0;

  return {
    jobId: telemetry.job_id,
    projectId: records[0]?.project_id || undefined,
    projectName: undefined,
    startedAt: telemetry.collection_started_at,
    completedAt: telemetry.collection_ended_at || undefined,
    durationSeconds: totalDurationSeconds,
    totalCost: summary?.total_cost || 0,
    currency: summary?.currency || 'USD',
    costByStage,
    costByProvider: summary?.operations_by_provider
      ? Object.entries(summary.operations_by_provider).reduce(
          (acc, [provider, _count]) => {
            const providerRecords = records.filter((r) => r.provider === provider);
            const cost = providerRecords.reduce((sum, r) => sum + (r.cost_estimate || 0), 0);
            acc[provider] = cost;
            return acc;
          },
          {} as Record<string, number>
        )
      : {},
    tokenStats,
    operations,
    optimizationSuggestions,
    withinBudget: true,
  };
}

/**
 * Generate diagnostics summary from telemetry
 */
export function generateDiagnosticsSummary(telemetry: RunTelemetryCollection) {
  const failedOperations = telemetry.records.filter((r) => r.result_status === 'error');
  const warnings = telemetry.records.filter((r) => r.result_status === 'warn');
  const summary = telemetry.summary;

  return {
    totalOperations: summary?.total_operations || 0,
    successfulOperations: summary?.successful_operations || 0,
    failedOperations: summary?.failed_operations || 0,
    warningCount: warnings.length,
    totalRetries: summary?.total_retries || 0,
    averageLatency:
      summary && summary.total_operations > 0
        ? summary.total_latency_ms / summary.total_operations
        : 0,
    failureDetails: failedOperations.map((op) => ({
      stage: op.stage,
      provider: op.provider || 'Unknown',
      errorCode: op.error_code || 'UNKNOWN',
      message: op.message || 'No error message',
      timestamp: op.started_at,
    })),
    warningDetails: warnings.map((op) => ({
      stage: op.stage,
      provider: op.provider || 'Unknown',
      message: op.message || 'No warning message',
      timestamp: op.started_at,
    })),
  };
}
