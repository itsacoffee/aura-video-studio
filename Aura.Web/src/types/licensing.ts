/**
 * Licensing and provenance types for export functionality
 */

export interface AssetLicensingInfo {
  assetId: string;
  assetType: string;
  sceneIndex: number;
  name: string;
  source: string;
  licenseType: string;
  licenseUrl?: string;
  commercialUseAllowed: boolean;
  attributionRequired: boolean;
  attributionText?: string;
  creator?: string;
  creatorUrl?: string;
  sourceUrl?: string;
  filePath?: string;
  metadata?: Record<string, string>;
}

export interface LicensingSummary {
  totalAssets: number;
  assetsByType: Record<string, number>;
  assetsBySource: Record<string, number>;
  assetsByLicenseType: Record<string, number>;
  assetsRequiringAttribution: number;
  assetsWithCommercialRestrictions: number;
}

export interface ProjectLicensingManifest {
  projectId: string;
  projectName: string;
  generatedAt: string;
  assets: AssetLicensingInfo[];
  allCommercialUseAllowed: boolean;
  warnings: string[];
  missingLicensingInfo: string[];
  summary: LicensingSummary;
}

export interface GenerateLicensingManifestRequest {
  projectId: string;
  timelineData?: unknown;
}

export interface ExportLicensingManifestRequest {
  projectId: string;
  format: 'json' | 'csv' | 'html' | 'text';
}

export interface LicensingSignOffRequest {
  projectId: string;
  acknowledgedCommercialRestrictions: boolean;
  acknowledgedAttributionRequirements: boolean;
  acknowledgedWarnings: boolean;
  notes?: string;
}

export interface LicensingSignOffResponse {
  projectId: string;
  signedOffAt: string;
  message: string;
}

export interface LicensingExportResponse {
  format: string;
  content: string;
  filename: string;
  contentType: string;
}

export interface LicensingValidationResult {
  projectId: string;
  isValid: boolean;
  hasWarnings: boolean;
  hasMissingInfo: boolean;
  commercialUseAllowed: boolean;
  warnings: string[];
  missingInfo: string[];
}
