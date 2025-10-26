import { useState, useRef, useEffect, memo, useMemo, useImperativeHandle, forwardRef } from 'react';
import { makeStyles, tokens, Text } from '@fluentui/react-components';
import { AppliedEffect } from '../../types/effects';
import { applyEffectsToFrame } from '../../utils/effectsEngine';
import { PlaybackEngine, PlaybackState, PlaybackMetrics } from '../../services/playbackEngine';
import { AudioSyncService } from '../../services/audioSyncService';
import { PlaybackControls } from '../VideoPreview/PlaybackControls';
import { TransportBar } from '../VideoPreview/TransportBar';
import type { PlaybackSpeed, PlaybackQuality } from '../../services/playbackEngine';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground6,
  },
  videoContainer: {
    flex: 1,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#000',
    position: 'relative',
    overflow: 'hidden',
  },
  video: {
    maxWidth: '100%',
    maxHeight: '100%',
    objectFit: 'contain',
    transition: 'transform 0.2s ease',
    display: 'none', // Hide original video when effects are active
  },
  canvas: {
    maxWidth: '100%',
    maxHeight: '100%',
    objectFit: 'contain',
    transition: 'transform 0.2s ease',
  },
  placeholder: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase500,
  },
});

interface VideoPreviewPanelProps {
  videoUrl?: string;
  currentTime?: number;
  effects?: AppliedEffect[];
  onTimeUpdate?: (time: number) => void;
  onPlay?: () => void;
  onPause?: () => void;
  onStop?: () => void;
}

export interface VideoPreviewPanelHandle {
  play: () => void;
  pause: () => void;
  stepForward: () => void;
  stepBackward: () => void;
  setPlaybackRate: (rate: number) => void;
  playAround: (secondsBefore?: number, secondsAfter?: number) => void;
}

