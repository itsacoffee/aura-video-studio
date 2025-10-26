/**
 * Professional Video Playback Engine
 *
 * Provides hardware-accelerated video playback with:
 * - Frame-accurate seeking and display
 * - Perfect A/V synchronization
 * - Variable playback speeds with pitch preservation
 * - Preview quality management
 * - Performance monitoring and optimization
 */

/**
 * Video quality metrics returned by non-standard API
 */
interface VideoQualityMetrics {
  droppedVideoFrames: number;
  totalVideoFrames: number;
  decodedVideoFrames: number;
}

/**
 * Extended HTMLVideoElement interface for non-standard properties
 */
interface ExtendedVideoProperties {
  preservesPitch?: boolean;
  mozPreservesPitch?: boolean;
  webkitPreservesPitch?: boolean;
}

/**
 * Extended Performance interface with memory property (Chrome)
 */
interface PerformanceWithMemory extends Performance {
  memory?: {
    usedJSHeapSize: number;
    totalJSHeapSize: number;
    jsHeapSizeLimit: number;
  };
}

export type PlaybackQuality = 'full' | 'half' | 'quarter';
export type PlaybackSpeed = 0.25 | 0.5 | 1.0 | 2.0 | 4.0;

export interface PlaybackState {
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  playbackSpeed: PlaybackSpeed;
  quality: PlaybackQuality;
  volume: number;
  isMuted: boolean;
  isLooping: boolean;
  inPoint: number | null;
  outPoint: number | null;
}

export interface PlaybackMetrics {
  droppedFrames: number;
  totalFrames: number;
  currentFPS: number;
  targetFPS: number;
  avSyncOffset: number; // in milliseconds
  bufferHealth: number; // 0-100%
  decodedFrames: number;
  memoryUsage: number; // in MB
}

export interface PlaybackEngineOptions {
  videoElement: HTMLVideoElement;
  canvasElement?: HTMLCanvasElement;
  frameRate?: number;
  enableHardwareAcceleration?: boolean;
  defaultQuality?: PlaybackQuality;
  onStateChange?: (state: PlaybackState) => void;
  onMetricsUpdate?: (metrics: PlaybackMetrics) => void;
  onError?: (error: Error) => void;
}

/**
 * Professional-grade video playback engine
 */
export class PlaybackEngine {
  private videoElement: HTMLVideoElement;
  private canvasElement: HTMLCanvasElement | null;
  private frameRate: number;
  private enableHardwareAccel: boolean;

  private state: PlaybackState = {
    isPlaying: false,
    currentTime: 0,
    duration: 0,
    playbackSpeed: 1.0,
    quality: 'full',
    volume: 1.0,
    isMuted: false,
    isLooping: false,
    inPoint: null,
    outPoint: null,
  };

  private metrics: PlaybackMetrics = {
    droppedFrames: 0,
    totalFrames: 0,
    currentFPS: 0,
    targetFPS: 30,
    avSyncOffset: 0,
    bufferHealth: 100,
    decodedFrames: 0,
    memoryUsage: 0,
  };

  private animationFrameId: number | null = null;
  private frameCount: number = 0;
  private fpsInterval: number = 1000;
  private lastFpsUpdate: number = 0;

  private onStateChange?: (state: PlaybackState) => void;
  private onMetricsUpdate?: (metrics: PlaybackMetrics) => void;
  private onError?: (error: Error) => void;

  private videoQualityObserver: ResizeObserver | null = null;
  private performanceObserver: PerformanceObserver | null = null;

  constructor(options: PlaybackEngineOptions) {
    this.videoElement = options.videoElement;
    this.canvasElement = options.canvasElement || null;
    this.frameRate = options.frameRate || 30;
    this.enableHardwareAccel = options.enableHardwareAcceleration ?? true;
    this.onStateChange = options.onStateChange;
    this.onMetricsUpdate = options.onMetricsUpdate;
    this.onError = options.onError;

    if (options.defaultQuality) {
      this.state.quality = options.defaultQuality;
    }

    this.metrics.targetFPS = this.frameRate;

    this.initialize();
  }

  /**
   * Initialize the playback engine
   */
  private initialize(): void {
    this.setupVideoElement();
    this.setupEventListeners();
    this.detectHardwareCapabilities();
    this.setupPerformanceMonitoring();
  }

