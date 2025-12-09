import { useCallback, useEffect, useState } from 'react';
import { apiUrl } from '@/config/api';
import { loggingService } from '@/services/loggingService';

/**
 * Result of an individual preflight check.
 */
export interface PreflightCheckResult {
  passed: boolean;
  skipped: boolean;
  status: string;
  details: string | null;
  suggestedAction: string | null;
}

/**
 * Comprehensive preflight validation report from the backend.
 */
export interface PreflightReport {
  ok: boolean;
  timestamp: string;
  durationMs: number;
  ffmpeg: PreflightCheckResult;
  ollama: PreflightCheckResult;
  tts: PreflightCheckResult;
  diskSpace: PreflightCheckResult;
  imageProvider: PreflightCheckResult;
  errors: string[];
  warnings: string[];
}

/**
 * Result of the usePreflightChecks hook.
 */
export interface UsePreflightChecksResult {
  /** The preflight report from the last check, or null if not yet run */
  report: PreflightReport | null;
  /** Whether a check is currently in progress */
  isLoading: boolean;
  /** Error from the last check, or null if successful */
  error: Error | null;
  /** Function to trigger a new preflight check */
  runChecks: () => Promise<PreflightReport | null>;
}

/**
 * Hook for running preflight validation checks before video generation.
 *
 * @param autoRun - If true, run the checks automatically on mount (default: false)
 * @returns Object containing report, loading state, error, and runChecks function
 *
 * @example
 * ```tsx
 * const { report, isLoading, error, runChecks } = usePreflightChecks(true);
 *
 * if (isLoading) return <Spinner />;
 * if (error) return <ErrorMessage error={error} />;
 * if (!report?.ok) {
 *   return (
 *     <PreflightErrors
 *       errors={report?.errors}
 *       warnings={report?.warnings}
 *       ffmpeg={report?.ffmpeg}
 *       ollama={report?.ollama}
 *     />
 *   );
 * }
 * ```
 */
export function usePreflightChecks(autoRun: boolean = false): UsePreflightChecksResult {
  const [report, setReport] = useState<PreflightReport | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(autoRun);
  const [error, setError] = useState<Error | null>(null);

  const runChecks = useCallback(async (): Promise<PreflightReport | null> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(apiUrl('/api/video/validate'), {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Cache-Control': 'no-cache',
        },
      });

      if (!response.ok) {
        const errorText = await response.text().catch(() => 'Unknown error');
        throw new Error(
          `Preflight check failed: ${response.status} ${response.statusText}. ${errorText}`
        );
      }

      const data = await response.json();

      // Map the backend response to our interface
      // Backend uses camelCase (configured in ASP.NET Core JSON options)
      const preflightReport: PreflightReport = {
        ok: data.ok,
        timestamp: data.timestamp,
        durationMs: data.durationMs,
        ffmpeg: mapCheckResult(data.ffmpeg),
        ollama: mapCheckResult(data.ollama),
        tts: mapCheckResult(data.tts),
        diskSpace: mapCheckResult(data.diskSpace),
        imageProvider: mapCheckResult(data.imageProvider),
        errors: data.errors || [],
        warnings: data.warnings || [],
      };

      setReport(preflightReport);
      loggingService.info(
        `Preflight checks completed: ${preflightReport.ok ? 'passed' : 'failed'}`,
        'usePreflightChecks',
        'runChecks',
        {
          ok: preflightReport.ok,
          errorCount: preflightReport.errors.length,
          durationMs: preflightReport.durationMs,
        }
      );

      return preflightReport;
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj);
      loggingService.error('Preflight check failed', errorObj, 'usePreflightChecks', 'runChecks');
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Auto-run on mount if autoRun is true
  // Note: runChecks is stable (empty deps in useCallback) so only autoRun triggers re-run
  useEffect(() => {
    if (autoRun) {
      runChecks();
    }
  }, [autoRun, runChecks]);

  return {
    report,
    isLoading,
    error,
    runChecks,
  };
}

/**
 * Maps a check result from the backend to our interface.
 */
function mapCheckResult(data: unknown): PreflightCheckResult {
  if (!data || typeof data !== 'object') {
    return {
      passed: false,
      skipped: true,
      status: 'Unknown',
      details: null,
      suggestedAction: null,
    };
  }

  const obj = data as Record<string, unknown>;
  return {
    passed: Boolean(obj.passed),
    skipped: Boolean(obj.skipped),
    status: String(obj.status || 'Unknown'),
    details: obj.details ? String(obj.details) : null,
    suggestedAction: obj.suggestedAction ? String(obj.suggestedAction) : null,
  };
}

export default usePreflightChecks;
