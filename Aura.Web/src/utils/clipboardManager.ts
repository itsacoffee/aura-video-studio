/**
 * ClipboardManager - Manages clipboard operations for timeline clips
 *
 * Provides cut/copy/paste functionality for timeline clips with support
 * for single clips and multiple clip selections.
 */

interface ClipboardData {
  type: 'clip' | 'clips';
  data: unknown;
  timestamp: number;
}

class ClipboardManager {
  private clipboard: ClipboardData | null = null;

  /**
   * Copy data to the clipboard
   * @param data - The clip or clips to copy
   */
  copy(data: unknown): void {
    this.clipboard = {
      type: Array.isArray(data) ? 'clips' : 'clip',
      data,
      timestamp: Date.now(),
    };
  }

  /**
   * Paste data from the clipboard
   * @returns The stored clip data, or null if clipboard is empty
   */
  paste(): unknown | null {
    return this.clipboard?.data || null;
  }

  /**
   * Cut data (copy and mark for removal)
   * @param data - The clip or clips to cut
   * @returns The cut data
   */
  cut(data: unknown): unknown {
    this.copy(data);
    return data;
  }

  /**
   * Check if the clipboard has data
   * @returns True if clipboard contains data
   */
  hasData(): boolean {
    return this.clipboard !== null;
  }

  /**
   * Clear the clipboard
   */
  clear(): void {
    this.clipboard = null;
  }

  /**
   * Get the type of data in the clipboard
   * @returns 'clip', 'clips', or null if empty
   */
  getDataType(): 'clip' | 'clips' | null {
    return this.clipboard?.type || null;
  }

  /**
   * Get the timestamp of when data was copied
   * @returns Timestamp in milliseconds, or null if clipboard is empty
   */
  getTimestamp(): number | null {
    return this.clipboard?.timestamp || null;
  }
}

export const clipboardManager = new ClipboardManager();
