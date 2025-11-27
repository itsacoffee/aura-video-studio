import { makeStyles, tokens, Text } from '@fluentui/react-components';
import {
  useState,
  useRef,
  useEffect,
  memo,
  useMemo,
  useImperativeHandle,
  forwardRef,
  useCallback,
} from 'react';
import { usePreviewContextMenu } from '../../hooks/usePreviewContextMenu';
import { AudioSyncService } from '../../services/audioSyncService';
import { PlaybackEngine, PlaybackState, PlaybackMetrics } from '../../services/playbackEngine';
import type { PlaybackSpeed, PlaybackQuality } from '../../services/playbackEngine';
import { AppliedEffect } from '../../types/effects';
import { applyEffectsToFrame } from '../../utils/effectsEngine';
import { Marker } from '../VideoEditor/MarkerList';
import { ZoomControls } from '../VideoEditor/ZoomControls';
import { PlaybackControls } from '../VideoPreview/PlaybackControls';
import { TransportBar } from '../VideoPreview/TransportBar';

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
  videoWrapper: {
    transition: 'transform 0.2s ease',
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
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase600,
    fontWeight: tokens.fontWeightMedium,
  },
  zoomBar: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
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
  onMarkerAdded?: (marker: Omit<Marker, 'id'>) => void;
  onFrameExported?: (success: boolean, filePath?: string, error?: string) => void;
}

export interface VideoPreviewPanelHandle {
  play: () => void;
  pause: () => void;
  stepForward: () => void;
  stepBackward: () => void;
  setPlaybackRate: (rate: number) => void;
  playAround: (secondsBefore?: number, secondsAfter?: number) => void;
  addMarker: () => void;
}

