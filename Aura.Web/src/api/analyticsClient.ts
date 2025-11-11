/**
 * API client for local analytics and usage insights
 * Privacy-first: All data stays local
 */

import { typedFetch, typedApiClient } from './typedClient';

export interface UsageStatistics {
  totalOperations: number;
  successfulOperations: number;
  failedOperations: number;
  successRate: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalTokens: number;
  averageDurationMs: number;
  totalDurationMs: number;
  totalVideoDurationSeconds: number;
  totalScenes: number;
  providerBreakdown: Record<string, ProviderUsageStats>;
  featureBreakdown: Record<string, number>;
  retryRate: number;
}

export interface ProviderUsageStats {
  totalOperations: number;
  successfulOperations: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  averageDurationMs: number;
}

export interface CostStatistics {
  totalCost: number;
  totalInputCost: number;
  totalOutputCost: number;
  averageCostPerOperation: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  costPerProvider: Record<string, number>;
  costPerMonth: Record<string, number>;
  topModels: Record<string, number>;
  currency: string;
}

export interface PerformanceStatistics {
  totalOperations: number;
  successfulOperations: number;
  averageDurationMs: number;
  medianDurationMs: number;
  minDurationMs: number;
  maxDurationMs: number;
  averageCpuUsage?: number;
  averageMemoryUsageMB?: number;
  peakMemoryUsageMB?: number;
  operationBreakdown: Record<string, OperationPerformance>;
}

export interface OperationPerformance {
  count: number;
  averageDurationMs: number;
  minDurationMs: number;
  maxDurationMs: number;
  successRate: number;
}

export interface MonthlyBudgetStatus {
  yearMonth: string;
  totalCost: number;
  providerCosts: Record<string, number>;
  currency: string;
  daysInMonth: number;
  daysElapsed: number;
  projectedMonthlyTotal: number;
  analyticsEnabled: boolean;
}

export interface CostEstimate {
  provider: string;
  model: string;
  inputTokens: number;
  outputTokens: number;
  estimatedCost: number;
  currency: string;
}

export interface AnalyticsSettings {
  id: number;
  isEnabled: boolean;
  usageStatisticsRetentionDays: number;
  costTrackingRetentionDays: number;
  performanceMetricsRetentionDays: number;
  autoCleanupEnabled: boolean;
  cleanupHourUtc: number;
  trackSuccessOnly: boolean;
  collectHardwareMetrics: boolean;
  aggregateOldData: boolean;
  aggregationThresholdDays: number;
  maxDatabaseSizeMB: number;
  createdAt: string;
  updatedAt: string;
}

export interface DatabaseInfo {
  usageRecords: number;
  costRecords: number;
  performanceRecords: number;
  summaryRecords: number;
  totalRecords: number;
  estimatedSizeMB: number;
  oldestRecordDate?: string;
  maxSizeMB: number;
  usagePercent: number;
}

export interface AnalyticsSummary {
  id: number;
  periodType: string;
  periodId: string;
  periodStart: string;
  periodEnd: string;
  totalGenerations: number;
  successfulGenerations: number;
  failedGenerations: number;
  totalTokens: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalCostUSD: number;
  averageDurationMs: number;
  totalRenderingTimeMs: number;
  mostUsedProvider?: string;
  mostUsedModel?: string;
  mostUsedFeature?: string;
  totalVideoDurationSeconds: number;
  totalScenes: number;
  averageCpuUsage?: number;
  averageMemoryUsageMB?: number;
}

/**
 * Get usage statistics for a date range
 */
export async function getUsageStatistics(
  startDate?: Date,
  endDate?: Date,
  provider?: string,
  generationType?: string
): Promise<UsageStatistics> {
  const params = new URLSearchParams();
  if (startDate) params.append('startDate', startDate.toISOString());
  if (endDate) params.append('endDate', endDate.toISOString());
  if (provider) params.append('provider', provider);
  if (generationType) params.append('generationType', generationType);

  return typedFetch<UsageStatistics>(`/api/analytics/usage?${params}`);
}

/**
 * Get cost statistics for a date range
 */
export async function getCostStatistics(
  startDate?: Date,
  endDate?: Date,
  provider?: string
): Promise<CostStatistics> {
  const params = new URLSearchParams();
  if (startDate) params.append('startDate', startDate.toISOString());
  if (endDate) params.append('endDate', endDate.toISOString());
  if (provider) params.append('provider', provider);

  return typedFetch<CostStatistics>(`/api/analytics/costs?${params}`);
}

/**
 * Get performance statistics for a date range
 */
export async function getPerformanceStatistics(
  startDate?: Date,
  endDate?: Date,
  operationType?: string
): Promise<PerformanceStatistics> {
  const params = new URLSearchParams();
  if (startDate) params.append('startDate', startDate.toISOString());
  if (endDate) params.append('endDate', endDate.toISOString());
  if (operationType) params.append('operationType', operationType);

  return typedFetch<PerformanceStatistics>(`/api/analytics/performance?${params}`);
}

/**
 * Get aggregated summaries
 */
export async function getAnalyticsSummaries(
  periodType: 'daily' | 'weekly' | 'monthly' = 'daily',
  limit: number = 30
): Promise<AnalyticsSummary[]> {
  const params = new URLSearchParams();
  params.append('periodType', periodType);
  params.append('limit', limit.toString());

  return typedFetch<AnalyticsSummary[]>(`/api/analytics/summaries?${params}`);
}

/**
 * Get current month's budget status
 */
export async function getCurrentMonthBudget(): Promise<MonthlyBudgetStatus> {
  return typedFetch<MonthlyBudgetStatus>('/api/analytics/costs/current-month');
}

/**
 * Estimate cost for a planned operation
 */
export async function estimateCost(
  provider: string,
  model: string,
  inputTokens: number,
  outputTokens: number
): Promise<CostEstimate> {
  return typedApiClient.post<CostEstimate>('/api/analytics/costs/estimate', {
    provider,
    model,
    inputTokens,
    outputTokens,
  });
}

/**
 * Get analytics settings
 */
export async function getAnalyticsSettings(): Promise<AnalyticsSettings> {
  return typedFetch<AnalyticsSettings>('/api/analytics/settings');
}

/**
 * Update analytics settings
 */
export async function updateAnalyticsSettings(
  settings: Partial<AnalyticsSettings>
): Promise<AnalyticsSettings> {
  return typedApiClient.put<AnalyticsSettings>('/api/analytics/settings', settings);
}

/**
 * Get database info
 */
export async function getDatabaseInfo(): Promise<DatabaseInfo> {
  return typedFetch<DatabaseInfo>('/api/analytics/database/info');
}

/**
 * Trigger manual cleanup
 */
export async function triggerCleanup(): Promise<{ message: string }> {
  return typedFetch<{ message: string }>('/api/analytics/cleanup', {
    method: 'POST',
  });
}

/**
 * Clear all analytics data
 */
export async function clearAllData(): Promise<{ message: string }> {
  return typedFetch<{ message: string }>('/api/analytics/data', {
    method: 'DELETE',
  });
}

/**
 * Export analytics data
 */
export async function exportAnalyticsData(
  startDate?: Date,
  endDate?: Date,
  format: 'json' | 'csv' = 'json'
): Promise<Blob> {
  const params = new URLSearchParams();
  if (startDate) params.append('startDate', startDate.toISOString());
  if (endDate) params.append('endDate', endDate.toISOString());
  params.append('format', format);

  const response = await fetch(`/api/analytics/export?${params}`);
  if (!response.ok) {
    throw new Error(`Failed to export data: ${response.statusText}`);
  }
  return response.blob();
}