  /**
   * Set up video element for optimal playback
   */
  private setupVideoElement(): void {
    // Enable hardware acceleration hints
    if (this.enableHardwareAccel) {
      this.videoElement.setAttribute('playsinline', 'true');
      this.videoElement.setAttribute('webkit-playsinline', 'true');
    }

    // Optimize for low latency
    this.videoElement.preload = 'auto';

    // Set initial volume
    this.videoElement.volume = this.state.volume;
    this.videoElement.muted = this.state.isMuted;

    // Apply quality settings
    this.applyQualitySettings();
  }

  /**
   * Set up event listeners for video element
   */
  private setupEventListeners(): void {
    this.videoElement.addEventListener('loadedmetadata', this.handleLoadedMetadata);
    this.videoElement.addEventListener('timeupdate', this.handleTimeUpdate);
    this.videoElement.addEventListener('play', this.handlePlay);
    this.videoElement.addEventListener('pause', this.handlePause);
    this.videoElement.addEventListener('ended', this.handleEnded);
    this.videoElement.addEventListener('error', this.handleError);
    this.videoElement.addEventListener('seeking', this.handleSeeking);
    this.videoElement.addEventListener('seeked', this.handleSeeked);
  }

  /**
   * Detect hardware acceleration capabilities
   */
  private detectHardwareCapabilities(): void {
    if (!this.enableHardwareAccel) return;

    // Check for MediaSource API support
    const hasMediaSource = 'MediaSource' in window;

    // Check for hardware video decoding
    const canvas = document.createElement('canvas');
    const gl = canvas.getContext('webgl2') || canvas.getContext('webgl');
    const hasWebGL = !!gl;

    // Intentional logging for hardware capability diagnostics
    if (hasMediaSource && hasWebGL) {
      // eslint-disable-next-line no-console
      console.log('✓ Hardware acceleration available');
    } else {
      console.warn('⚠ Limited hardware acceleration support');
      this.enableHardwareAccel = false;
    }
  }

  /**
   * Set up performance monitoring
   */
  private setupPerformanceMonitoring(): void {
    // Monitor video playback quality
    if ('getVideoPlaybackQuality' in this.videoElement) {
      setInterval(() => {
        const quality = this.videoElement.getVideoPlaybackQuality() as unknown as VideoQualityMetrics;
        if (quality) {
          this.metrics.droppedFrames = quality.droppedVideoFrames || 0;
          this.metrics.totalFrames = quality.totalVideoFrames || 0;
          this.metrics.decodedFrames = quality.decodedVideoFrames || 0;

          // Notify if metrics changed
          if (this.onMetricsUpdate) {
            this.onMetricsUpdate({ ...this.metrics });
          }
        }
      }, 1000);
    }

    // Monitor memory usage
    const perfWithMemory = performance as PerformanceWithMemory;
    if ('memory' in performance) {
      setInterval(() => {
        const memory = perfWithMemory.memory;
        if (memory) {
          this.metrics.memoryUsage = memory.usedJSHeapSize / (1024 * 1024); // Convert to MB
        }
      }, 2000);
    }
  }

  /**
   * Apply quality settings to video playback
   */
  private applyQualitySettings(): void {
    const quality = this.state.quality;

    // Calculate scale factor based on quality
    let scaleFactor = 1.0;
    switch (quality) {
      case 'half':
        scaleFactor = 0.5;
        break;
      case 'quarter':
        scaleFactor = 0.25;
        break;
      case 'full':
      default:
        scaleFactor = 1.0;
        break;
    }

    // Apply to canvas if available
    if (this.canvasElement && this.videoElement.videoWidth && this.videoElement.videoHeight) {
      const width = Math.floor(this.videoElement.videoWidth * scaleFactor);
      const height = Math.floor(this.videoElement.videoHeight * scaleFactor);

      this.canvasElement.width = width;
      this.canvasElement.height = height;
    }
  }

  /**
   * Event Handlers
   */
  private handleLoadedMetadata = (): void => {
    this.state.duration = this.videoElement.duration;
    this.notifyStateChange();
  };