const VideoPreviewPanelInner = forwardRef<VideoPreviewPanelHandle, VideoPreviewPanelProps>(function VideoPreviewPanel({
  videoUrl,
  currentTime = 0,
  effects = [],
  onTimeUpdate,
  onPlay,
  onPause,
}: VideoPreviewPanelProps, ref) {
  const styles = useStyles();
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const playbackEngineRef = useRef<PlaybackEngine | null>(null);
  const audioSyncServiceRef = useRef<AudioSyncService | null>(null);
  
  // State from playback engine
  const [playbackState, setPlaybackState] = useState<PlaybackState>({
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
  });
  
  const [metrics, setMetrics] = useState<PlaybackMetrics>({
    droppedFrames: 0,
    totalFrames: 0,
    currentFPS: 0,
    targetFPS: 30,
    avSyncOffset: 0,
    bufferHealth: 100,
    decodedFrames: 0,
    memoryUsage: 0,
  });

  // Memoize effects to prevent unnecessary re-renders
  const memoizedEffects = useMemo(() => effects, [effects]);
  const hasEffects = useMemo(() => memoizedEffects.length > 0, [memoizedEffects]);

  // Expose imperative methods via ref
  useImperativeHandle(ref, () => ({
    play: () => {
      playbackEngineRef.current?.play();
    },
    pause: () => {
      playbackEngineRef.current?.pause();
    },
    stepForward: () => {
      playbackEngineRef.current?.stepForward();
    },
    stepBackward: () => {
      playbackEngineRef.current?.stepBackward();
    },
    setPlaybackRate: (rate: number) => {
      // Map rate to closest PlaybackSpeed
      let speed: PlaybackSpeed = 1.0;
      if (rate <= 0.375) speed = 0.25;
      else if (rate <= 0.75) speed = 0.5;
      else if (rate <= 1.5) speed = 1.0;
      else if (rate <= 3.0) speed = 2.0;
      else speed = 4.0;
      
      playbackEngineRef.current?.setPlaybackSpeed(speed);
    },
    playAround: (secondsBefore = 2, secondsAfter = 2) => {
      playbackEngineRef.current?.playAround(secondsBefore, secondsAfter);
    },
  }), []);

  // Initialize playback engine
  useEffect(() => {
    if (!videoRef.current || !canvasRef.current) return;
    
    // Create playback engine
    const engine = new PlaybackEngine({
      videoElement: videoRef.current,
      canvasElement: canvasRef.current,
      frameRate: 30,
      enableHardwareAcceleration: true,
      defaultQuality: 'full',
      onStateChange: (state) => {
        setPlaybackState(state);
        onTimeUpdate?.(state.currentTime);
      },
      onMetricsUpdate: (newMetrics) => {
        setMetrics(newMetrics);
      },
      onError: (error) => {
        console.error('Playback error:', error);
      },
    });
    
    playbackEngineRef.current = engine;
    
    // Create audio sync service
    const audioSync = new AudioSyncService({
      videoElement: videoRef.current,
      targetFrameRate: 30,
      maxSyncOffsetMs: 33, // ~1 frame at 30fps
      onSyncIssue: (offset) => {
        console.warn('A/V sync issue detected:', offset, 'ms');
      },
    });
    
    audioSyncServiceRef.current = audioSync;
    audioSync.startMonitoring();
    
    return () => {
      engine.destroy();
      audioSync.destroy();
      playbackEngineRef.current = null;
      audioSyncServiceRef.current = null;
    };
  }, [onTimeUpdate]);
  
  // Load video source
  useEffect(() => {
    if (videoUrl && playbackEngineRef.current) {
      playbackEngineRef.current.loadVideo(videoUrl);
    }
  }, [videoUrl]);
  
  // Sync external current time with video
  useEffect(() => {
    if (playbackEngineRef.current && currentTime !== playbackState.currentTime) {
      playbackEngineRef.current.seek(currentTime);
    }
  }, [currentTime, playbackState.currentTime]);

  // Apply effects to video frame
  useEffect(() => {
    if (!videoRef.current || !canvasRef.current) return;
    
    const video = videoRef.current;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const renderFrame = () => {
      if (video.paused && !video.ended) return;
      
      // Set canvas size to match video
      if (canvas.width !== video.videoWidth || canvas.height !== video.videoHeight) {
        canvas.width = video.videoWidth || 640;
        canvas.height = video.videoHeight || 480;
      }

      // Draw video frame to canvas
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

      // Apply effects if any
      if (hasEffects) {
        const sourceCanvas = document.createElement('canvas');
        sourceCanvas.width = canvas.width;
        sourceCanvas.height = canvas.height;
        const sourceCtx = sourceCanvas.getContext('2d');
        if (sourceCtx) {
          sourceCtx.drawImage(canvas, 0, 0);
          const effectCanvas = applyEffectsToFrame(sourceCanvas, memoizedEffects, playbackState.currentTime);
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(effectCanvas, 0, 0);
        }
      }

      if (playbackState.isPlaying) {
        requestAnimationFrame(renderFrame);
      }
    };

    if (playbackState.isPlaying || hasEffects) {
      renderFrame();
    }
  }, [playbackState.isPlaying, playbackState.currentTime, hasEffects, memoizedEffects]);

  // Playback control handlers
  const handlePlay = () => {
    playbackEngineRef.current?.play();
    onPlay?.();
  };

  const handlePause = () => {
    playbackEngineRef.current?.pause();
    onPause?.();
  };

  const handleStepBackward = () => {
    playbackEngineRef.current?.stepBackward();
  };

  const handleStepForward = () => {
    playbackEngineRef.current?.stepForward();
  };

  const handleSpeedChange = (speed: PlaybackSpeed) => {
    playbackEngineRef.current?.setPlaybackSpeed(speed);
  };

  const handleQualityChange = (quality: PlaybackQuality) => {
    playbackEngineRef.current?.setQuality(quality);
  };

  const handleToggleLoop = () => {
    playbackEngineRef.current?.setLoop(!playbackState.isLooping);
  };

  const handleSeek = (time: number) => {
    playbackEngineRef.current?.seek(time);
    onTimeUpdate?.(time);
  };

  const handleSetInPoint = () => {
    playbackEngineRef.current?.setInPoint(playbackState.currentTime);
  };

  const handleSetOutPoint = () => {
    playbackEngineRef.current?.setOutPoint(playbackState.currentTime);
  };

  const handleClearInOutPoints = () => {
    playbackEngineRef.current?.clearInOutPoints();
  };

  return (
    <div className={styles.container}>
      <div className={styles.videoContainer}>
        {videoUrl ? (
          <>
            <video
              ref={videoRef}
              className={styles.video}
              src={videoUrl}
              style={{ 
                display: hasEffects ? 'none' : 'block',
              }}
            >
              <track kind="captions" />
            </video>
            <canvas
              ref={canvasRef}
              className={styles.canvas}
              style={{ 
                display: hasEffects ? 'block' : 'none',
              }}
            />
          </>
        ) : (
          <Text className={styles.placeholder}>No video loaded</Text>
        )}
      </div>

      {/* Transport Bar */}
      <TransportBar
        currentTime={playbackState.currentTime}
        duration={playbackState.duration}
        inPoint={playbackState.inPoint}
        outPoint={playbackState.outPoint}
        onSeek={handleSeek}
        onSetInPoint={handleSetInPoint}
        onSetOutPoint={handleSetOutPoint}
        onClearInOutPoints={handleClearInOutPoints}
        disabled={!videoUrl}
      />

      {/* Playback Controls */}
      <PlaybackControls
        isPlaying={playbackState.isPlaying}
        playbackSpeed={playbackState.playbackSpeed}
        quality={playbackState.quality}
        isLooping={playbackState.isLooping}
        hasInOutPoints={playbackState.inPoint !== null && playbackState.outPoint !== null}
        droppedFrames={metrics.droppedFrames}
        currentFPS={metrics.currentFPS}
        targetFPS={metrics.targetFPS}
        onPlay={handlePlay}
        onPause={handlePause}
        onStepBackward={handleStepBackward}
        onStepForward={handleStepForward}
        onSpeedChange={handleSpeedChange}
        onQualityChange={handleQualityChange}
        onToggleLoop={handleToggleLoop}
        disabled={!videoUrl}
      />
    </div>
  );
});

// Export memoized component with ref forwarding
export const VideoPreviewPanel = memo(VideoPreviewPanelInner);
