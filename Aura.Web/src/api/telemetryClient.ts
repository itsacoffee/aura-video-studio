/**
 * API client for RunTelemetry endpoints
 */

import apiClient from '@/services/api/apiClient';
import type { RunTelemetryCollection, TelemetrySchemaInfo } from '@/types/telemetry';

/**
 * Get telemetry data for a specific job
 */
export async function getJobTelemetry(jobId: string): Promise<RunTelemetryCollection> {
  const response = await apiClient.get<RunTelemetryCollection>(`/api/telemetry/${jobId}`);
  return response.data;
}

/**
 * Get telemetry schema information
 */
export async function getTelemetrySchema(): Promise<TelemetrySchemaInfo> {
  const response = await apiClient.get<TelemetrySchemaInfo>('/api/telemetry/schema');
  return response.data;
}

/**
 * Check if telemetry is available for a job
 */
export async function hasTelemetry(jobId: string): Promise<boolean> {
  try {
    await getJobTelemetry(jobId);
    return true;
  } catch (error: unknown) {
    return false;
  }
}
