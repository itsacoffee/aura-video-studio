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
  githubRepo?: string; // e.g., "BtbN/FFmpeg-Builds"
  assetPattern?: string; // Pattern to match GitHub release assets
  // Gating information
  isGated?: boolean;
  canInstall?: boolean;
  canAutoStart?: boolean; // New: can the engine auto-start with current hardware
  gatingReason?: string;
  vramTooltip?: string;
  icon?: string;
  tags?: string[];
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
  installPath?: string;
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

export interface EngineInstance {
  id: string;
  engineId: string;
  name: string;
  version: string;
  mode: 'Managed' | 'External';
  installPath: string;
  executablePath?: string;
  port?: number;
  status: 'not_installed' | 'installed' | 'running';
  isRunning: boolean;
  isHealthy: boolean;
  notes?: string;
  healthCheckUrl?: string;
}

export interface AttachEngineRequest {
  engineId: string;
  installPath: string;
  executablePath?: string;
  port?: number;
  healthCheckUrl?: string;
  notes?: string;
}

export interface ReconfigureEngineRequest {
  instanceId: string;
  installPath?: string;
  executablePath?: string;
  port?: number;
  healthCheckUrl?: string;
  notes?: string;
}

export interface EngineDiagnostics {
  availableDiskSpaceBytes?: number;
  checksumStatus?: string;
  expectedSha256?: string;
  actualSha256?: string;
  failedUrl?: string;
  pathWritable?: boolean;
  error?: string;
  installPath?: string;
  isInstalled?: boolean;
  pathExists?: boolean;
  lastError?: string;
  issues?: string[];
}
