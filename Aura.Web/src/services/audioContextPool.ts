/**
 * Audio Context Pool
 *
 * Manages a single shared AudioContext instance to prevent memory leaks
 * from creating multiple AudioContext instances across components.
 *
 * Pattern: Singleton with reference counting
 */

class AudioContextPool {
  private static instance: AudioContextPool | null = null;
  private audioContext: AudioContext | null = null;
  private referenceCount = 0;

  private constructor() {
    // Private constructor to enforce singleton
  }

  public static getInstance(): AudioContextPool {
    if (!AudioContextPool.instance) {
      AudioContextPool.instance = new AudioContextPool();
    }
    return AudioContextPool.instance;
  }

  /**
   * Get or create the shared AudioContext instance
   */
  public getContext(): AudioContext {
    if (!this.audioContext) {
      const AudioContextClass =
        window.AudioContext ||
        (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;

      if (!AudioContextClass) {
        throw new Error('AudioContext not supported in this browser');
      }

      this.audioContext = new AudioContextClass();

      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.log('[AudioContextPool] Created new AudioContext');
      }
    }

    this.referenceCount++;

    if (import.meta.env.DEV) {
      // eslint-disable-next-line no-console
      console.log(`[AudioContextPool] Reference count: ${this.referenceCount}`);
    }

    return this.audioContext;
  }

  /**
   * Release a reference to the AudioContext
   * When all references are released, the context is closed
   */
  public releaseContext(): void {
    this.referenceCount--;

    if (import.meta.env.DEV) {
      // eslint-disable-next-line no-console
      console.log(`[AudioContextPool] Reference count: ${this.referenceCount}`);
    }

    // Close context when no more references (with debounce)
    if (this.referenceCount <= 0 && this.audioContext) {
      this.referenceCount = 0;

      // Debounce closing to avoid rapid open/close cycles
      setTimeout(() => {
        if (this.referenceCount === 0 && this.audioContext) {
          if (import.meta.env.DEV) {
            // eslint-disable-next-line no-console
            console.log('[AudioContextPool] Closing AudioContext (no more references)');
          }

          this.audioContext.close();
          this.audioContext = null;
        }
      }, 5000); // 5 second debounce
    }
  }

  /**
   * Force close the AudioContext (called on app unmount)
   */
  public forceClose(): void {
    if (this.audioContext) {
      if (import.meta.env.DEV) {
        // eslint-disable-next-line no-console
        console.log('[AudioContextPool] Force closing AudioContext');
      }

      this.audioContext.close();
      this.audioContext = null;
      this.referenceCount = 0;
    }
  }

  /**
   * Get current state for debugging
   */
  public getState(): { hasContext: boolean; referenceCount: number } {
    return {
      hasContext: this.audioContext !== null,
      referenceCount: this.referenceCount,
    };
  }
}

// Export singleton instance
export const audioContextPool = AudioContextPool.getInstance();

/**
 * Hook to use the pooled AudioContext
 * Automatically acquires and releases the context
 *
 * @example
 * ```tsx
 * const MyComponent = () => {
 *   const audioContext = useAudioContext();
 *
 *   useEffect(() => {
 *     if (audioContext) {
 *       // Use audio context
 *     }
 *   }, [audioContext]);
 *
 *   return <div>...</div>;
 * };
 * ```
 */
export function useAudioContext(): AudioContext | null {
  const [context, setContext] = useState<AudioContext | null>(null);

  useEffect(() => {
    let ctx: AudioContext | null = null;

    try {
      ctx = audioContextPool.getContext();
      setContext(ctx);
    } catch (error) {
      console.error('Failed to get AudioContext:', error);
    }

    return () => {
      if (ctx) {
        audioContextPool.releaseContext();
      }
    };
  }, []);

  return context;
}

// Import for the hook
import { useState, useEffect } from 'react';
