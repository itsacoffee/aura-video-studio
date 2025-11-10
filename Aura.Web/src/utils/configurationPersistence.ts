/**
 * Configuration Persistence Utilities
 * 
 * Handles saving, loading, and backing up configuration data
 */

import { loggingService as logger } from '../services/loggingService';

const CONFIG_BACKUP_KEY = 'configurationBackup';
const CONFIG_HISTORY_KEY = 'configurationHistory';
const MAX_HISTORY_ENTRIES = 5;

export interface ConfigurationBackup {
  timestamp: string;
  version: string;
  data: {
    providers: Record<string, unknown>;
    workspace: Record<string, unknown>;
    ffmpeg: Record<string, unknown>;
    general: Record<string, unknown>;
  };
}

export interface ConfigurationExport {
  version: string;
  exportDate: string;
  includesSecrets: boolean;
  configuration: {
    providers: Record<string, unknown>;
    workspace: Record<string, unknown>;
    ffmpeg: Record<string, unknown>;
    settings: Record<string, unknown>;
  };
}

/**
 * Create a backup of the current configuration
 */
export function createConfigurationBackup(
  providersConfig: Record<string, unknown>,
  workspaceConfig: Record<string, unknown>,
  ffmpegConfig: Record<string, unknown>,
  generalSettings: Record<string, unknown>
): void {
  try {
    const backup: ConfigurationBackup = {
      timestamp: new Date().toISOString(),
      version: '1.0.0',
      data: {
        providers: providersConfig,
        workspace: workspaceConfig,
        ffmpeg: ffmpegConfig,
        general: generalSettings,
      },
    };

    // Save current backup
    localStorage.setItem(CONFIG_BACKUP_KEY, JSON.stringify(backup));

    // Add to history
    addToConfigurationHistory(backup);

    logger.info('Configuration backup created successfully', 'configurationPersistence', 'createBackup');
  } catch (error) {
    logger.error(
      'Failed to create configuration backup',
      error instanceof Error ? error : new Error(String(error)),
      'configurationPersistence',
      'createBackup'
    );
  }
}

/**
 * Restore configuration from the most recent backup
 */
export function restoreConfigurationBackup(): ConfigurationBackup | null {
  try {
    const backupStr = localStorage.getItem(CONFIG_BACKUP_KEY);
    if (!backupStr) {
      return null;
    }

    const backup: ConfigurationBackup = JSON.parse(backupStr);
    logger.info('Configuration backup restored', 'configurationPersistence', 'restoreBackup');
    return backup;
  } catch (error) {
    logger.error(
      'Failed to restore configuration backup',
      error instanceof Error ? error : new Error(String(error)),
      'configurationPersistence',
      'restoreBackup'
    );
    return null;
  }
}

/**
 * Get configuration history
 */
export function getConfigurationHistory(): ConfigurationBackup[] {
  try {
    const historyStr = localStorage.getItem(CONFIG_HISTORY_KEY);
    if (!historyStr) {
      return [];
    }

    return JSON.parse(historyStr);
  } catch (error) {
    logger.error(
      'Failed to get configuration history',
      error instanceof Error ? error : new Error(String(error)),
      'configurationPersistence',
      'getHistory'
    );
    return [];
  }
}

/**
 * Add an entry to configuration history
 */
function addToConfigurationHistory(backup: ConfigurationBackup): void {
  try {
    const history = getConfigurationHistory();
    
    // Add new entry
    history.unshift(backup);

    // Keep only the most recent entries
    const trimmedHistory = history.slice(0, MAX_HISTORY_ENTRIES);

    localStorage.setItem(CONFIG_HISTORY_KEY, JSON.stringify(trimmedHistory));
  } catch (error) {
    logger.warn(
      'Failed to add to configuration history',
      'configurationPersistence',
      'addToHistory',
      { error: String(error) }
    );
  }
}

/**
 * Export configuration to JSON (for download)
 */
