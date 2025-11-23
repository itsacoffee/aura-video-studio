/**
 * Settings Validation Service
 *
 * Validates that required settings are configured and valid.
 * Used by ConfigurationGate to determine if setup is needed.
 */

import { apiUrl } from '../config/api';
import { isValidPath, migrateLegacyPath } from '../utils/pathUtils';
import { getLocalFirstRunStatus } from './firstRunService';
import { ffmpegClient } from './api/ffmpegClient';

export interface SettingsValidation {
  valid: boolean;
  error?: string;
  missingSettings?: string[];
}

interface ProviderStatusSummary {
  name: string;
  category?: string;
  isAvailable?: boolean;
  isOnline?: boolean;
}

interface ProviderStatusResponse {
  providers?: ProviderStatusSummary[];
}

/**
 * Validate that all required settings are configured
 * CRITICAL: If user has completed wizard, be lenient with validation failures
 */
export async function validateRequiredSettings(): Promise<SettingsValidation> {
  const missingSettings: string[] = [];
  let ffmpegMissing = false;

  // CRITICAL FIX: Check if user has completed wizard - if so, be lenient with validation
  const hasCompletedWizard = getLocalFirstRunStatus();
  console.info('[validateRequiredSettings] User has completed wizard:', hasCompletedWizard);

  try {
    // Check FFmpeg availability (required for video generation)
    const ffmpegStatus = await checkFFmpegStatus();
    if (!ffmpegStatus.available) {
      missingSettings.push('FFmpeg');
      ffmpegMissing = true;
    }

    const llmStatus = await hasReadyLlmProvider(hasCompletedWizard);
    if (!llmStatus.ready) {
      missingSettings.push('LLM Provider');
    }

    // Check default save location - no longer required to be set, we provide a default
    const saveLocation = await getDefaultSaveLocation();
    if (!saveLocation) {
      console.warn('Could not determine default save location');
    }

    // CRITICAL FIX: If user has completed wizard, don't block them on validation failures
    // Show warnings but allow access - they can fix issues in Settings
    if (missingSettings.length > 0) {
      let errorMessage = `Required software missing: ${missingSettings.join(', ')}`;
      if (!ffmpegMissing && missingSettings.length === 1 && !llmStatus.ready && llmStatus.message) {
        errorMessage = llmStatus.message;
      }

      // If user completed wizard, allow access but log warning
      if (hasCompletedWizard) {
        console.warn('[validateRequiredSettings] User completed wizard but validation failed - allowing access:', {
          missingSettings,
          errorMessage,
        });
        // Return valid but with a warning - don't block access
        return { valid: true };
      }

      return {
        valid: false,
        error: errorMessage,
        missingSettings,
      };
    }

    return { valid: true };
  } catch (error: unknown) {
    console.error('Settings validation error:', error);

    // CRITICAL FIX: If user completed wizard, don't block on validation errors
    if (hasCompletedWizard) {
      console.warn('[validateRequiredSettings] Validation error but user completed wizard - allowing access');
      return { valid: true };
    }

    return {
      valid: false,
      error: error instanceof Error ? error.message : 'Settings validation failed',
    };
  }
}

/**
 * Check if FFmpeg is available and configured
 */
async function checkFFmpegStatus(): Promise<{ available: boolean; path?: string }> {
  try {
    const status = await ffmpegClient.getStatus();
    return {
      available: Boolean(status.installed && status.valid),
      path: status.path ?? undefined,
    };
  } catch (error: unknown) {
    console.error('FFmpeg status check failed:', error);

    try {
      const response = await fetch(apiUrl('/api/downloads/ffmpeg/status'));
      if (!response.ok) {
        return { available: false };
      }

      const data = await response.json();
      const isAvailable = data.state === 'Installed' || data.state === 'ExternalAttached';

      return {
        available: isAvailable,
        path: data.path,
      };
    } catch (legacyError) {
      console.error('Legacy FFmpeg status check also failed:', legacyError);
      return { available: false };
    }
  }
}

