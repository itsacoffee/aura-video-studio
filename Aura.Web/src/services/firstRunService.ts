/**
 * Service for managing first-run detection and onboarding completion status
 *
 * This service manages the first-run flag in both localStorage (for immediate checks)
 * and backend database (for cross-device/reinstall scenarios).
 */

import { apiUrl } from '../config/api';
import { loggingService as logger } from './loggingService';

const FIRST_RUN_KEY = 'hasCompletedFirstRun';
const LEGACY_KEY = 'hasSeenOnboarding'; // For backward compatibility
const CACHE_DURATION_MS = 5000; // 5 seconds

export interface FirstRunStatus {
  hasCompletedFirstRun: boolean;
  completedAt?: string;
  version?: string;
}

// Cache for first-run status to prevent excessive API calls
let firstRunCache: { status: boolean; timestamp: number } | null = null;

/**
 * Check if the user has completed the first-run wizard
 *
 * Checks both localStorage (fast) and backend (persistent across devices/reinstalls)
 * Returns true if EITHER shows completion (optimistic approach)
 * Uses caching to prevent excessive API calls
 */
export async function hasCompletedFirstRun(): Promise<boolean> {
  // Check cache first
  const now = Date.now();
  if (firstRunCache && now - firstRunCache.timestamp < CACHE_DURATION_MS) {
    return firstRunCache.status;
  }

  // Check localStorage first (fast path)
  const localStatus = getLocalFirstRunStatus();
  if (localStatus) {
    // Update cache
    firstRunCache = { status: true, timestamp: now };
    return true;
  }

  // Check backend as fallback with retry logic
  try {
    const backendStatus = await getBackendFirstRunStatusWithRetry();
    if (backendStatus.hasCompletedFirstRun) {
      // Sync to localStorage for future fast checks
      setLocalFirstRunStatus(true);
      // Update cache
      firstRunCache = { status: true, timestamp: now };
      return true;
    }
    // Update cache with false status
    firstRunCache = { status: false, timestamp: now };
  } catch (error) {
    logger.warn(
      'Failed to check backend first-run status, using localStorage only',
      'firstRunService',
      'checkFirstRun',
      { error: String(error) }
    );
    // Cache the negative result for a short time
    firstRunCache = { status: false, timestamp: now };
  }

  return false;
}

/**
 * Get first-run status from localStorage
 */
export function getLocalFirstRunStatus(): boolean {
  // Check new key first
  const newKeyValue = localStorage.getItem(FIRST_RUN_KEY);
  if (newKeyValue === 'true') {
    return true;
  }

  // Check legacy key for backward compatibility
  const legacyValue = localStorage.getItem(LEGACY_KEY);
  if (legacyValue === 'true') {
    // Migrate to new key
    setLocalFirstRunStatus(true);
    return true;
  }

  return false;
}

/**
 * Set first-run status in localStorage
 */
export function setLocalFirstRunStatus(completed: boolean): void {
  localStorage.setItem(FIRST_RUN_KEY, completed.toString());
  // Also set legacy key for backward compatibility
  localStorage.setItem(LEGACY_KEY, completed.toString());
}

/**
 * Get first-run status from backend (using new wizard endpoint)
 */
export async function getBackendFirstRunStatus(): Promise<FirstRunStatus> {
  const response = await fetch(apiUrl('/api/setup/wizard/status'));

  if (!response.ok) {
    if (response.status === 404) {
      return { hasCompletedFirstRun: false };
    }
    throw new Error(`Failed to get first-run status: ${response.statusText}`);
  }

  const data = await response.json();
  return {
    hasCompletedFirstRun: data.completed || false,
    completedAt: data.completedAt,
    version: data.version,
  };
}

/**
 * Get first-run status from backend with retry logic
 * Implements exponential backoff with max 3 retries
 */
async function getBackendFirstRunStatusWithRetry(
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<FirstRunStatus> {
  let lastError: Error | null = null;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await getBackendFirstRunStatus();
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error));

      // Don't retry on last attempt
      if (attempt < maxRetries - 1) {
        // Exponential backoff: 1s, 2s, 4s
        const delay = baseDelay * Math.pow(2, attempt);
        logger.info(
          `Retrying first-run status check (attempt ${attempt + 1}/${maxRetries}) after ${delay}ms`,
          'firstRunService',
          'retry'
        );
        await new Promise((resolve) => setTimeout(resolve, delay));
      }
    }
  }

  // All retries failed
  throw lastError || new Error('Failed to get first-run status after retries');
}

