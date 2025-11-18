/**
 * API client for Provider Profile Lock management
 */

import axios from 'axios';
import type {
  SetProfileLockRequest,
  UnlockProfileLockRequest,
  ProfileLockStatus,
  OfflineCompatibilityResponse,
  ValidateProviderRequest,
  ValidateProviderResponse,
  ProviderProfileLock
} from '../types/profileLock';
import { env } from '@/config/env';

const API_BASE_URL = env.apiBaseUrl;

/**
 * Get the current profile lock status for a job
 */
export async function getProfileLockStatus(jobId?: string): Promise<ProfileLockStatus> {
  const url = `${API_BASE_URL}/api/provider-lock/status`;
  const params = jobId ? { jobId } : {};
  
  const response = await axios.get<ProfileLockStatus>(url, { params });
  return response.data;
}

/**
 * Set a provider profile lock for a job
 */
export async function setProfileLock(
  request: SetProfileLockRequest
): Promise<ProviderProfileLock> {
  const url = `${API_BASE_URL}/api/provider-lock/set`;
  
  const response = await axios.post<ProviderProfileLock>(url, request);
  return response.data;
}

/**
 * Unlock a provider profile lock, allowing provider switching
 */
export async function unlockProfileLock(
  request: UnlockProfileLockRequest
): Promise<{ jobId: string; unlocked: boolean; reason?: string; correlationId: string }> {
  const url = `${API_BASE_URL}/api/provider-lock/unlock`;
  
  const response = await axios.post(url, request);
  return response.data;
}

/**
 * Check if a provider is compatible with offline mode
 */
export async function checkOfflineCompatibility(
  providerName: string
): Promise<OfflineCompatibilityResponse> {
  const url = `${API_BASE_URL}/api/provider-lock/offline-compatible`;
  
  const response = await axios.get<OfflineCompatibilityResponse>(url, {
    params: { providerName }
  });
  return response.data;
}

/**
 * Validate a provider request against the active profile lock
 */
export async function validateProvider(
  request: ValidateProviderRequest
): Promise<ValidateProviderResponse> {
  const url = `${API_BASE_URL}/api/provider-lock/validate`;
  
  const response = await axios.post<ValidateProviderResponse>(url, request);
  return response.data;
}

/**
 * Remove a profile lock completely
 */
export async function removeProfileLock(
  jobId: string,
  isSessionLevel: boolean = true
): Promise<{ jobId: string; removed: boolean; correlationId: string }> {
  const url = `${API_BASE_URL}/api/provider-lock/${jobId}`;
  
  const response = await axios.delete(url, {
    params: { isSessionLevel }
  });
  return response.data;
}

/**
 * Helper: Create a profile lock request for a specific provider
 */
export function createProfileLockRequest(
  jobId: string,
  providerName: string,
  providerType: string,
  options?: {
    offlineModeEnabled?: boolean;
    applicableStages?: string[];
    isSessionLevel?: boolean;
    allowManualFallback?: boolean;
    maxWaitSeconds?: number;
  }
): SetProfileLockRequest {
  return {
    jobId,
    providerName,
    providerType,
    isEnabled: true,
    offlineModeEnabled: options?.offlineModeEnabled ?? false,
    applicableStages: options?.applicableStages,
    isSessionLevel: options?.isSessionLevel ?? true,
    metadata: {
      tags: [],
      source: 'User',
      allowManualFallback: options?.allowManualFallback ?? true,
      maxWaitBeforeFallbackSeconds: options?.maxWaitSeconds
    }
  };
}