async function hasReadyLlmProvider(hasCompletedWizard: boolean = false): Promise<{ ready: boolean; message?: string }> {
  try {
    const response = await fetch(apiUrl('/api/provider-status'));
    if (!response.ok) {
      // CRITICAL FIX: If user completed wizard, be lenient - assume providers are configured
      // The API might be temporarily unavailable or having issues, but don't block the user
      if (hasCompletedWizard) {
        console.warn('[hasReadyLlmProvider] Provider status API failed but user completed wizard - assuming providers configured');
        return { ready: true };
      }

      return {
        ready: false,
        message: 'Unable to verify provider status. Complete setup in Settings > Providers.',
      };
    }

    const data = (await response.json()) as ProviderStatusResponse;
    const providers = data.providers ?? [];
    const llmProviders = providers.filter(
      (provider) => (provider.category ?? '').toLowerCase() === 'llm'
    );

    const hasOnlineProvider = llmProviders.some(
      (provider) => Boolean(provider.isOnline) && Boolean(provider.isAvailable)
    );
    const hasOllamaProvider = llmProviders.some(
      (provider) => Boolean(provider.isAvailable) && provider.name.toLowerCase().includes('ollama')
    );

    const onlyRuleBased =
      llmProviders.length > 0 &&
      llmProviders.every((provider) => provider.name.toLowerCase().includes('rule-based'));

    if (hasOnlineProvider || hasOllamaProvider) {
      return { ready: true };
    }

    if (onlyRuleBased) {
      return {
        ready: false,
        message:
          'Add an API key (OpenAI, Anthropic, Gemini) or start Ollama to unlock AI script generation.',
      };
    }

    return {
      ready: false,
      message: 'Configure at least one LLM provider in Settings > Providers to continue.',
    };
  } catch (error) {
    console.error('Provider status check failed:', error);

    // CRITICAL FIX: If user completed wizard, be lenient on API failures
    // Don't block them if the API is temporarily unavailable
    if (hasCompletedWizard) {
      console.warn('[hasReadyLlmProvider] Provider status check error but user completed wizard - assuming providers configured');
      return { ready: true };
    }

    return {
      ready: false,
      message: 'Could not verify provider status. Ensure at least one provider is configured.',
    };
  }
}

/**
 * Get the platform-specific default save location
 * Note: Browser cannot access actual home directory for security reasons,
 * so we return placeholder paths that the backend will resolve properly
 */
function getPlatformDefaultSaveLocation(): string {
  const platform = navigator.platform.toLowerCase();

  if (platform.includes('win')) {
    return '%USERPROFILE%\\Videos\\Aura';
  } else if (platform.includes('mac')) {
    return '~/Movies/Aura';
  } else {
    return '~/Videos/Aura';
  }
}

/**
 * Get the configured default save location from settings
 */
async function getDefaultSaveLocation(): Promise<string | null> {
  try {
    // Check localStorage first (fast path)
    const localSettings = localStorage.getItem('workspacePreferences');
    if (localSettings) {
      const prefs = JSON.parse(localSettings);
      if (prefs.defaultSaveLocation) {
        // Migrate legacy paths if needed
        return migrateLegacyPath(prefs.defaultSaveLocation);
      }
    }

    // Fall back to backend settings
    const response = await fetch(apiUrl('/api/settings/user'));
    if (!response.ok) {
      return getPlatformDefaultSaveLocation();
    }

    const settings = await response.json();
    const saveLocation = settings?.general?.defaultProjectSaveLocation;

    if (saveLocation && isValidPath(saveLocation)) {
      return saveLocation;
    }

    return getPlatformDefaultSaveLocation();
  } catch (error: unknown) {
    console.error('Failed to get default save location:', error);
    return getPlatformDefaultSaveLocation();
  }
}

/**
 * Migrate settings from old format to new format
 * This is called during app initialization
 */
export async function migrateSettingsIfNeeded(): Promise<void> {
  try {
    // Check if migration is needed
    const localSettings = localStorage.getItem('workspacePreferences');
    if (!localSettings) {
      return;
    }

    const prefs = JSON.parse(localSettings);
    let needsMigration = false;
    const migratedPrefs = { ...prefs };

    // Migrate default save location if it has placeholder
    if (prefs.defaultSaveLocation && !isValidPath(prefs.defaultSaveLocation)) {
      migratedPrefs.defaultSaveLocation = migrateLegacyPath(prefs.defaultSaveLocation);
      needsMigration = true;
    }

    // Migrate cache location if it has placeholder
    if (prefs.cacheLocation && !isValidPath(prefs.cacheLocation)) {
      migratedPrefs.cacheLocation = migrateLegacyPath(prefs.cacheLocation);
      needsMigration = true;
    }

    // Save migrated settings
    if (needsMigration) {
      localStorage.setItem('workspacePreferences', JSON.stringify(migratedPrefs));

      // Also update backend settings
      await fetch(apiUrl('/api/settings'), {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          defaultSaveLocation: migratedPrefs.defaultSaveLocation,
          cacheLocation: migratedPrefs.cacheLocation,
        }),
      });
    }
  } catch (error: unknown) {
    console.error('Settings migration failed:', error);
  }
}

/**
 * Persist workspace preferences to localStorage and backend
 */
export async function saveWorkspacePreferences(preferences: {
  defaultSaveLocation: string;
  cacheLocation: string;
  autosaveInterval: number;
  theme: 'light' | 'dark' | 'auto';
}): Promise<void> {
  try {
    // Save to localStorage for fast access
    localStorage.setItem('workspacePreferences', JSON.stringify(preferences));

    // Save to backend for persistence
    await fetch(apiUrl('/api/settings'), {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        defaultSaveLocation: preferences.defaultSaveLocation,
        cacheLocation: preferences.cacheLocation,
        autosaveInterval: preferences.autosaveInterval,
        theme: preferences.theme,
      }),
    });
  } catch (error: unknown) {
    console.error('Failed to save workspace preferences:', error);
    throw error;
  }
}
