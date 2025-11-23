/**
 * Service for tracking and managing overall configuration status
 *
 * This service provides a centralized way to check if the application
 * is properly configured and ready for video generation.
 */

import { apiUrl } from '../config/api';
import { getLocalFirstRunStatus } from './firstRunService';
import { loggingService as logger } from './loggingService';

const CONFIG_STATUS_KEY = 'configurationStatus';
const REFRESH_INTERVAL = 30000; // 30 seconds

export interface ConfigurationStatus {
  isConfigured: boolean;
  lastChecked: string;
  checks: {
    providerConfigured: boolean;
    providerValidated: boolean;
    workspaceCreated: boolean;
    ffmpegDetected: boolean;
    apiKeysValid: boolean;
  };
  details: {
    configuredProviders: string[];
    ffmpegPath?: string;
    ffmpegVersion?: string;
    workspacePath?: string;
    diskSpaceAvailable?: number;
    gpuAvailable?: boolean;
  };
  issues: ConfigurationIssue[];
}

export interface ConfigurationIssue {
  severity: 'error' | 'warning' | 'info';
  code: string;
  message: string;
  actionLabel?: string;
  actionUrl?: string;
}

export interface SystemCheckResult {
  ffmpeg: {
    installed: boolean;
    version?: string;
    path?: string;
    error?: string;
  };
  diskSpace: {
    available: number;
    total: number;
    unit: 'GB';
    sufficient: boolean;
  };
  gpu: {
    available: boolean;
    name?: string;
    vramGB?: number;
  };
  providers: {
    configured: string[];
    validated: string[];
    errors: Record<string, string>;
  };
}

class ConfigurationStatusService {
  private cachedStatus: ConfigurationStatus | null = null;
  private lastFetch: number = 0;
  private listeners: Array<(status: ConfigurationStatus) => void> = [];

  /**
   * Get current configuration status with caching
   */
  async getStatus(forceRefresh = false): Promise<ConfigurationStatus> {
    const now = Date.now();

    if (!forceRefresh && this.cachedStatus && now - this.lastFetch < REFRESH_INTERVAL) {
      return this.cachedStatus;
    }

    try {
      // Check localStorage for quick status
      const localStatus = this.getLocalStatus();

      // Fetch fresh status from backend
      const backendStatus = await this.fetchBackendStatus();

      // Merge and prioritize backend data
      const status: ConfigurationStatus = {
        ...localStatus,
        ...backendStatus,
        lastChecked: new Date().toISOString(),
      };

      // Cache the result
      this.cachedStatus = status;
      this.lastFetch = now;
      this.saveLocalStatus(status);

      // Notify listeners
      this.notifyListeners(status);

      return status;
    } catch (error) {
      logger.error(
        'Failed to fetch configuration status',
        error instanceof Error ? error : new Error(String(error)),
        'configurationStatusService',
        'getStatus'
      );

      // Return cached or default status
      return this.cachedStatus || this.getDefaultStatus();
    }
  }

