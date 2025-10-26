/**
 * Custom React hook for pacing analysis state management
 * Provides loading, error, and data states with caching
 */

import { useState, useCallback, useRef } from 'react';
import * as pacingService from '../services/pacingService';
import {
  PacingAnalysisRequest,
  PacingAnalysisResponse,
  PacingAnalysisState,
  ReanalyzeRequest,
} from '../types/pacing';

interface UsePacingAnalysisReturn extends PacingAnalysisState {
  analyzePacing: (request: PacingAnalysisRequest) => Promise<void>;
  reanalyzePacing: (request: ReanalyzeRequest) => Promise<void>;
  clearAnalysis: () => void;
  reset: () => void;
}

/**
 * Hook for managing pacing analysis state
 * Includes caching to avoid duplicate API calls
 */
export function usePacingAnalysis(): UsePacingAnalysisReturn {
  const [state, setState] = useState<PacingAnalysisState>({
    loading: false,
    error: null,
    data: null,
  });

  // Cache to avoid duplicate analysis
  const cacheRef = useRef<Map<string, PacingAnalysisResponse>>(new Map());

  // Generate cache key from request
  const getCacheKey = useCallback((request: PacingAnalysisRequest): string => {
    return JSON.stringify({
      script: request.script,
      scenesCount: request.scenes.length,
      platform: request.targetPlatform,
      duration: request.targetDuration,
    });
  }, []);

  // Analyze pacing with caching
  const analyzePacing = useCallback(
    async (request: PacingAnalysisRequest) => {
      const cacheKey = getCacheKey(request);

      // Check cache first
      if (cacheRef.current.has(cacheKey)) {
        setState({
          loading: false,
          error: null,
          data: cacheRef.current.get(cacheKey)!,
        });
        return;
      }

      setState({
        loading: true,
        error: null,
        data: null,
      });

      try {
        const result = await pacingService.analyzePacing(request);

        // Cache the result
        cacheRef.current.set(cacheKey, result);

        setState({
          loading: false,
          error: null,
          data: result,
        });
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Failed to analyze pacing';
        setState({
          loading: false,
          error: errorMessage,
          data: null,
        });
      }
    },
    [getCacheKey]
  );

  // Reanalyze with different parameters
  const reanalyzePacing = useCallback(
    async (request: ReanalyzeRequest) => {
      if (!state.data?.analysisId) {
        setState({
          ...state,
          error: 'No previous analysis found. Please analyze first.',
        });
        return;
      }

      setState({
        ...state,
        loading: true,
        error: null,
      });

      try {
        const result = await pacingService.reanalyzePacing(state.data.analysisId, request);

        setState({
          loading: false,
          error: null,
          data: result,
        });
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Failed to reanalyze pacing';
        setState({
          ...state,
          loading: false,
          error: errorMessage,
        });
      }
    },
    [state]
  );

  // Clear current analysis but keep cache
  const clearAnalysis = useCallback(() => {
    setState({
      loading: false,
      error: null,
      data: null,
    });
  }, []);

  // Reset everything including cache
  const reset = useCallback(() => {
    cacheRef.current.clear();
    setState({
      loading: false,
      error: null,
      data: null,
    });
  }, []);

  return {
    ...state,
    analyzePacing,
    reanalyzePacing,
    clearAnalysis,
    reset,
  };
}
