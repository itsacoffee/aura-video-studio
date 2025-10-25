import { useState, useRef, useEffect } from 'react';
import { makeStyles, tokens, Button, Slider, Text, Menu, MenuTrigger, MenuList, MenuItem, MenuPopover } from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Stop24Regular,
  Previous24Regular,
  Next24Regular,
  SpeakerMute24Regular,
  Speaker224Regular,
  ZoomIn24Regular,
  ZoomOut24Regular,
  ZoomFit24Regular,
} from '@fluentui/react-icons';
import { AppliedEffect } from '../../types/effects';
import { applyEffectsToFrame } from '../../utils/effectsEngine';

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
  controls: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  playbackControls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  timeDisplay: {
    minWidth: '100px',
    fontSize: tokens.fontSizeBase300,
    fontFamily: 'monospace',
  },
  seekBar: {
    flex: 1,
  },
  volumeControl: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    minWidth: '120px',
  },
  volumeSlider: {
    width: '80px',
  },
  zoomControls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
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

export function VideoPreviewPanel({
  videoUrl,
  currentTime = 0,
  effects = [],
  onTimeUpdate,
  onPlay,
  onPause,
  onStop,
}: VideoPreviewPanelProps) {
  const styles = useStyles();
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [duration, setDuration] = useState(0);
  const [localTime, setLocalTime] = useState(0);
  const [volume, setVolume] = useState(100);
  const [isMuted, setIsMuted] = useState(false);
  const [zoom, setZoom] = useState(100); // Zoom level percentage

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
      if (effects.length > 0) {
        const sourceCanvas = document.createElement('canvas');
        sourceCanvas.width = canvas.width;
        sourceCanvas.height = canvas.height;
        const sourceCtx = sourceCanvas.getContext('2d');
        if (sourceCtx) {
          sourceCtx.drawImage(canvas, 0, 0);
          const effectCanvas = applyEffectsToFrame(sourceCanvas, effects, localTime);
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(effectCanvas, 0, 0);
        }
      }

      if (isPlaying) {
        requestAnimationFrame(renderFrame);
      }
    };

    if (isPlaying || effects.length > 0) {
      renderFrame();
    }
  }, [isPlaying, effects, localTime]);

  // Sync external current time with video
  useEffect(() => {
    if (videoRef.current && currentTime !== localTime) {
      videoRef.current.currentTime = currentTime;
      setLocalTime(currentTime);
    }
  }, [currentTime, localTime]);

  const handlePlayPause = () => {
    if (!videoRef.current) return;

    if (isPlaying) {
      videoRef.current.pause();
      setIsPlaying(false);
      onPause?.();
    } else {
      videoRef.current.play();
      setIsPlaying(true);
      onPlay?.();
    }
  };

  const handleStop = () => {
    if (!videoRef.current) return;
    videoRef.current.pause();
    videoRef.current.currentTime = 0;
    setIsPlaying(false);
    setLocalTime(0);
    onStop?.();
    onTimeUpdate?.(0);
  };

  const handleTimeUpdate = () => {
    if (!videoRef.current) return;
    const time = videoRef.current.currentTime;
    setLocalTime(time);
    onTimeUpdate?.(time);
  };

  const handleLoadedMetadata = () => {
    if (!videoRef.current) return;
    setDuration(videoRef.current.duration);
  };

  const handleSeek = (_: unknown, data: { value: number }) => {
    if (!videoRef.current) return;
    const time = data.value;
    videoRef.current.currentTime = time;
    setLocalTime(time);
    onTimeUpdate?.(time);
  };

  const handleFrameStep = (forward: boolean) => {
    if (!videoRef.current) return;
    // Assuming 30fps, one frame is ~0.033 seconds
    const frameTime = 1 / 30;
    const newTime = forward ? localTime + frameTime : localTime - frameTime;
    const clampedTime = Math.max(0, Math.min(duration, newTime));
    videoRef.current.currentTime = clampedTime;
    setLocalTime(clampedTime);
    onTimeUpdate?.(clampedTime);
  };

  const handleVolumeChange = (_: unknown, data: { value: number }) => {
    if (!videoRef.current) return;
    const vol = data.value;
    setVolume(vol);
    videoRef.current.volume = vol / 100;
    if (vol === 0) {
      setIsMuted(true);
    } else if (isMuted) {
      setIsMuted(false);
    }
  };

  const toggleMute = () => {
    if (!videoRef.current) return;
    const newMuted = !isMuted;
    setIsMuted(newMuted);
    videoRef.current.muted = newMuted;
  };

  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const handleZoomIn = () => {
    setZoom((prev) => Math.min(200, prev + 25));
  };

  const handleZoomOut = () => {
    setZoom((prev) => Math.max(50, prev - 25));
  };

  const handleZoomFit = () => {
    setZoom(100);
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
              onTimeUpdate={handleTimeUpdate}
              onLoadedMetadata={handleLoadedMetadata}
              onEnded={() => setIsPlaying(false)}
              style={{ 
                transform: `scale(${zoom / 100})`,
                display: effects.length > 0 ? 'none' : 'block',
              }}
            >
              <track kind="captions" />
            </video>
            <canvas
              ref={canvasRef}
              className={styles.canvas}
              style={{ 
                transform: `scale(${zoom / 100})`,
                display: effects.length > 0 ? 'block' : 'none',
              }}
            />
          </>
        ) : (
          <Text className={styles.placeholder}>No video loaded</Text>
        )}
      </div>

      <div className={styles.controls}>
        <div className={styles.playbackControls}>
          <Button
            appearance="subtle"
            icon={<Previous24Regular />}
            onClick={() => handleFrameStep(false)}
            disabled={!videoUrl}
            aria-label="Previous frame"
          />
          {isPlaying ? (
            <Button
              appearance="primary"
              icon={<Pause24Regular />}
              onClick={handlePlayPause}
              disabled={!videoUrl}
              aria-label="Pause"
            />
          ) : (
            <Button
              appearance="primary"
              icon={<Play24Regular />}
              onClick={handlePlayPause}
              disabled={!videoUrl}
              aria-label="Play"
            />
          )}
          <Button
            appearance="subtle"
            icon={<Stop24Regular />}
            onClick={handleStop}
            disabled={!videoUrl}
            aria-label="Stop"
          />
          <Button
            appearance="subtle"
            icon={<Next24Regular />}
            onClick={() => handleFrameStep(true)}
            disabled={!videoUrl}
            aria-label="Next frame"
          />

          <Text className={styles.timeDisplay}>
            {formatTime(localTime)} / {formatTime(duration)}
          </Text>

          <div className={styles.seekBar}>
            <Slider
              min={0}
              max={duration || 100}
              value={localTime}
              onChange={handleSeek}
              disabled={!videoUrl}
            />
          </div>

          <div className={styles.volumeControl}>
            <Button
              appearance="subtle"
              icon={isMuted ? <SpeakerMute24Regular /> : <Speaker224Regular />}
              onClick={toggleMute}
              aria-label={isMuted ? 'Unmute' : 'Mute'}
            />
            <Slider
              className={styles.volumeSlider}
              min={0}
              max={100}
              value={isMuted ? 0 : volume}
              onChange={handleVolumeChange}
            />
          </div>

          <div className={styles.zoomControls}>
            <Menu>
              <MenuTrigger disableButtonEnhancement>
                <Button
                  appearance="subtle"
                  icon={<ZoomFit24Regular />}
                  aria-label="Zoom controls"
                >
                  {zoom}%
                </Button>
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  <MenuItem icon={<ZoomIn24Regular />} onClick={handleZoomIn}>
                    Zoom In (125%)
                  </MenuItem>
                  <MenuItem icon={<ZoomOut24Regular />} onClick={handleZoomOut}>
                    Zoom Out (75%)
                  </MenuItem>
                  <MenuItem icon={<ZoomFit24Regular />} onClick={handleZoomFit}>
                    Fit to Window (100%)
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>
          </div>
        </div>
      </div>
    </div>
  );
}
