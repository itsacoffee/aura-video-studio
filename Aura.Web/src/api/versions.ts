import { get, post, patch, del } from '@/services/api/apiClient';
import type {
  VersionListResponse,
  VersionResponse,
  VersionDetailResponse,
  CreateSnapshotRequest,
  RestoreVersionRequest,
  UpdateVersionRequest,
  VersionComparisonResponse,
  StorageUsageResponse,
} from '@/types/api-v1';

/**
 * Get all versions for a project
 */
export async function getVersions(projectId: string): Promise<VersionListResponse> {
  return get<VersionListResponse>(`/api/projects/${projectId}/versions`);
}

/**
 * Get a specific version by ID
 */
export async function getVersion(
  projectId: string,
  versionId: string
): Promise<VersionDetailResponse> {
  return get<VersionDetailResponse>(`/api/projects/${projectId}/versions/${versionId}`);
}

/**
 * Create a manual snapshot
 */
export async function createSnapshot(request: CreateSnapshotRequest): Promise<VersionResponse> {
  return post<VersionResponse>(`/api/projects/${request.projectId}/versions`, request);
}

/**
 * Restore a version
 */
export async function restoreVersion(
  request: RestoreVersionRequest
): Promise<{ success: boolean; message: string }> {
  return post<{ success: boolean; message: string }>(
    `/api/projects/${request.projectId}/versions/restore`,
    request
  );
}

/**
 * Update version metadata
 */
export async function updateVersion(
  projectId: string,
  versionId: string,
  request: UpdateVersionRequest
): Promise<void> {
  await patch<void>(`/api/projects/${projectId}/versions/${versionId}`, request);
}

/**
 * Delete a version
 */
export async function deleteVersion(projectId: string, versionId: string): Promise<void> {
  await del<void>(`/api/projects/${projectId}/versions/${versionId}`);
}

/**
 * Compare two versions
 */
export async function compareVersions(
  projectId: string,
  version1Id: string,
  version2Id: string
): Promise<VersionComparisonResponse> {
  return get<VersionComparisonResponse>(
    `/api/projects/${projectId}/versions/compare?version1Id=${version1Id}&version2Id=${version2Id}`
  );
}

/**
 * Get storage usage
 */
export async function getStorageUsage(projectId: string): Promise<StorageUsageResponse> {
  return get<StorageUsageResponse>(`/api/projects/${projectId}/versions/storage`);
}

/**
 * Trigger manual autosave
 */
export async function triggerAutosave(
  projectId: string
): Promise<{ success: boolean; message: string }> {
  return post<{ success: boolean; message: string }>(
    `/api/projects/${projectId}/versions/autosave`,
    {}
  );
}
