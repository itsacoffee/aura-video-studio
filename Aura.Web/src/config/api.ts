import { env } from './env';

/**
 * API configuration - centralized API base URL management
 *
 * The base URL is resolved once at startup via env.apiBaseUrl which already
 * consults the Electron desktop bridge when available.
 */
export const API_BASE_URL = env.apiBaseUrl.replace(/\/$/, '');

/**
 * Construct a full API endpoint URL
 */
export function apiUrl(path: string): string {
  // Ensure path starts with /
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  return `${API_BASE_URL}${cleanPath}`;
}

/**
 * Helper to make API calls with proper error handling
 */
export async function apiFetch<T = unknown>(path: string, options?: RequestInit): Promise<T> {
  const url = apiUrl(path);
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`API request failed: ${response.status} ${response.statusText}\n${errorText}`);
  }

  // Handle empty responses
  const text = await response.text();
  if (!text) {
    return undefined as T;
  }

  return JSON.parse(text) as T;
}
