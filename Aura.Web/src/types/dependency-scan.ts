/**
 * Types for the Dependency Scanner API
 */

export type IssueSeverity = 'Info' | 'Warning' | 'Error';

export type IssueCategory =  
  | 'FFmpeg'
  | 'Network'
  | 'Provider'
  | 'Storage'
  | 'System'
  | 'Runtime';

export interface DependencyIssue {
  id: string;
  category: IssueCategory;
  severity: IssueSeverity;
  title: string;
  description: string;
  remediation: string;
  docsUrl?: string;
  relatedSettingKey?: string;
  actionId?: string;
  metadata?: Record<string, unknown>;
}

export interface GpuInfo {
  vendor: string;
  model: string;
  vramMb: number;
  supportsHardwareAcceleration: boolean;
}

export interface SystemInfo {
  platform: string;
  architecture: string;
  osVersion: string;
  cpuCores: number;
  totalMemoryMb: number;
  gpu?: GpuInfo;
}

export interface DependencyScanResult {
  scanTime: string;
  duration: string;
  systemInfo: SystemInfo;
  issues: DependencyIssue[];
  success: boolean;
  hasErrors: boolean;
  hasWarnings: boolean;
  correlationId?: string;
}

export interface ScanProgressEvent {
  event: 'started' | 'step' | 'issue' | 'completed' | 'error';
  message?: string;
  percentComplete?: number;
  issue?: DependencyIssue;
  scanTime?: string;
  duration?: string;
  issueCount?: number;
  hasErrors?: boolean;
  hasWarnings?: boolean;
  cached?: boolean;
  error?: string;
  correlationId?: string;
}