export function exportConfiguration(
  providersConfig: Record<string, unknown>,
  workspaceConfig: Record<string, unknown>,
  ffmpegConfig: Record<string, unknown>,
  settings: Record<string, unknown>,
  includeSecrets = false
): string {
  const exportData: ConfigurationExport = {
    version: '1.0.0',
    exportDate: new Date().toISOString(),
    includesSecrets: includeSecrets,
    configuration: {
      providers: includeSecrets ? providersConfig : maskSecrets(providersConfig),
      workspace: workspaceConfig,
      ffmpeg: ffmpegConfig,
      settings,
    },
  };

  return JSON.stringify(exportData, null, 2);
}

/**
 * Import configuration from JSON
 */
export function importConfiguration(json: string): ConfigurationExport | null {
  try {
    const data: ConfigurationExport = JSON.parse(json);

    // Validate structure
    if (!data.version || !data.configuration) {
      throw new Error('Invalid configuration format');
    }

    logger.info('Configuration imported successfully', 'configurationPersistence', 'importConfiguration');
    return data;
  } catch (error) {
    logger.error(
      'Failed to import configuration',
      error instanceof Error ? error : new Error(String(error)),
      'configurationPersistence',
      'importConfiguration'
    );
    return null;
  }
}

/**
 * Download configuration as a file
 */
export function downloadConfigurationFile(
  providersConfig: Record<string, unknown>,
  workspaceConfig: Record<string, unknown>,
  ffmpegConfig: Record<string, unknown>,
  settings: Record<string, unknown>,
  includeSecrets = false
): void {
  const json = exportConfiguration(providersConfig, workspaceConfig, ffmpegConfig, settings, includeSecrets);
  const blob = new Blob([json], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  
  const a = document.createElement('a');
  a.href = url;
  a.download = `aura-config-${new Date().toISOString().split('T')[0]}.json`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);

  logger.info('Configuration file downloaded', 'configurationPersistence', 'downloadFile');
}

/**
 * Mask sensitive values in configuration
 */
function maskSecrets(config: Record<string, unknown>): Record<string, unknown> {
  const masked: Record<string, unknown> = {};

  for (const [key, value] of Object.entries(config)) {
    if (typeof value === 'object' && value !== null) {
      masked[key] = maskSecrets(value as Record<string, unknown>);
    } else if (typeof value === 'string' && (
      key.toLowerCase().includes('key') ||
      key.toLowerCase().includes('secret') ||
      key.toLowerCase().includes('password') ||
      key.toLowerCase().includes('token')
    )) {
      // Mask sensitive values
      masked[key] = value.length > 8 ? `${value.substring(0, 4)}...${value.substring(value.length - 4)}` : '***';
    } else {
      masked[key] = value;
    }
  }

  return masked;
}

/**
 * Clear all configuration backups
 */
export function clearConfigurationBackups(): void {
  try {
    localStorage.removeItem(CONFIG_BACKUP_KEY);
    localStorage.removeItem(CONFIG_HISTORY_KEY);
    logger.info('Configuration backups cleared', 'configurationPersistence', 'clearBackups');
  } catch (error) {
    logger.error(
      'Failed to clear configuration backups',
      error instanceof Error ? error : new Error(String(error)),
      'configurationPersistence',
      'clearBackups'
    );
  }
}

/**
 * Validate configuration data
 */
export function validateConfiguration(data: unknown): { valid: boolean; errors: string[] } {
  const errors: string[] = [];

  if (typeof data !== 'object' || data === null) {
    errors.push('Configuration data must be an object');
    return { valid: false, errors };
  }

  const config = data as Record<string, unknown>;

  if (!config.version) {
    errors.push('Configuration version is missing');
  }

  if (!config.configuration) {
    errors.push('Configuration data is missing');
  }

  return {
    valid: errors.length === 0,
    errors,
  };
}

/**
 * Migrate configuration between versions
 */
export function migrateConfiguration(
  oldVersion: string,
  newVersion: string,
  data: Record<string, unknown>
): Record<string, unknown> {
  // For now, just return the data as-is
  // In the future, add version-specific migration logic
  logger.info(
    `Migrating configuration from ${oldVersion} to ${newVersion}`,
    'configurationPersistence',
    'migrate'
  );
  return data;
}
