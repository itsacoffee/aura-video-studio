import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { getHealthSummary, getHealthDetails } from '../services/api/healthApi';
import { loggingService as logger } from '../services/loggingService';
import type {
  HealthSummaryResponse,
  HealthDetailsResponse,
  HealthCheckDetail,
} from '../types/api-v1';

interface HealthDiagnosticsState {
  summary: HealthSummaryResponse | null;
  details: HealthDetailsResponse | null;
  isLoading: boolean;
  error: string | null;
  lastCheckTime: Date | null;

  // Actions
  fetchHealthSummary: () => Promise<void>;
  fetchHealthDetails: () => Promise<void>;
  refreshHealth: () => Promise<void>;
  clearError: () => void;

  // Computed helpers
  isSystemReady: () => boolean;
  getRequiredFailedChecks: () => HealthCheckDetail[];
  hasMinimalSetup: () => boolean;
}

export const useHealthDiagnostics = create<HealthDiagnosticsState>()(
  persist(
    (set, get) => ({
      summary: null,
      details: null,
      isLoading: false,
      error: null,
      lastCheckTime: null,

      fetchHealthSummary: async () => {
        set({ isLoading: true, error: null });
        try {
          const summary = await getHealthSummary();
          set({
            summary,
            lastCheckTime: new Date(),
            isLoading: false,
          });
        } catch (error: unknown) {
          const errorObj = error instanceof Error ? error : new Error(String(error));
          logger.error(
            'Failed to fetch health summary',
            errorObj,
            'healthDiagnostics',
            'fetchHealthSummary'
          );
          set({
            error: errorObj.message,
            isLoading: false,
          });
        }
      },

      fetchHealthDetails: async () => {
        set({ isLoading: true, error: null });
        try {
          const details = await getHealthDetails();
          set({
            details,
            lastCheckTime: new Date(),
            isLoading: false,
          });
        } catch (error: unknown) {
          const errorObj = error instanceof Error ? error : new Error(String(error));
          logger.error(
            'Failed to fetch health details',
            errorObj,
            'healthDiagnostics',
            'fetchHealthDetails'
          );
          set({
            error: errorObj.message,
            isLoading: false,
          });
        }
      },

      refreshHealth: async () => {
        await get().fetchHealthDetails();
      },

      clearError: () => {
        set({ error: null });
      },

      isSystemReady: () => {
        const { details } = get();
        return details?.isReady ?? false;
      },

      getRequiredFailedChecks: () => {
        const { details } = get();
        if (!details) return [];
        return details.checks.filter((check) => check.isRequired && check.status === 'fail');
      },

      hasMinimalSetup: () => {
        const { details } = get();
        if (!details) return false;

        // Check for minimal setup: Config + FFmpeg + at least one LLM and one TTS
        const checks = details.checks;
        const configOk = checks.some((c) => c.id === 'config_present' && c.status === 'pass');
        const ffmpegOk = checks.some((c) => c.id === 'ffmpeg_present' && c.status === 'pass');
        const hasLlm = checks.some((c) => c.category === 'LLM' && c.status === 'pass');
        const hasTts = checks.some((c) => c.category === 'TTS' && c.status === 'pass');

        return configOk && ffmpegOk && hasLlm && hasTts;
      },
    }),
    {
      name: 'aura-health-diagnostics',
      partialize: (state) => ({
        summary: state.summary,
        details: state.details,
        lastCheckTime: state.lastCheckTime,
      }),
    }
  )
);