  /**
   * Run comprehensive system checks
   */
  async runSystemChecks(): Promise<SystemCheckResult> {
    try {
      const response = await fetch(apiUrl('/api/health/system-check'));

      if (!response.ok) {
        throw new Error(`System check failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      logger.error(
        'Failed to run system checks',
        error instanceof Error ? error : new Error(String(error)),
        'configurationStatusService',
        'runSystemChecks'
      );

      // Return error state
      return {
        ffmpeg: { installed: false, error: 'Check failed' },
        diskSpace: { available: 0, total: 0, unit: 'GB', sufficient: false },
        gpu: { available: false },
        providers: { configured: [], validated: [], errors: {} },
      };
    }
  }

  /**
   * Test all configured providers
   */
  async testProviders(): Promise<
    Record<string, { success: boolean; message: string; responseTimeMs: number }>
  > {
    try {
      const response = await fetch(apiUrl('/api/provider-profiles/test-all'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });

      if (!response.ok) {
        throw new Error(`Provider test failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      logger.error(
        'Failed to test providers',
        error instanceof Error ? error : new Error(String(error)),
        'configurationStatusService',
        'testProviders'
      );
      return {};
    }
  }

  /**
   * Check if FFmpeg is installed and working
   */
  async checkFFmpeg(): Promise<{
    installed: boolean;
    version?: string;
    path?: string;
    error?: string;
  }> {
    try {
      const response = await fetch(apiUrl('/api/ffmpeg/status'));

      if (!response.ok) {
        return { installed: false, error: `Check failed: ${response.statusText}` };
      }

      const data = await response.json();
      return {
        installed: data.installed && data.valid,
        version: data.version,
        path: data.path,
        error: data.error,
      };
    } catch (error) {
      return {
        installed: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Subscribe to configuration status changes
   */
  subscribe(listener: (status: ConfigurationStatus) => void): () => void {
    this.listeners.push(listener);

    // Return unsubscribe function
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  /**
   * Mark configuration as complete
   */
  async markConfigured(): Promise<void> {
    const status = await this.getStatus(true);
    status.isConfigured = true;
    status.lastChecked = new Date().toISOString();

    this.saveLocalStatus(status);
    this.notifyListeners(status);
  }

  /**
   * Reset configuration status (for testing or reconfiguration)
   */
  async resetConfiguration(): Promise<void> {
    const status = this.getDefaultStatus();
    this.cachedStatus = status;
    this.saveLocalStatus(status);
    this.notifyListeners(status);
  }

  /**
   * Check disk space availability
   */
  async checkDiskSpace(
    path?: string
  ): Promise<{ available: number; total: number; unit: 'GB'; sufficient: boolean }> {
    try {
      const response = await fetch(
        apiUrl(`/api/system/disk-space${path ? `?path=${encodeURIComponent(path)}` : ''}`)
      );

      if (!response.ok) {
        throw new Error(`Disk space check failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      logger.error(
        'Failed to check disk space',
        error instanceof Error ? error : new Error(String(error)),
        'configurationStatusService',
        'checkDiskSpace'
      );
      return { available: 0, total: 0, unit: 'GB', sufficient: false };
    }
  }

  // Private helper methods

  private async fetchBackendStatus(): Promise<Partial<ConfigurationStatus>> {
    try {
      const response = await fetch(apiUrl('/api/setup/configuration-status'));

      if (!response.ok) {
        if (response.status === 404) {
          // Endpoint doesn't exist yet, build status from individual checks
          return await this.buildStatusFromChecks();
        }
        throw new Error(`Status check failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      logger.warn(
        'Failed to fetch backend configuration status, building from checks',
        'configurationStatusService',
        'fetchBackendStatus',
        { error: String(error) }
      );
      return await this.buildStatusFromChecks();
    }
  }

  private async buildStatusFromChecks(): Promise<Partial<ConfigurationStatus>> {
    // CRITICAL FIX: Check if user has completed wizard - if so, be lenient with checks
    const hasCompletedWizard = getLocalFirstRunStatus();
    console.info('[configurationStatusService] User has completed wizard:', hasCompletedWizard);

    // CRITICAL FIX: Trigger a rescan before checking FFmpeg to ensure managed version is detected
    // The managed version is always installed on build but may not be detected on first check
    try {
      const rescanResponse = await fetch(apiUrl('/api/ffmpeg/rescan'), { method: 'POST' });
      if (rescanResponse.ok) {
        console.info('[configurationStatusService] FFmpeg rescan triggered to detect managed version');
        // Wait a moment for rescan to complete
        await new Promise((resolve) => setTimeout(resolve, 1000));
      }
    } catch (error) {
      console.warn('[configurationStatusService] Failed to trigger FFmpeg rescan:', error);
      // Continue with normal check even if rescan fails
    }

    const [ffmpeg, systemCheck] = await Promise.allSettled([
      this.checkFFmpeg(),
      this.runSystemChecks(),
    ]);

    const ffmpegResult = ffmpeg.status === 'fulfilled' ? ffmpeg.value : { installed: false };
    const systemCheckResult = systemCheck.status === 'fulfilled' ? systemCheck.value : null;

    // CRITICAL FIX: If user completed wizard, assume providers are configured even if API check fails
    // This prevents false "not configured" errors when API is temporarily unavailable
    const providersConfiguredFromCheck = (systemCheckResult?.providers.configured.length ?? 0) > 0;
    const providerConfigured = hasCompletedWizard || providersConfiguredFromCheck;

    const checks = {
      providerConfigured,
      providerValidated: (systemCheckResult?.providers.validated.length ?? 0) > 0,
      workspaceCreated: true, // Assume workspace exists if no error
      ffmpegDetected: ffmpegResult.installed,
      apiKeysValid: (systemCheckResult?.providers.validated.length ?? 0) > 0,
    };

    // CRITICAL FIX: If user completed wizard, only require FFmpeg (providers assumed configured)
    const isConfigured = hasCompletedWizard
      ? checks.ffmpegDetected // Only require FFmpeg if wizard completed
      : checks.providerConfigured && checks.ffmpegDetected; // Require both if wizard not completed

    return {
      isConfigured,
      checks,
      details: {
        configuredProviders: systemCheckResult?.providers.configured ?? [],
        ffmpegPath: ffmpegResult.path,
        ffmpegVersion: ffmpegResult.version,
        diskSpaceAvailable: systemCheckResult?.diskSpace.available,
        gpuAvailable: systemCheckResult?.gpu.available,
      },
      issues: this.generateIssues(checks, ffmpegResult),
    };
  }

  private generateIssues(
    checks: ConfigurationStatus['checks'],
    ffmpegResult: { installed: boolean; error?: string }
  ): ConfigurationIssue[] {
    const issues: ConfigurationIssue[] = [];

    if (!checks.providerConfigured) {
      issues.push({
        severity: 'error',
        code: 'NO_PROVIDER',
        message:
          'No AI provider configured. Configure at least one provider to generate video scripts.',
        actionLabel: 'Configure Providers',
        actionUrl: '/setup',
      });
    }

    if (!checks.ffmpegDetected) {
      issues.push({
        severity: 'error',
        code: 'NO_FFMPEG',
        message:
          ffmpegResult.error || 'FFmpeg not detected. FFmpeg is required for video rendering.',
        actionLabel: 'Install FFmpeg',
        actionUrl: '/setup',
      });
    }

    if (checks.providerConfigured && !checks.providerValidated) {
      issues.push({
        severity: 'warning',
        code: 'PROVIDER_NOT_VALIDATED',
        message: 'Provider configured but not validated. Test your API keys to ensure they work.',
        actionLabel: 'Test Providers',
      });
    }

    return issues;
  }

  private getLocalStatus(): ConfigurationStatus {
    try {
      const stored = localStorage.getItem(CONFIG_STATUS_KEY);
      if (stored) {
        return JSON.parse(stored);
      }
    } catch (error) {
      logger.warn(
        'Failed to parse local configuration status',
        'configurationStatusService',
        'getLocalStatus',
        { error: String(error) }
      );
    }
    return this.getDefaultStatus();
  }

  private saveLocalStatus(status: ConfigurationStatus): void {
    try {
      localStorage.setItem(CONFIG_STATUS_KEY, JSON.stringify(status));
    } catch (error) {
      logger.warn(
        'Failed to save local configuration status',
        'configurationStatusService',
        'saveLocalStatus',
        { error: String(error) }
      );
    }
  }

  private getDefaultStatus(): ConfigurationStatus {
    return {
      isConfigured: false,
      lastChecked: new Date().toISOString(),
      checks: {
        providerConfigured: false,
        providerValidated: false,
        workspaceCreated: false,
        ffmpegDetected: false,
        apiKeysValid: false,
      },
      details: {
        configuredProviders: [],
      },
      issues: [
        {
          severity: 'error',
          code: 'NOT_CONFIGURED',
          message: 'System not configured. Please complete the setup wizard.',
          actionLabel: 'Start Setup',
          actionUrl: '/setup',
        },
      ],
    };
  }

  private notifyListeners(status: ConfigurationStatus): void {
    this.listeners.forEach((listener) => {
      try {
        listener(status);
      } catch (error) {
        logger.error(
          'Error in configuration status listener',
          error instanceof Error ? error : new Error(String(error)),
          'configurationStatusService',
          'notifyListeners'
        );
      }
    });
  }
}

// Export singleton instance
export const configurationStatusService = new ConfigurationStatusService();
