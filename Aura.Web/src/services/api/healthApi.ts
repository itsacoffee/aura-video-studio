import { apiUrl } from '../../config/api';
import type { HealthSummaryResponse, HealthDetailsResponse } from '../../types/api-v1';
import { get } from './apiClient';

export interface HealthCheckEntry {
  name: string;
  status: 'healthy' | 'degraded' | 'unhealthy';
  description?: string;
  duration: number;
  data?: Record<string, any>;
  tags?: string[];
  exception?: string;
}

export interface HealthCheckResponse {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: string;
  duration?: number;
  environment?: string;
  version?: string;
  checks: HealthCheckEntry[];
  tag?: string;
}

/**
 * Get high-level health summary (liveness probe)
 */
export async function getHealthLive(): Promise<{ status: string; timestamp: string }> {
  return get<{ status: string; timestamp: string }>(`${apiUrl}/health/live`);
}

/**
 * Get readiness status (all ready checks)
 */
export async function getHealthReady(): Promise<HealthCheckResponse> {
  return get<HealthCheckResponse>(`${apiUrl}/health/ready`);
}

/**
 * Get detailed health information for all checks
 */
export async function getHealthDetails(): Promise<HealthCheckResponse> {
  return get<HealthCheckResponse>(`${apiUrl}/health`);
}

/**
 * Get health checks by tag
 */
export async function getHealthByTag(tag: string): Promise<HealthCheckResponse> {
  return get<HealthCheckResponse>(`${apiUrl}/health/${tag}`);
}

/**
 * @deprecated Use getHealthDetails instead
 */
export async function getHealthSummary(): Promise<HealthSummaryResponse> {
  // Fallback for backward compatibility
  return getHealthDetails() as any;
}
