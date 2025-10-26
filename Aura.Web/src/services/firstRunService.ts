/**
 * Service for managing first-run detection and onboarding completion status
 * 
 * This service manages the first-run flag in both localStorage (for immediate checks)
 * and backend database (for cross-device/reinstall scenarios).
 */

import { apiUrl } from '../config/api';

const FIRST_RUN_KEY = 'hasCompletedFirstRun';
const LEGACY_KEY = 'hasSeenOnboarding'; // For backward compatibility

export interface FirstRunStatus {
  hasCompletedFirstRun: boolean;
  completedAt?: string;
  version?: string;
}

/**
 * Check if the user has completed the first-run wizard
 * 
 * Checks both localStorage (fast) and backend (persistent across devices/reinstalls)
 * Returns true if EITHER shows completion (optimistic approach)
 */
export async function hasCompletedFirstRun(): Promise<boolean> {
  // Check localStorage first (fast path)
  const localStatus = getLocalFirstRunStatus();
  if (localStatus) {
    return true;
  }

  // Check backend as fallback
  try {
    const backendStatus = await getBackendFirstRunStatus();
    if (backendStatus.hasCompletedFirstRun) {
      // Sync to localStorage for future fast checks
      setLocalFirstRunStatus(true);
      return true;
    }
  } catch (error) {
    console.warn('Failed to check backend first-run status, using localStorage only:', error);
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
 * Get first-run status from backend
 */
export async function getBackendFirstRunStatus(): Promise<FirstRunStatus> {
  const response = await fetch(apiUrl('/api/settings/first-run'));
  
  if (!response.ok) {
    if (response.status === 404) {
      // No status saved yet, treat as not completed
      return { hasCompletedFirstRun: false };
    }
    throw new Error(`Failed to get first-run status: ${response.statusText}`);
  }

  const data = await response.json();
  return data;
}

/**
 * Set first-run completion status in backend
 */
export async function setBackendFirstRunStatus(completed: boolean): Promise<void> {
  const response = await fetch(apiUrl('/api/settings/first-run'), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      hasCompletedFirstRun: completed,
      completedAt: completed ? new Date().toISOString() : null,
      version: '1.0.0', // Application version
    }),
  });

  if (!response.ok) {
    throw new Error(`Failed to set first-run status: ${response.statusText}`);
  }
}

/**
 * Mark first-run as completed in both localStorage and backend
 */
export async function markFirstRunCompleted(): Promise<void> {
  // Set in localStorage immediately for fast feedback
  setLocalFirstRunStatus(true);

  // Persist to backend (fire and forget, don't block on errors)
  try {
    await setBackendFirstRunStatus(true);
  } catch (error) {
    console.error('Failed to persist first-run completion to backend:', error);
    // Don't throw - localStorage is sufficient for now
  }
}

/**
 * Reset first-run status (for testing or re-running wizard)
 * This function is now exported for use in Settings to allow users to re-run the wizard
 */
export async function resetFirstRunStatus(): Promise<void> {
  // Clear localStorage
  setLocalFirstRunStatus(false);
  localStorage.removeItem(FIRST_RUN_KEY);
  localStorage.removeItem(LEGACY_KEY);

  // Clear backend (fire and forget)
  try {
    await setBackendFirstRunStatus(false);
  } catch (error) {
    console.error('Failed to reset first-run status in backend:', error);
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
