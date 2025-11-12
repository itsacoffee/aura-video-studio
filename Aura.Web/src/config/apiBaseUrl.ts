/**
 * Utilities for resolving the API base URL used by the frontend.
 * Supports multiple environments:
 * - Electron desktop app (via window.electron.backend.getUrl() or window.AURA_BACKEND_URL)
 * - Browser with explicit configuration (VITE_API_BASE_URL)
 * - Browser served from backend (window.location.origin)
 * - Development fallback (http://127.0.0.1:5005)
 */

export type ApiBaseUrlSource = 'electron' | 'env' | 'origin' | 'fallback';

export interface ApiBaseUrlResolution {
  value: string;
  source: ApiBaseUrlSource;
  raw?: string | undefined;
  isElectron?: boolean;
}

const DEFAULT_DEV_API_BASE_URL = 'http://127.0.0.1:5005';

const trimValue = (value: unknown): string | undefined => {
  if (typeof value !== 'string') {
    return undefined;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : undefined;
};

/**
 * Check if running in Electron environment
 */
export function isElectronEnvironment(): boolean {
  return (
    typeof window !== 'undefined' &&
    (window.AURA_IS_ELECTRON === true || window.electron !== undefined)
  );
}

/**
 * Get backend URL from Electron API (async)
 * Returns null if not in Electron or if the API is unavailable
 */
export async function getElectronBackendUrl(): Promise<string | null> {
  if (typeof window === 'undefined') {
    return null;
  }

  // Check for synchronous global variable first (faster)
  if (window.AURA_BACKEND_URL) {
    const trimmed = trimValue(window.AURA_BACKEND_URL);
    if (trimmed) {
      return trimmed;
    }
  }

  // Try async Electron API if available
  if (window.electron?.backend?.getUrl) {
    try {
      const url = await window.electron.backend.getUrl();
      const trimmed = trimValue(url);
      return trimmed || null;
    } catch (error) {
      console.warn('Failed to get backend URL from Electron API:', error);
      return null;
    }
  }

  return null;
}

/**
 * Resolve the API base URL from configuration or runtime context.
 * This is the synchronous version that uses cached Electron values.
 * For initial Electron setup, use resolveApiBaseUrlAsync() instead.
 */
export function resolveApiBaseUrl(): ApiBaseUrlResolution {
  const isElectron = isElectronEnvironment();

  // Priority 1: Electron global variable (synchronous, fastest)
  if (isElectron && window.AURA_BACKEND_URL) {
    const trimmed = trimValue(window.AURA_BACKEND_URL);
    if (trimmed) {
      return {
        value: trimmed,
        raw: window.AURA_BACKEND_URL,
        source: 'electron',
        isElectron: true,
      };
    }
  }

  // Priority 2: Environment variable (build-time configuration)
  const rawEnvValue = trimValue(import.meta.env.VITE_API_BASE_URL);
  if (rawEnvValue) {
    return {
      value: rawEnvValue,
      raw: rawEnvValue,
      source: 'env',
      isElectron,
    };
  }

  // Priority 3: Current origin (when served from backend)
  if (typeof window !== 'undefined' && window.location?.origin) {
    return {
      value: window.location.origin,
      raw: window.location.origin,
      source: 'origin',
      isElectron,
    };
  }

  // Priority 4: Development fallback
  return {
    value: DEFAULT_DEV_API_BASE_URL,
    raw: DEFAULT_DEV_API_BASE_URL,
    source: 'fallback',
    isElectron,
  };
}

/**
 * Resolve the API base URL asynchronously (for Electron initial setup).
 * This should be called during app initialization to properly detect Electron backend.
 */
export async function resolveApiBaseUrlAsync(): Promise<ApiBaseUrlResolution> {
  const isElectron = isElectronEnvironment();

  // Priority 1: Try to get URL from Electron API (async)
  if (isElectron) {
    const electronUrl = await getElectronBackendUrl();
    if (electronUrl) {
      return {
        value: electronUrl,
        raw: electronUrl,
        source: 'electron',
        isElectron: true,
      };
    }
  }

  // Fall back to synchronous resolution
  return resolveApiBaseUrl();
}
