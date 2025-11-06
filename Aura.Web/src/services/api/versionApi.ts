/**
 * Version API service
 * Provides methods to fetch application version information
 */

import type { VersionInfo } from '../../types/api-v1';
import apiClient from './apiClient';

/**
 * Get application version information
 */
export async function getVersion(): Promise<VersionInfo> {
  const response = await apiClient.get<VersionInfo>('/api/version');
  return response.data;
}
