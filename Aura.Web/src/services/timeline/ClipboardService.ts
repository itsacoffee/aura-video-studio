/**
 * Clipboard service for copy/paste operations
 */

import type { TimelineScene } from '../../types/timeline';

export interface ClipboardData {
  scenes: TimelineScene[];
  timestamp: number;
}

export class ClipboardService {
  private clipboardData: ClipboardData | null = null;
  private readonly storageKey = 'aura-timeline-clipboard';

  /**
   * Copy scenes to clipboard
   */
  public copy(scenes: TimelineScene[]): void {
    this.clipboardData = {
      scenes: JSON.parse(JSON.stringify(scenes)), // Deep clone
      timestamp: Date.now(),
    };

    // Also save to localStorage as backup
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(this.clipboardData));
    } catch (error) {
      console.warn('Failed to save clipboard to localStorage:', error);
    }
  }

  /**
   * Paste scenes at specified time
   */
  public paste(insertTime: number): TimelineScene[] | null {
    const data = this.getClipboardData();
    if (!data || data.scenes.length === 0) return null;

    // Adjust scene timings to start at insert time
    let currentTime = insertTime;
    return data.scenes.map((scene) => {
      const result = {
        ...scene,
        index: -1, // Will be set by caller
        start: currentTime,
      };
      currentTime += scene.duration;
      return result;
    });
  }

  /**
   * Duplicate scenes immediately after original
   */
  public duplicate(scenes: TimelineScene[], afterTime: number): TimelineScene[] {
    // Copy scenes
    this.copy(scenes);

    // Paste immediately after
    const duplicated = this.paste(afterTime);
    return duplicated || [];
  }

  /**
   * Check if clipboard has data
   */
  public hasData(): boolean {
    return this.getClipboardData() !== null;
  }

  /**
   * Clear clipboard
   */
  public clear(): void {
    this.clipboardData = null;
    try {
      localStorage.removeItem(this.storageKey);
    } catch (error) {
      console.warn('Failed to clear clipboard from localStorage:', error);
    }
  }

  /**
   * Get clipboard data (from memory or localStorage)
   */
  private getClipboardData(): ClipboardData | null {
    if (this.clipboardData) {
      return this.clipboardData;
    }

    // Try to restore from localStorage
    try {
      const stored = localStorage.getItem(this.storageKey);
      if (stored) {
        this.clipboardData = JSON.parse(stored);
        return this.clipboardData;
      }
    } catch (error) {
      console.warn('Failed to restore clipboard from localStorage:', error);
    }

    return null;
  }
}

// Singleton instance
export const clipboardService = new ClipboardService();