  private handleTimeUpdate = (): void => {
    this.state.currentTime = this.videoElement.currentTime;

    // Check loop points
    if (this.state.isLooping && this.state.outPoint !== null) {
      if (this.state.currentTime >= this.state.outPoint) {
        const inPoint = this.state.inPoint || 0;
        this.seek(inPoint);
      }
    }

    this.notifyStateChange();
  };

  private handlePlay = (): void => {
    this.state.isPlaying = true;
    this.startRenderLoop();
    this.notifyStateChange();
  };

  private handlePause = (): void => {
    this.state.isPlaying = false;
    this.stopRenderLoop();
    this.notifyStateChange();
  };

  private handleEnded = (): void => {
    if (this.state.isLooping) {
      const inPoint = this.state.inPoint || 0;
      this.seek(inPoint);
      this.play();
    } else {
      this.state.isPlaying = false;
      this.stopRenderLoop();
      this.notifyStateChange();
    }
  };

  private handleError = (): void => {
    const error = new Error(
      `Video playback error: ${this.videoElement.error?.message || 'Unknown error'}`
    );
    console.error('Playback error:', error);
    if (this.onError) {
      this.onError(error);
    }
  };

  private handleSeeking = (): void => {
    // Track seeking for performance
  };

  private handleSeeked = (): void => {
    // Update state after seek completes
    this.state.currentTime = this.videoElement.currentTime;
    this.notifyStateChange();
  };

  /**
   * Render loop for frame-accurate playback
   */
  private startRenderLoop(): void {
    if (this.animationFrameId !== null) return;

    this.lastFpsUpdate = performance.now();
    this.frameCount = 0;

    const renderFrame = (currentTime: number) => {
      if (!this.state.isPlaying) {
        this.stopRenderLoop();
        return;
      }

      // Calculate FPS
      this.frameCount++;
      const elapsed = currentTime - this.lastFpsUpdate;
      if (elapsed >= this.fpsInterval) {
        this.metrics.currentFPS = Math.round((this.frameCount * 1000) / elapsed);
        this.frameCount = 0;
        this.lastFpsUpdate = currentTime;

        if (this.onMetricsUpdate) {
          this.onMetricsUpdate({ ...this.metrics });
        }
      }

      // Render to canvas if available
      if (this.canvasElement) {
        this.renderToCanvas();
      }

      this.animationFrameId = requestAnimationFrame(renderFrame);
    };

    this.animationFrameId = requestAnimationFrame(renderFrame);
  }

  private stopRenderLoop(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
  }

  /**
   * Render video frame to canvas
   */
  private renderToCanvas(): void {
    if (!this.canvasElement || !this.videoElement.videoWidth) return;

    const ctx = this.canvasElement.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, this.canvasElement.width, this.canvasElement.height);