/**
 * Set first-run completion status in backend (using new wizard endpoint)
 */
export async function setBackendFirstRunStatus(completed: boolean): Promise<void> {
  if (completed) {
    const response = await fetch(apiUrl('/api/setup/wizard/complete'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        version: '1.0.0',
        selectedTier: localStorage.getItem('selectedTier') || 'free',
        lastStep: 10,
      }),
    });

    if (!response.ok) {
      throw new Error(`Failed to complete wizard: ${response.statusText}`);
    }
  } else {
    const response = await fetch(apiUrl('/api/setup/wizard/reset'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to reset wizard: ${response.statusText}`);
    }
  }
}

/**
 * Clear the first-run status cache
 * Should be called after wizard completion or reset
 */
export function clearFirstRunCache(): void {
  firstRunCache = null;
}

/**
 * Mark first-run as completed in both localStorage and backend
 */
export async function markFirstRunCompleted(): Promise<void> {
  // Set in localStorage immediately for fast feedback
  setLocalFirstRunStatus(true);

  // Clear cache to force fresh check next time
  clearFirstRunCache();

  // Persist to backend (fire and forget, don't block on errors)
  try {
    await setBackendFirstRunStatus(true);
  } catch (error) {
    logger.error(
      'Failed to persist first-run completion to backend',
      error instanceof Error ? error : new Error(String(error)),
      'firstRunService',
      'completeFirstRun'
    );
    // Don't throw - localStorage is sufficient for now
  }
}

/**
 * Reset first-run status (for testing or re-running wizard)
 * This function is now exported for use in Settings to allow users to re-run the wizard
 */
export async function resetFirstRunStatus(): Promise<void> {
  // Clear cache
  clearFirstRunCache();

  // Clear localStorage
  setLocalFirstRunStatus(false);
  localStorage.removeItem(FIRST_RUN_KEY);
  localStorage.removeItem(LEGACY_KEY);

  // Clear backend (fire and forget)
  try {
    await setBackendFirstRunStatus(false);
  } catch (error) {
    logger.error(
      'Failed to reset first-run status in backend',
      error instanceof Error ? error : new Error(String(error)),
      'firstRunService',
      'resetFirstRun'
    );
    // Don't throw - localStorage clearing is sufficient
  }
}

/**
 * Allow users to mark wizard as seen for the current session without persisting
 * Useful for "Never show again" checkbox
 */
export function markWizardNeverShowAgain(): void {
  localStorage.setItem('wizardNeverShowAgain', 'true');
}

/**
 * Check if user has chosen to never show the wizard again
 */
export function shouldNeverShowWizard(): boolean {
  return localStorage.getItem('wizardNeverShowAgain') === 'true';
}

/**
 * Check if first-run status needs migration from legacy key
 */
export function migrateLegacyFirstRunStatus(): void {
  const legacyValue = localStorage.getItem(LEGACY_KEY);
  const newValue = localStorage.getItem(FIRST_RUN_KEY);

  // If legacy exists but new doesn't, migrate
  if (legacyValue === 'true' && !newValue) {
    setLocalFirstRunStatus(true);
  }
}

/**
 * Save wizard progress to backend (for resume functionality)
 */
export async function saveWizardProgressToBackend(
  lastStep: number,
  selectedTier: string | null,
  wizardState?: string
): Promise<void> {
  try {
    const response = await fetch(apiUrl('/api/setup/wizard/save-progress'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        lastStep,
        selectedTier,
        wizardState,
      }),
    });

    if (!response.ok) {
      throw new Error(`Failed to save wizard progress: ${response.statusText}`);
    }
  } catch (error) {
    logger.error(
      'Failed to save wizard progress to backend',
      error instanceof Error ? error : new Error(String(error)),
      'firstRunService',
      'saveWizardProgress'
    );
  }
}