const VideoPreviewPanelInner = forwardRef<VideoPreviewPanelHandle, VideoPreviewPanelProps>(
  function VideoPreviewPanel(
    {
      videoUrl,
      currentTime = 0,
      effects = [],
      onTimeUpdate,
      onPlay,
      onPause,
      onMarkerAdded,
      onFrameExported,
    }: VideoPreviewPanelProps,
    ref
  ) {
    const styles = useStyles();
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const playbackEngineRef = useRef<PlaybackEngine | null>(null);
    const audioSyncServiceRef = useRef<AudioSyncService | null>(null);

    // Zoom state for preview
    const [zoom, setZoom] = useState(1.0);

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

    // Format time helper for markers
    const formatTime = useCallback((seconds: number): string => {
      const mins = Math.floor(seconds / 60);
      const secs = Math.floor(seconds % 60);
      return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }, []);

    // Handler for adding markers
    const handleAddMarker = useCallback(
      (time: number) => {
        if (onMarkerAdded) {
          onMarkerAdded({
            time,
            label: `Marker at ${formatTime(time)}`,
          });
        }
      },
      [onMarkerAdded, formatTime]
    );

    // Handler for exporting the current frame as an image
    const handleExportFrame = useCallback(
      async (time: number) => {
        if (!videoRef.current && !canvasRef.current) {
          onFrameExported?.(false, undefined, 'No video source available');
          return;
        }

        try {
          // Use canvas if effects are applied, otherwise capture from video
          const sourceElement = hasEffects ? canvasRef.current : videoRef.current;
          if (!sourceElement) {
            onFrameExported?.(false, undefined, 'Source element not available');
            return;
          }

          // Create a temporary canvas to capture the frame
          const exportCanvas = document.createElement('canvas');
          if (sourceElement instanceof HTMLVideoElement) {
            exportCanvas.width = sourceElement.videoWidth || 1920;
            exportCanvas.height = sourceElement.videoHeight || 1080;
          } else {
            exportCanvas.width = sourceElement.width || 1920;
            exportCanvas.height = sourceElement.height || 1080;
          }

          const ctx = exportCanvas.getContext('2d');
          if (!ctx) {
            onFrameExported?.(false, undefined, 'Failed to get canvas context');
            return;
          }

          ctx.drawImage(sourceElement, 0, 0, exportCanvas.width, exportCanvas.height);

          // Convert canvas to blob
          exportCanvas.toBlob(
            async (blob) => {
              if (!blob) {
                console.error('Failed to create blob from canvas');
                onFrameExported?.(false, undefined, 'Failed to create image blob');
                return;
              }

              // Check if Electron API is available for native save dialog
              if (window.electron?.dialogs?.showSaveDialog) {
                const result = await window.electron.dialogs.showSaveDialog({
                  defaultPath: `frame-${formatTime(time).replace(':', '-')}.png`,
                  filters: [{ name: 'PNG Image', extensions: ['png'] }],
                });

                if (result.canceled) {
                  // User cancelled, not an error
                  return;
                }

                if (result.filePath && window.electron?.fs?.writeFile) {
                  const buffer = await blob.arrayBuffer();
                  const writeResult = await window.electron.fs.writeFile(result.filePath, buffer);
                  if (writeResult.success) {
                    console.info('Frame exported to:', result.filePath);
                    onFrameExported?.(true, result.filePath);
                  } else {
                    console.error('Failed to write file:', writeResult.error);
                    onFrameExported?.(false, undefined, writeResult.error);
                  }
                }
              } else {
                // Fallback to browser download
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `frame-${formatTime(time).replace(':', '-')}.png`;
                a.click();
                URL.revokeObjectURL(url);
                // Notify success for browser download
                onFrameExported?.(true, a.download);
              }
            },
            'image/png',
            1.0
          );
        } catch (error: unknown) {
          const errorMessage = error instanceof Error ? error.message : String(error);
          console.error('Failed to export frame:', errorMessage);
          onFrameExported?.(false, undefined, errorMessage);
        }
      },
      [hasEffects, formatTime, onFrameExported]
    );

    // Handler for toggling playback
    const handleTogglePlayback = useCallback(() => {
      if (playbackState.isPlaying) {
        playbackEngineRef.current?.pause();
        onPause?.();
      } else {
        playbackEngineRef.current?.play();
        onPlay?.();
      }
    }, [playbackState.isPlaying, onPause, onPlay]);

    // Handler for setting zoom
    const handleSetZoom = useCallback((newZoom: number | 'fit') => {
      if (newZoom === 'fit') {
        setZoom(1.0);
      } else {
        setZoom(newZoom);
      }
    }, []);

    // Context menu hook integration
    const handlePreviewContextMenu = usePreviewContextMenu(
      handleTogglePlayback,
      handleAddMarker,
      handleExportFrame,
      handleSetZoom
    );

    // Handle context menu on video container
    const handleVideoContextMenu = useCallback(
      (e: React.MouseEvent) => {
        handlePreviewContextMenu(
          e,
          playbackState.currentTime,
          playbackState.duration,
          playbackState.isPlaying,
          zoom
        );
      },
      [
        handlePreviewContextMenu,
        playbackState.currentTime,
        playbackState.duration,
        playbackState.isPlaying,
        zoom,
      ]
    );

    // Expose imperative methods via ref
    useImperativeHandle(
      ref,
      () => ({
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
          // Map continuous rate to discrete PlaybackSpeed values
          // Uses midpoints between speeds for better user experience
          // e.g., 0.25-0.375 -> 0.25, 0.375-0.75 -> 0.5, etc.
          let speed: PlaybackSpeed = 1.0;
          if (rate <= 0.375)
            speed = 0.25; // Below midpoint of 0.25 and 0.5
          else if (rate <= 0.75)
            speed = 0.5; // Below midpoint of 0.5 and 1.0
          else if (rate <= 1.5)
            speed = 1.0; // Below midpoint of 1.0 and 2.0
          else if (rate <= 3.0)
            speed = 2.0; // Below midpoint of 2.0 and 4.0
          else speed = 4.0; // Above 3.0

          playbackEngineRef.current?.setPlaybackSpeed(speed);
        },
        playAround: (secondsBefore = 2, secondsAfter = 2) => {
          playbackEngineRef.current?.playAround(secondsBefore, secondsAfter);
        },
        addMarker: () => {
          handleAddMarker(playbackState.currentTime);
        },
      }),
      [handleAddMarker, playbackState.currentTime]
    );

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
            const effectCanvas = applyEffectsToFrame(
              sourceCanvas,
              memoizedEffects,
              playbackState.currentTime
            );
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
        {/* eslint-disable-next-line jsx-a11y/no-static-element-interactions -- Context menu container for video preview */}
        <div className={styles.videoContainer} onContextMenu={handleVideoContextMenu}>
          {videoUrl ? (
            <div className={styles.videoWrapper} style={{ transform: `scale(${zoom})` }}>
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
            </div>
          ) : (
            <Text className={styles.placeholder}>No video loaded</Text>
          )}
        </div>

        {/* Zoom Controls */}
        <div className={styles.zoomBar}>
          <ZoomControls
            zoom={zoom}
            onZoomChange={setZoom}
            onZoomFit={() => setZoom(1.0)}
            disabled={!videoUrl}
          />
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
  }
);

// Export memoized component with ref forwarding
export const VideoPreviewPanel = memo(VideoPreviewPanelInner);
