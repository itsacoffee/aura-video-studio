/**
 * OpenCut Transcription Store
 *
 * Manages transcription state for the OpenCut video editor.
 * Handles transcription progress, results caching, and
 * integration with the captions system.
 */

import { create } from 'zustand';
import type {
  TranscriptionResult,
  TranscriptionProgress,
  TranscriptionOptions,
} from '../services/transcriptionService';

/**
 * State for the transcription store
 */
interface TranscriptionState {
  /** Cached transcription results keyed by clip ID */
  results: Map<string, TranscriptionResult>;
  /** Current transcription progress */
  progress: TranscriptionProgress | null;
  /** Whether transcription is in progress */
  isTranscribing: boolean;
  /** Current error message if any */
  error: string | null;
  /** ID of the clip currently being transcribed */
  activeClipId: string | null;
}

/**
 * Actions for the transcription store
 */
interface TranscriptionActions {
  /**
   * Start transcription for a clip
   *
   * @param clipId - ID of the clip to transcribe
   * @param audioUrl - URL of the audio source
   * @param options - Transcription options
   */
  startTranscription: (
    clipId: string,
    audioUrl: string,
    options?: TranscriptionOptions
  ) => Promise<void>;

  /**
   * Cancel the current transcription
   */
  cancelTranscription: () => void;

  /**
   * Get cached transcription result for a clip
   *
   * @param clipId - ID of the clip
   * @returns Cached result or undefined
   */
  getResult: (clipId: string) => TranscriptionResult | undefined;

  /**
   * Clear cached transcription result for a clip
   *
   * @param clipId - ID of the clip
   */
  clearResult: (clipId: string) => void;

  /**
   * Clear all cached transcription results
   */
  clearAllResults: () => void;

  /**
   * Apply transcription results to the captions store
   *
   * @param clipId - ID of the transcribed clip
   * @param trackId - ID of the caption track to add captions to
   * @param maxCharsPerCaption - Maximum characters per caption
   * @param maxDuration - Maximum duration per caption in seconds
   */
  applyToCaptions: (
    clipId: string,
    trackId: string,
    maxCharsPerCaption?: number,
    maxDuration?: number
  ) => void;

  /**
   * Set the current error
   *
   * @param error - Error message or null to clear
   */
  setError: (error: string | null) => void;

  /**
   * Reset the store to initial state
   */
  reset: () => void;
}

export type TranscriptionStore = TranscriptionState & TranscriptionActions;

/**
 * OpenCut transcription store for managing speech-to-text operations
 */
export const useTranscriptionStore = create<TranscriptionStore>((set, get) => ({
  results: new Map(),
  progress: null,
  isTranscribing: false,
  error: null,
  activeClipId: null,

  startTranscription: async (clipId, audioUrl, options = {}) => {
    // Don't start if already transcribing
    if (get().isTranscribing) {
      set({ error: 'Transcription already in progress' });
      return;
    }

    // Clear previous state
    set({
      isTranscribing: true,
      error: null,
      progress: null,
      activeClipId: clipId,
    });

    try {
      // Dynamically import the transcription service to avoid circular dependencies
      const { transcribeAudio, cancelTranscription } =
        await import('../services/transcriptionService');

      // Set up cancellation check
      const checkCancelled = () => !get().isTranscribing;

      const result = await transcribeAudio(audioUrl, options, (progress) => {
        // Check if cancelled
        if (checkCancelled()) {
          cancelTranscription();
          return;
        }

        set({ progress });
      });

      // Check if cancelled after completion
      if (checkCancelled()) {
        return;
      }

      // Store result
      set((state) => {
        const results = new Map(state.results);
        results.set(clipId, result);
        return {
          results,
          isTranscribing: false,
          progress: { status: 'complete', progress: 100, message: 'Transcription complete' },
        };
      });
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Unknown error during transcription';

      // Only set error if not cancelled
      if (get().isTranscribing) {
        set({
          error: errorMessage,
          isTranscribing: false,
          progress: { status: 'error', progress: 0, message: errorMessage },
        });
      }
    }
  },

  cancelTranscription: () => {
    // Import and call cancel
    import('../services/transcriptionService').then(({ cancelTranscription }) => {
      cancelTranscription();
    });

    set({
      isTranscribing: false,
      progress: null,
      error: 'Transcription cancelled',
      activeClipId: null,
    });
  },

  getResult: (clipId) => {
    return get().results.get(clipId);
  },

  clearResult: (clipId) => {
    set((state) => {
      const results = new Map(state.results);
      results.delete(clipId);
      return { results };
    });
  },

  clearAllResults: () => {
    set({ results: new Map() });
  },

  applyToCaptions: async (
    clipId: string,
    trackId: string,
    maxCharsPerCaption: number = 80,
    maxDuration: number = 5
  ) => {
    const result = get().getResult(clipId);
    if (!result) {
      set({ error: 'No transcription result found for this clip' });
      return;
    }

    try {
      // Import dependencies
      const { segmentsToCaptions } = await import('../services/transcriptionService');
      const { useOpenCutCaptionsStore } = await import('./opencutCaptions');

      // Convert segments to captions
      const captions = segmentsToCaptions(result.segments, maxCharsPerCaption, maxDuration);

      // Add captions to the store
      const captionsStore = useOpenCutCaptionsStore.getState();
      captionsStore.addCaptions(
        trackId,
        captions.map((c) => ({
          startTime: c.startTime,
          endTime: c.endTime,
          text: c.text,
        }))
      );

      // Clear the transcription result after applying
      get().clearResult(clipId);
    } catch (error: unknown) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to apply transcription to captions';
      set({ error: errorMessage });
    }
  },

  setError: (error) => {
    set({ error });
  },

  reset: () => {
    // Cancel any ongoing transcription
    get().cancelTranscription();

    set({
      results: new Map(),
      progress: null,
      isTranscribing: false,
      error: null,
      activeClipId: null,
    });
  },
}));
