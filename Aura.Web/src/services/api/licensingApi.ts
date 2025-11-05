import apiClient from './apiClient';
import type {
  ProjectLicensingManifest,
  GenerateLicensingManifestRequest,
  ExportLicensingManifestRequest,
  LicensingSignOffRequest,
  LicensingSignOffResponse,
  LicensingExportResponse,
  LicensingValidationResult,
} from '../../types/licensing';

/**
 * API client for licensing and provenance operations
 */

/**
 * Generate licensing manifest for a project
 */
export async function generateLicensingManifest(
  request: GenerateLicensingManifestRequest
): Promise<ProjectLicensingManifest> {
  const response = await apiClient.post<ProjectLicensingManifest>(
    '/api/licensing/manifest/generate',
    request
  );
  return response.data;
}

/**
 * Get existing licensing manifest for a project
 */
export async function getLicensingManifest(
  projectId: string
): Promise<ProjectLicensingManifest> {
  const response = await apiClient.get<ProjectLicensingManifest>(
    `/api/licensing/manifest/${projectId}`
  );
  return response.data;
}

/**
 * Export licensing manifest in specified format
 */
export async function exportLicensingManifest(
  request: ExportLicensingManifestRequest
): Promise<LicensingExportResponse> {
  const response = await apiClient.post<LicensingExportResponse>(
    '/api/licensing/manifest/export',
    request
  );
  return response.data;
}

/**
 * Download licensing manifest file
 */
export async function downloadLicensingManifest(
  projectId: string,
  format: 'json' | 'csv' | 'html' | 'text' = 'json'
): Promise<Blob> {
  const response = await apiClient.get<Blob>(
    `/api/licensing/manifest/${projectId}/download`,
    {
      params: { format },
      responseType: 'blob',
    }
  );
  return response.data;
}

/**
 * Record licensing sign-off
 */
export async function recordLicensingSignOff(
  request: LicensingSignOffRequest
): Promise<LicensingSignOffResponse> {
  const response = await apiClient.post<LicensingSignOffResponse>(
    '/api/licensing/signoff',
    request
  );
  return response.data;
}

/**
 * Validate licensing manifest
 */
export async function validateLicensingManifest(
  projectId: string
): Promise<LicensingValidationResult> {
  const response = await apiClient.get<LicensingValidationResult>(
    `/api/licensing/manifest/${projectId}/validate`
  );
  return response.data;
}

/**
 * Download manifest as file and trigger browser download
 */
export function downloadManifestFile(
  content: string,
  filename: string,
  contentType: string
): void {
  const blob = new Blob([content], { type: contentType });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
