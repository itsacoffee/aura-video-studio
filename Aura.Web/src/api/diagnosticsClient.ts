import { apiClient } from './typedClient';

export interface ErrorLogEntry {
  timestamp: string;
  category: 'User' | 'System' | 'Provider' | 'Network' | 'Application';
  exceptionType: string;
  message: string;
  stackTrace?: string;
  correlationId: string;
  errorCode?: string;
  userMessage?: string;
  suggestedActions?: string[];
  isTransient: boolean;
  severity: 'Information' | 'Warning' | 'Error' | 'Critical';
}

export interface ErrorStatistics {
  totalUniqueErrors: number;
  totalOccurrences: number;
  mostFrequentError?: {
    signature: string;
    exceptionType: string;
    message: string;
    count: number;
    firstSeen: string;
    lastSeen: string;
  };
  errorTypes: Record<string, number>;
}

export interface RecoveryGuide {
  correlationId: string;
  timestamp: string;
  exceptionType: string;
  errorCode?: string;
  userFriendlyMessage: string;
  isTransient: boolean;
  severity: 'Information' | 'Warning' | 'Error' | 'Critical';
  manualActions: string[];
  automatedRecovery?: {
    name: string;
    description: string;
    estimatedTimeSeconds: number;
  };
  troubleshootingSteps: Array<{
    step: number;
    title: string;
    description: string;
    actions: string[];
  }>;
  documentationLinks: Array<{
    title: string;
    url: string;
    description: string;
  }>;
}

export interface RecoveryAttemptResult {
  correlationId: string;
  startTime: string;
  endTime: string;
  success: boolean;
  message?: string;
}

/**
 * Get recent errors from the error log
 */
export async function getRecentErrors(count = 50, category?: string): Promise<ErrorLogEntry[]> {
  const params = new URLSearchParams({ count: count.toString() });
  if (category) {
    params.append('category', category);
  }

  const response = await apiClient.get<{ errors: ErrorLogEntry[] }>(
    `/api/diagnostics/errors?${params}`
  );
  return response.errors;
}

/**
 * Search errors by correlation ID
 */
export async function searchErrorsByCorrelationId(correlationId: string): Promise<ErrorLogEntry[]> {
  const response = await apiClient.get<{ errors: ErrorLogEntry[] }>(
    `/api/diagnostics/errors/by-correlation/${correlationId}`
  );
  return response.errors;
}

/**
 * Get error statistics
 */
export async function getErrorStatistics(hoursAgo?: number): Promise<ErrorStatistics> {
  const params = hoursAgo ? `?hoursAgo=${hoursAgo}` : '';
  return await apiClient.get(`/api/diagnostics/errors/stats${params}`);
}

/**
 * Get aggregated errors
 */
export async function getAggregatedErrors(hoursAgo?: number, limit = 50) {
  const params = new URLSearchParams({ limit: limit.toString() });
  if (hoursAgo) {
    params.append('hoursAgo', hoursAgo.toString());
  }

  return await apiClient.get(`/api/diagnostics/errors/aggregated?${params}`);
}

/**
 * Export diagnostic information
 */
export async function exportDiagnostics(hoursAgo?: number): Promise<Blob> {
  const params = hoursAgo ? `?hoursAgo=${hoursAgo}` : '';
  const response = await fetch(`/api/diagnostics/export${params}`, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error('Failed to export diagnostics');
  }

  return await response.blob();
}

/**
 * Get recovery guidance for an error
 */
export async function getRecoveryGuide(
  exceptionType: string,
  message: string,
  correlationId?: string
): Promise<RecoveryGuide> {
  return await apiClient.post('/api/diagnostics/recovery-guide', {
    exceptionType,
    message,
    correlationId,
  });
}

/**
 * Attempt automated recovery
 */
export async function attemptRecovery(
  exceptionType: string,
  message: string,
  correlationId?: string
): Promise<RecoveryAttemptResult> {
  return await apiClient.post('/api/diagnostics/recovery-attempt', {
    exceptionType,
    message,
    correlationId,
  });
}

/**
 * Clean up old error logs
 */
export async function cleanupOldErrors(daysOld = 30) {
  return await apiClient.delete(`/api/diagnostics/errors/cleanup?daysOld=${daysOld}`);
}

/**
 * Check health of error handling system
 */
export async function checkErrorHandlingHealth() {
  return await apiClient.get('/api/diagnostics/health');
}
