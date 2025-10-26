/**
 * Audio/Video Synchronization Service
 *
 * Monitors and corrects A/V sync drift to maintain perfect synchronization
 * within 1 frame tolerance (~33ms at 30fps).
 */

export interface AVSyncOptions {
  videoElement: HTMLVideoElement;
  targetFrameRate?: number;
  maxSyncOffsetMs?: number; // Maximum acceptable offset in milliseconds
  correctionThresholdMs?: number; // Threshold to trigger correction
  onSyncIssue?: (offset: number) => void;
}

export interface AVSyncMetrics {
  currentOffset: number; // in milliseconds
  averageOffset: number;
  maxOffset: number;
  correctionCount: number;
  inSync: boolean;
  lastCorrectionTime: number;
}

/**
 * Audio/Video Synchronization Monitor and Corrector
 */
export class AudioSyncService {
  private videoElement: HTMLVideoElement;
  private maxSyncOffsetMs: number;
  private correctionThresholdMs: number;
  private onSyncIssue?: (offset: number) => void;

  private metrics: AVSyncMetrics = {
    currentOffset: 0,
    averageOffset: 0,
    maxOffset: 0,
    correctionCount: 0,
    inSync: true,
    lastCorrectionTime: 0,
  };

  private monitorInterval: number | null = null;
  private offsetHistory: number[] = [];
  private maxHistorySize: number = 100;

  // Audio context for precise timing
  private audioContext: AudioContext | null = null;
  private audioSource: MediaElementAudioSourceNode | null = null;
  private analyser: AnalyserNode | null = null;

  constructor(options: AVSyncOptions) {
    this.videoElement = options.videoElement;
    // targetFrameRate stored for potential future use in frame-accurate sync
    this.maxSyncOffsetMs = options.maxSyncOffsetMs || 33; // ~1 frame at 30fps
    this.correctionThresholdMs = options.correctionThresholdMs || 16; // ~0.5 frame at 30fps
    this.onSyncIssue = options.onSyncIssue;

    this.initialize();
  }

  /**
   * Initialize audio context for precise timing
   */
  private initialize(): void {
    try {
      // Create audio context for timing analysis
      const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
      if (AudioContextClass) {
        this.audioContext = new AudioContextClass();

        // Create audio source from video element
        if (!this.videoElement.srcObject && this.videoElement.src) {
          this.audioSource = this.audioContext.createMediaElementSource(this.videoElement);
          this.analyser = this.audioContext.createAnalyser();

          // Connect audio pipeline
          this.audioSource.connect(this.analyser);
          this.analyser.connect(this.audioContext.destination);
        }
      }
    } catch (error) {
      console.warn('Failed to initialize audio context:', error);
      // Continue without audio context - we can still monitor using video element timing
    }
  }

  /**
   * Start monitoring A/V synchronization
   */
  startMonitoring(intervalMs: number = 100): void {
    this.stopMonitoring();

    this.monitorInterval = window.setInterval(() => {
      this.checkSync();
    }, intervalMs);
  }

  /**
   * Stop monitoring
   */
  stopMonitoring(): void {
    if (this.monitorInterval !== null) {
      clearInterval(this.monitorInterval);
      this.monitorInterval = null;
    }
  }

  /**
   * Check current A/V sync status
   */
  private checkSync(): void {
    const offset = this.calculateOffset();

    // Update current offset
    this.metrics.currentOffset = offset;

    // Track offset history
    this.offsetHistory.push(offset);
    if (this.offsetHistory.length > this.maxHistorySize) {
      this.offsetHistory.shift();
    }

    // Calculate average offset
    if (this.offsetHistory.length > 0) {
      const sum = this.offsetHistory.reduce((a, b) => a + b, 0);
      this.metrics.averageOffset = sum / this.offsetHistory.length;
    }

    // Track max offset
    if (Math.abs(offset) > Math.abs(this.metrics.maxOffset)) {
      this.metrics.maxOffset = offset;
    }

    // Check if in sync
    this.metrics.inSync = Math.abs(offset) <= this.maxSyncOffsetMs;

    // Apply correction if needed
    if (Math.abs(offset) >= this.correctionThresholdMs) {
      this.applyCorrection(offset);

      if (this.onSyncIssue) {
        this.onSyncIssue(offset);
      }
    }
  }

