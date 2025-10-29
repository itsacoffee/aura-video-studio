/**
 * Dependency Checker Service
 *
 * This service provides programmatic API access to check system dependencies
 * for Aura Video Studio, including FFmpeg, Python, pip packages, GPU detection,
 * and filesystem validation.
 */

export interface DependencyStatus {
  installed: boolean;
  version?: string;
  path?: string;
  fullVersion?: string;
  canAutoInstall?: boolean;
  error?: string;
}

export interface PipPackageStatus {
  installed: boolean;
  version?: string;
  required: boolean;
}

export interface GpuStatus {
  available: boolean;
  vendor?: string;
  model?: string;
  vram?: number;
  cudaAvailable?: boolean;
}

export interface ServiceStatus {
  reachable: boolean;
  latency?: number;
  error?: string;
}

export interface DependencyCheckResult {
  ffmpeg?: DependencyStatus;
  python?: DependencyStatus;
  pipPackages?: Record<string, PipPackageStatus>;
  gpu?: GpuStatus;
  services?: Record<string, ServiceStatus>;
  lastCheck?: string;
}

export interface InstallProgress {
  jobId: string;
  status: 'queued' | 'in_progress' | 'completed' | 'failed';
  progress: number;
  message: string;
  eta?: number;
  result?: DependencyStatus;
  error?: string;
}

export interface PathValidationResult {
  valid: boolean;
  version?: string;
  path?: string;
  error?: string;
}

/**
 * Main dependency checker service class
 */
export class DependencyCheckerService {
  private baseUrl = '/api';

