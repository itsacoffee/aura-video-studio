import { apiUrl } from '../../config/api';
import type { HealthSummaryResponse, HealthDetailsResponse } from '../../types/api-v1';
import { get } from './apiClient';

/**
 * Get high-level health summary
 */
export async function getHealthSummary(): Promise<HealthSummaryResponse> {
  return get<HealthSummaryResponse>(`${apiUrl}/health/summary`);
}

/**
 * Get detailed health information for all checks
 */
export async function getHealthDetails(): Promise<HealthDetailsResponse> {
  return get<HealthDetailsResponse>(`${apiUrl}/health/details`);
}