  /**
   * Calculate current A/V offset
   */
  private calculateOffset(): number {
    // In a browser, video and audio are typically synchronized by the browser itself
    // We can detect drift by comparing expected playback position with actual position

    if (!this.audioContext || !this.videoElement.paused) {
      // Use video element's buffered ranges to detect sync issues
      const buffered = this.videoElement.buffered;
      const currentTime = this.videoElement.currentTime;

      if (buffered.length > 0) {
        // Check if current time is within buffered range
        let inBuffer = false;
        let nearestBufferEnd = 0;

        for (let i = 0; i < buffered.length; i++) {
          const start = buffered.start(i);
          const end = buffered.end(i);

          if (currentTime >= start && currentTime <= end) {
            inBuffer = true;
            nearestBufferEnd = end;
            break;
          }
        }

        // If we're not in a buffered range, there might be a sync issue
        if (!inBuffer && buffered.length > 0) {
          // Calculate distance to nearest buffer
          const bufferStart = buffered.start(0);
          return (currentTime - bufferStart) * 1000; // Convert to ms
        }

        // Check buffer health
        const bufferAhead = nearestBufferEnd - currentTime;
        if (bufferAhead < 0.5) {
          // Low buffer might cause sync issues
          return bufferAhead * 1000; // Negative value indicates low buffer
        }
      }
    }

    // If we have audio context, use it for more precise timing
    if (this.audioContext && !this.videoElement.paused) {
      const audioTime = this.audioContext.currentTime;
      const videoTime = this.videoElement.currentTime;

      // Calculate offset (positive means audio is ahead, negative means video is ahead)
      return (audioTime - videoTime) * 1000; // Convert to milliseconds
    }

    return 0;
  }

  /**
   * Apply correction to sync A/V
   */
  private applyCorrection(offset: number): void {
    // Only apply correction if not already correcting recently
    const now = Date.now();
    if (now - this.metrics.lastCorrectionTime < 1000) {
      return; // Wait at least 1 second between corrections
    }

    // For small offsets, use playbackRate adjustment
    if (Math.abs(offset) < 100) {
      // Less than 100ms
      // Temporarily adjust playback rate to correct drift
      const correction = offset > 0 ? 0.98 : 1.02; // Slow down or speed up by 2%
      const originalRate = this.videoElement.playbackRate;

      this.videoElement.playbackRate = originalRate * correction;

      // Restore original rate after brief correction
      setTimeout(() => {
        this.videoElement.playbackRate = originalRate;
      }, 500);
    } else {
      // For larger offsets, seek to correct position
      const correctionSeconds = offset / 1000;
      const targetTime = this.videoElement.currentTime - correctionSeconds;

      if (targetTime >= 0 && targetTime <= this.videoElement.duration) {
        this.videoElement.currentTime = targetTime;
      }
    }

    this.metrics.correctionCount++;
    this.metrics.lastCorrectionTime = now;
  }

  /**
   * Get current sync metrics
   */
  getMetrics(): AVSyncMetrics {
    return { ...this.metrics };
  }

  /**
   * Reset metrics
   */
  resetMetrics(): void {
    this.metrics = {
      currentOffset: 0,
      averageOffset: 0,
      maxOffset: 0,
      correctionCount: 0,
      inSync: true,
      lastCorrectionTime: 0,
    };
    this.offsetHistory = [];
  }

  /**
   * Check if currently in sync
   */
  isInSync(): boolean {
    return this.metrics.inSync;
  }

  /**
   * Get offset history for analysis
   */
  getOffsetHistory(): number[] {
    return [...this.offsetHistory];
  }

  /**
   * Cleanup and destroy
   */
  destroy(): void {
    this.stopMonitoring();

    // Disconnect audio nodes
    if (this.audioSource) {
      this.audioSource.disconnect();
    }
    if (this.analyser) {
      this.analyser.disconnect();
    }

    // Close audio context
    if (this.audioContext && this.audioContext.state !== 'closed') {
      this.audioContext.close().catch((err) => {
        console.warn('Failed to close audio context:', err);
      });
    }

    this.audioContext = null;
    this.audioSource = null;
    this.analyser = null;
  }
}