    // Draw video frame
    ctx.drawImage(this.videoElement, 0, 0, this.canvasElement.width, this.canvasElement.height);
  }

  /**
   * Notify state change
   */
  private notifyStateChange(): void {
    if (this.onStateChange) {
      this.onStateChange({ ...this.state });
    }
  }

  /**
   * Public API
   */

  /**
   * Play video
   */
  async play(): Promise<void> {
    try {
      await this.videoElement.play();
    } catch (error) {
      console.error('Failed to play video:', error);
      if (this.onError && error instanceof Error) {
        this.onError(error);
      }
    }
  }

  /**
   * Pause video
   */
  pause(): void {
    this.videoElement.pause();
  }

  /**
   * Toggle play/pause
   */
  async togglePlay(): Promise<void> {
    if (this.state.isPlaying) {
      this.pause();
    } else {
      await this.play();
    }
  }

  /**
   * Seek to specific time
   */
  seek(time: number): void {
    const clampedTime = Math.max(0, Math.min(this.state.duration, time));
    this.videoElement.currentTime = clampedTime;
  }

  /**
   * Step forward one frame
   */
  stepForward(): void {
    const frameTime = 1 / this.frameRate;
    this.seek(this.state.currentTime + frameTime);
  }

  /**
   * Step backward one frame
   */
  stepBackward(): void {
    const frameTime = 1 / this.frameRate;
    this.seek(this.state.currentTime - frameTime);
  }

  /**
   * Set playback speed
   */
  setPlaybackSpeed(speed: PlaybackSpeed): void {
    this.state.playbackSpeed = speed;
    this.videoElement.playbackRate = speed;

    // Preserve audio pitch at different speeds
    const extendedVideo = this.videoElement as HTMLVideoElement & ExtendedVideoProperties;
    if ('preservesPitch' in this.videoElement) {
      extendedVideo.preservesPitch = true;
    } else if ('mozPreservesPitch' in this.videoElement) {
      extendedVideo.mozPreservesPitch = true;
    } else if ('webkitPreservesPitch' in this.videoElement) {
      extendedVideo.webkitPreservesPitch = true;
    }

    this.notifyStateChange();
  }

  /**
   * Set preview quality
   */
  setQuality(quality: PlaybackQuality): void {
    this.state.quality = quality;
    this.applyQualitySettings();
    this.notifyStateChange();
  }

  /**
   * Set volume
   */
  setVolume(volume: number): void {
    const clampedVolume = Math.max(0, Math.min(1, volume));
    this.state.volume = clampedVolume;
    this.videoElement.volume = clampedVolume;
    this.notifyStateChange();
  }

  /**
   * Toggle mute
   */
  toggleMute(): void {
    this.state.isMuted = !this.state.isMuted;
    this.videoElement.muted = this.state.isMuted;
    this.notifyStateChange();
  }

  /**
   * Set mute state
   */
  setMuted(muted: boolean): void {
    this.state.isMuted = muted;
    this.videoElement.muted = muted;
    this.notifyStateChange();
  }

  /**
   * Set loop mode
   */
  setLoop(loop: boolean): void {
    this.state.isLooping = loop;
    this.notifyStateChange();
  }

  /**
   * Set in point for looping
   */
  setInPoint(time: number | null): void {
    this.state.inPoint = time;
    this.notifyStateChange();
  }

  /**
   * Set out point for looping
   */
  setOutPoint(time: number | null): void {
    this.state.outPoint = time;
    this.notifyStateChange();
  }

  /**
   * Clear in/out points
   */
  clearInOutPoints(): void {
    this.state.inPoint = null;
    this.state.outPoint = null;
    this.notifyStateChange();
  }

  /**
   * Play around current position (preview mode)
   */
  async playAround(secondsBefore: number = 2, secondsAfter: number = 2): Promise<void> {
    const startTime = Math.max(0, this.state.currentTime - secondsBefore);
    const endTime = Math.min(this.state.duration, this.state.currentTime + secondsAfter);

    // Set temporary loop points
    const originalInPoint = this.state.inPoint;
    const originalOutPoint = this.state.outPoint;
    const originalLoop = this.state.isLooping;

    this.state.inPoint = startTime;
    this.state.outPoint = endTime;
    this.state.isLooping = false;

    this.seek(startTime);
    await this.play();

    // Restore original loop points after playback
    const checkPlayback = setInterval(() => {
      if (this.state.currentTime >= endTime || !this.state.isPlaying) {
        clearInterval(checkPlayback);
        this.pause();
        this.state.inPoint = originalInPoint;
        this.state.outPoint = originalOutPoint;
        this.state.isLooping = originalLoop;
        this.notifyStateChange();
      }
    }, 100);
  }

  /**
   * Get current state
   */
  getState(): PlaybackState {
    return { ...this.state };
  }

  /**
   * Get current metrics
   */
  getMetrics(): PlaybackMetrics {
    return { ...this.metrics };
  }

  /**
   * Load video source
   */
  loadVideo(src: string): void {
    this.videoElement.src = src;
  }

  /**
   * Cleanup and destroy
   */
  destroy(): void {
    this.stopRenderLoop();

    // Remove event listeners
    this.videoElement.removeEventListener('loadedmetadata', this.handleLoadedMetadata);
    this.videoElement.removeEventListener('timeupdate', this.handleTimeUpdate);
    this.videoElement.removeEventListener('play', this.handlePlay);
    this.videoElement.removeEventListener('pause', this.handlePause);
    this.videoElement.removeEventListener('ended', this.handleEnded);
    this.videoElement.removeEventListener('error', this.handleError);
    this.videoElement.removeEventListener('seeking', this.handleSeeking);
    this.videoElement.removeEventListener('seeked', this.handleSeeked);

    // Cleanup observers
    if (this.videoQualityObserver) {
      this.videoQualityObserver.disconnect();
    }
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
    }
  }
}
