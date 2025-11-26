/**
 * Configuration service for unified configuration and startup validation
 * 
 * This service provides access to the unified configuration validation endpoints:
 * - /api/config/status - Comprehensive configuration validation status
 * - /api/config/dependencies - Individual dependency status checks
 */

import { apiUrl } from '../config/api';
import { loggingService as logger } from './loggingService';

export interface ConfigurationStatusResponse {
  isValid: boolean;
  canStart: boolean;
  criticalIssues: ValidationIssue[];
  warnings: ValidationIssue[];
  validationDurationMs: number;
  timestamp: string;
}

export interface ValidationIssue {
  category: string;
  code: string;
  message: string;
  resolution: string | null;
}

export interface DependencyStatusResponse {
  ffmpeg: {
    isAvailable: boolean;
    message: string;
    version: string | null;
  };
  ollama: {
    isAvailable: boolean;
    message: string;
    details: string | null;
  };
  database: {
    isAvailable: boolean;
    message: string;
    path: string | null;
  };
  outputDirectory: {
    isAvailable: boolean;
    message: string;
    path: string | null;
  };
  timestamp: string;
}

class ConfigService {
  private cachedStatus: ConfigurationStatusResponse | null = null;
  private cachedDependencies: DependencyStatusResponse | null = null;
  private lastStatusFetch: number = 0;
  private lastDependenciesFetch: number = 0;
  private readonly CACHE_DURATION = 30000; // 30 seconds

  /**
   * Get comprehensive configuration validation status
   */
  async getConfigurationStatus(forceRefresh = false): Promise<ConfigurationStatusResponse> {
    const now = Date.now();

    if (!forceRefresh && this.cachedStatus && now - this.lastStatusFetch < this.CACHE_DURATION) {
      return this.cachedStatus;
    }

    try {
      const response = await fetch(apiUrl('/api/config/status'));

      if (!response.ok) {
        throw new Error(`Configuration status check failed: ${response.statusText}`);
      }

      const status = await response.json() as ConfigurationStatusResponse;
      
      this.cachedStatus = status;
      this.lastStatusFetch = now;

      return status;
    } catch (error) {
      logger.error(
        'Failed to fetch configuration status',
        error instanceof Error ? error : new Error(String(error)),
        'configService',
        'getConfigurationStatus'
      );

      // Return cached status if available, otherwise throw
      if (this.cachedStatus) {
        return this.cachedStatus;
      }
      throw error;
    }
  }

  /**
   * Get individual dependency status
   */
  async getDependencyStatus(forceRefresh = false): Promise<DependencyStatusResponse> {
    const now = Date.now();

    if (!forceRefresh && this.cachedDependencies && now - this.lastDependenciesFetch < this.CACHE_DURATION) {
      return this.cachedDependencies;
    }

    try {
      const response = await fetch(apiUrl('/api/config/dependencies'));

      if (!response.ok) {
        throw new Error(`Dependency status check failed: ${response.statusText}`);
      }

      const dependencies = await response.json() as DependencyStatusResponse;
      
      this.cachedDependencies = dependencies;
      this.lastDependenciesFetch = now;

      return dependencies;
    } catch (error) {
      logger.error(
        'Failed to fetch dependency status',
        error instanceof Error ? error : new Error(String(error)),
        'configService',
        'getDependencyStatus'
      );

      // Return cached dependencies if available, otherwise throw
      if (this.cachedDependencies) {
        return this.cachedDependencies;
      }
      throw error;
    }
  }

  /**
   * Check if the application can start (no critical issues)
   */
  async canStart(): Promise<boolean> {
    try {
      const status = await this.getConfigurationStatus();
      return status.canStart;
    } catch (error) {
      logger.warn(
        'Failed to check if application can start',
        error instanceof Error ? error : new Error(String(error)),
        'configService',
        'canStart'
      );
      // Assume it can start if we can't check (fail open)
      return true;
    }
  }

  /**
   * Get critical issues that prevent startup
   */
  async getCriticalIssues(): Promise<ValidationIssue[]> {
    try {
      const status = await this.getConfigurationStatus();
      return status.criticalIssues;
    } catch (error) {
      logger.warn(
        'Failed to get critical issues',
        error instanceof Error ? error : new Error(String(error)),
        'configService',
        'getCriticalIssues'
      );
      return [];
    }
  }

  /**
   * Get non-critical warnings
   */
  async getWarnings(): Promise<ValidationIssue[]> {
    try {
      const status = await this.getConfigurationStatus();
      return status.warnings;
    } catch (error) {
      logger.warn(
        'Failed to get warnings',
        error instanceof Error ? error : new Error(String(error)),
        'configService',
        'getWarnings'
      );
      return [];
    }
  }

  /**
   * Clear cached status (force refresh on next call)
   */
  clearCache(): void {
    this.cachedStatus = null;
    this.cachedDependencies = null;
    this.lastStatusFetch = 0;
    this.lastDependenciesFetch = 0;
  }
}

export const configService = new ConfigService();

