export interface EngineManifestEntry {
  id: string;
  name: string;
  version: string;
  description?: string;
  sizeBytes: number;
  defaultPort?: number;
  licenseUrl?: string;
  requiredVRAMGB?: number;
  isInstalled: boolean;
  installPath: string;
  // Gating information
  isGated?: boolean;
  canInstall?: boolean;
  gatingReason?: string;
  vramTooltip?: string;
}

export interface EngineStatus {
  engineId: string;
  name: string;
  status: 'not_installed' | 'installed' | 'running';
  installedVersion?: string;
  isInstalled: boolean;
  isRunning: boolean;
  isHealthy: boolean;
  port?: number;
  health?: 'healthy' | 'unreachable' | null;
  processId?: number;
  logsPath?: string;
  messages: string[];
}

export interface EngineInstallProgress {
  engineId: string;
  phase: 'downloading' | 'extracting' | 'verifying' | 'complete' | 'error';
  bytesProcessed: number;
  totalBytes: number;
  percentComplete: number;
  message?: string;
}

export interface EngineVerificationResult {
  engineId: string;
  isValid: boolean;
  status: string;
  missingFiles: string[];
  issues: string[];
}

export interface InstallRequest {
  engineId: string;
  version?: string;
  port?: number;
}

export interface StartRequest {
  engineId: string;
  port?: number;
  args?: string;
}

export interface EngineActionRequest {
  engineId: string;
}
