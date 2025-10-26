/**
 * Pacing analysis API service
 * Handles all API calls related to pacing optimization
 */

import {
  PacingAnalysisRequest,
  PacingAnalysisResponse,
  PlatformPresetsResponse,
  ReanalyzeRequest,
} from '../types/pacing';

const API_BASE = '/api/pacing';

/**
 * Analyzes script and scenes for optimal pacing
 */
export async function analyzePacing(
  request: PacingAnalysisRequest
): Promise<PacingAnalysisResponse> {
  try {
    const response = await fetch(`${API_BASE}/analyze`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(
        errorData?.detail || errorData?.message || `HTTP error! status: ${response.status}`
      );
    }

    return await response.json();
  } catch (error) {
    console.error('Error analyzing pacing:', error);
    throw error;
  }
}

/**
 * Gets available platform presets with pacing recommendations
 */
export async function getPlatformPresets(): Promise<PlatformPresetsResponse> {
  try {
    const response = await fetch(`${API_BASE}/platforms`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching platform presets:', error);
    throw error;
  }
}

/**
 * Reanalyzes pacing with different parameters
 * Note: This uses the cached analysis ID from previous analysis
 */
export async function reanalyzePacing(
  analysisId: string,
  request: ReanalyzeRequest
): Promise<PacingAnalysisResponse> {
  try {
    const response = await fetch(`${API_BASE}/reanalyze/${analysisId}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(
        errorData?.detail || errorData?.message || `HTTP error! status: ${response.status}`
      );
    }

    return await response.json();
  } catch (error) {
    console.error('Error reanalyzing pacing:', error);
    throw error;
  }
}

/**
 * Retrieves a previous analysis by ID
 */
export async function getAnalysis(analysisId: string): Promise<PacingAnalysisResponse> {
  try {
    const response = await fetch(`${API_BASE}/analysis/${analysisId}`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching analysis:', error);
    throw error;
  }
}

/**
 * Converts ISO 8601 duration string to seconds
 * Handles formats like "PT15S", "PT1M30S", "PT1H30M45S"
 */
export function durationToSeconds(duration: string): number {
  // Pattern is safe: anchored with ^ and $, optional groups are non-overlapping
  // eslint-disable-next-line security/detect-unsafe-regex
  const match = duration.match(/^PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)(?:\.(\d+))?S)?$/);
  if (!match) return 0;

  const hours = parseInt(match[1] || '0', 10);
  const minutes = parseInt(match[2] || '0', 10);
  const seconds = parseFloat(match[3] || '0');

  return hours * 3600 + minutes * 60 + seconds;
}

/**
 * Converts seconds to ISO 8601 duration string
 */
export function secondsToDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = seconds % 60;

  let duration = 'PT';
  if (hours > 0) duration += `${hours}H`;
  if (minutes > 0) duration += `${minutes}M`;
  if (secs > 0 || (hours === 0 && minutes === 0)) duration += `${secs.toFixed(1)}S`;

  return duration;
}

/**
 * Formats duration string to human-readable format
 */
export function formatDuration(duration: string): string {
  const seconds = durationToSeconds(duration);
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);

  if (mins > 0) {
    return `${mins}m ${secs}s`;
  }
  return `${secs}s`;
}

/**
 * Calculates the percentage change between two values
 */
export function calculatePercentageChange(current: number, optimal: number): number {
  if (current === 0) return 0;
  return ((optimal - current) / current) * 100;
}