  /**
   * Check all dependencies
   * @returns Comprehensive dependency status
   */
  async checkAll(): Promise<DependencyCheckResult> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/check`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      return await response.json();
    } catch (error) {
      console.error('Failed to check dependencies:', error);
      throw error;
    }
  }

  /**
   * Check FFmpeg availability and version
   * @returns FFmpeg status
   */
  async checkFFmpeg(): Promise<DependencyStatus> {
    const result = await this.checkAll();
    return (
      result.ffmpeg || {
        installed: false,
        canAutoInstall: true,
        error: 'FFmpeg status unavailable',
      }
    );
  }

  /**
   * Check Python installation
   * @returns Python status
   */
  async checkPython(): Promise<DependencyStatus> {
    const result = await this.checkAll();
    return (
      result.python || {
        installed: false,
        error: 'Python status unavailable',
      }
    );
  }

  /**
   * Check installed pip packages
   * @returns Map of package name to status
   */
  async checkPipPackages(): Promise<Record<string, PipPackageStatus>> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/pip-packages`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      const data = await response.json();
      return data.pipPackages || {};
    } catch (error) {
      console.error('Failed to check pip packages:', error);
      return {};
    }
  }

  /**
   * Check GPU availability and capabilities
   * @returns GPU status
   */
  async checkGPU(): Promise<GpuStatus> {
    try {
      const response = await fetch(`${this.baseUrl}/hardware/probe`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      const data = await response.json();
      return data.gpu || { available: false };
    } catch (error) {
      console.error('Failed to check GPU:', error);
      return { available: false };
    }
  }

  /**
   * Test AI service endpoints
   * @returns Map of service name to reachability status
   */
  async testServices(): Promise<Record<string, ServiceStatus>> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/test-services`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      const data = await response.json();
      return data.services || {};
    } catch (error) {
      console.error('Failed to test services:', error);
      return {};
    }
  }

  /**
   * Force a fresh dependency rescan
   * @returns Updated dependency status
   */
  async rescan(): Promise<DependencyCheckResult> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/rescan`, {
        method: 'POST',
      });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      return await response.json();
    } catch (error) {
      console.error('Failed to rescan dependencies:', error);
      throw error;
    }
  }

  /**
   * Trigger auto-installation of a dependency
   * @param dependency Name of dependency to install (e.g., 'ffmpeg')
   * @returns Installation job details
   */
  async install(dependency: string): Promise<InstallProgress> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/install/${dependency}`, {
        method: 'POST',
      });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      return await response.json();
    } catch (error) {
      console.error(`Failed to install ${dependency}:`, error);
      throw error;
    }
  }

  /**
   * Check installation progress
   * @param jobId Installation job ID
   * @returns Current installation status
   */
  async getInstallStatus(jobId: string): Promise<InstallProgress> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/install/status/${jobId}`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      return await response.json();
    } catch (error) {
      console.error(`Failed to get install status for ${jobId}:`, error);
      throw error;
    }
  }

  /**
   * Poll installation status until completion
   * @param jobId Installation job ID
   * @param onProgress Optional progress callback
   * @returns Final installation result
   */
  async waitForInstallation(
    jobId: string,
    onProgress?: (progress: InstallProgress) => void
  ): Promise<InstallProgress> {
    const pollInterval = 1000; // 1 second
    const maxAttempts = 300; // 5 minutes max

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const status = await this.getInstallStatus(jobId);

      if (onProgress) {
        onProgress(status);
      }

      if (status.status === 'completed' || status.status === 'failed') {
        return status;
      }

      // Wait before next poll
      await new Promise((resolve) => setTimeout(resolve, pollInterval));
    }

    throw new Error('Installation timeout - exceeded maximum wait time');
  }

  /**
   * Validate a custom path for a dependency
   * @param dependency Dependency name (e.g., 'ffmpeg')
   * @param path Custom path to validate
   * @returns Validation result
   */
  async validatePath(dependency: string, path: string): Promise<PathValidationResult> {
    try {
      const response = await fetch(`${this.baseUrl}/dependencies/validate-path`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ dependency, path }),
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Failed to validate path for ${dependency}:`, error);
      return {
        valid: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Get cached dependency status from localStorage
   * @returns Cached status or null if not available
   */
  getCachedStatus(): DependencyCheckResult | null {
    try {
      const cached = localStorage.getItem('dependencyStatus');
      if (cached) {
        return JSON.parse(cached);
      }
    } catch (error) {
      console.error('Failed to get cached dependency status:', error);
    }
    return null;
  }

  /**
   * Cache dependency status in localStorage
   * @param status Dependency status to cache
   */
  cacheStatus(status: DependencyCheckResult): void {
    try {
      status.lastCheck = new Date().toISOString();
      localStorage.setItem('dependencyStatus', JSON.stringify(status));
    } catch (error) {
      console.error('Failed to cache dependency status:', error);
    }
  }

  /**
   * Check if cached status is stale
   * @param maxAgeMinutes Maximum age in minutes before considering stale
   * @returns True if cache is stale or unavailable
   */
  isCacheStale(maxAgeMinutes = 60): boolean {
    const cached = this.getCachedStatus();
    if (!cached || !cached.lastCheck) {
      return true;
    }

    const lastCheckTime = new Date(cached.lastCheck).getTime();
    const now = Date.now();
    const ageMinutes = (now - lastCheckTime) / (1000 * 60);

    return ageMinutes > maxAgeMinutes;
  }

  /**
   * Get dependency status, using cache if available and fresh
   * @param forceRefresh Force a fresh check even if cache is valid
   * @returns Dependency status
   */
  async getStatus(forceRefresh = false): Promise<DependencyCheckResult> {
    if (!forceRefresh && !this.isCacheStale()) {
      const cached = this.getCachedStatus();
      if (cached) {
        return cached;
      }
    }

    const status = await this.checkAll();
    this.cacheStatus(status);
    return status;
  }
}

/**
 * Singleton instance
 */
export const dependencyChecker = new DependencyCheckerService();

/**
 * Utility functions
 */

/**
 * Check if system meets minimum requirements
 * @param status Dependency check result
 * @returns True if minimum requirements are met
 */
export function meetsMinimumRequirements(status: DependencyCheckResult): boolean {
  // At minimum, FFmpeg must be available for core video functionality
  return status.ffmpeg?.installed === true;
}

/**
 * Get list of missing critical dependencies
 * @param status Dependency check result
 * @returns Array of missing dependency names
 */
export function getMissingCriticalDependencies(status: DependencyCheckResult): string[] {
  const missing: string[] = [];

  if (!status.ffmpeg?.installed) {
    missing.push('FFmpeg');
  }

  return missing;
}

/**
 * Get list of missing optional dependencies
 * @param status Dependency check result
 * @returns Array of missing optional dependency names
 */
export function getMissingOptionalDependencies(status: DependencyCheckResult): string[] {
  const missing: string[] = [];

  if (!status.python?.installed) {
    missing.push('Python');
  }

  if (!status.gpu?.available) {
    missing.push('GPU acceleration');
  }

  return missing;
}

/**
 * Get human-readable dependency status summary
 * @param status Dependency check result
 * @returns Status summary string
 */
export function getStatusSummary(status: DependencyCheckResult): string {
  const criticalMissing = getMissingCriticalDependencies(status);
  const optionalMissing = getMissingOptionalDependencies(status);

  if (criticalMissing.length > 0) {
    return `Critical dependencies missing: ${criticalMissing.join(', ')}`;
  }

  if (optionalMissing.length > 0) {
    return `Running with limited features (missing: ${optionalMissing.join(', ')})`;
  }

  return 'All dependencies available';
}
