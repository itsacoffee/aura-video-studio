import { useEffect, useRef, useState } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Slider,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Speaker224Regular,
  SpeakerMute24Regular,
  Previous24Regular,
  Next24Regular,
  FullScreenMaximize24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
  },
  videoContainer: {
    position: 'relative',
    flex: 1,
    backgroundColor: '#000',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  video: {
    maxWidth: '100%',
    maxHeight: '100%',
    width: 'auto',
    height: 'auto',
  },
  placeholder: {
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  controls: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  controlsRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  seekBar: {
    flex: 1,
  },
  timecode: {
    minWidth: '100px',
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  volumeControl: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    width: '150px',
  },
  sceneMarker: {
    position: 'absolute',
    top: '0',
    bottom: '0',
    width: '2px',
    backgroundColor: tokens.colorBrandBackground,
    cursor: 'pointer',
    '&:hover': {
      width: '4px',
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
});

interface VideoPreviewPlayerProps {
  videoUrl?: string;
  currentTime?: number;
  onTimeUpdate?: (time: number) => void;
  onSeek?: (time: number) => void;
}

export function VideoPreviewPlayer({
  videoUrl,
  currentTime = 0,
  onTimeUpdate,
  onSeek,
}: VideoPreviewPlayerProps) {
  const styles = useStyles();
  const videoRef = useRef<HTMLVideoElement>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [volume, setVolume] = useState(1);
  const [isMuted, setIsMuted] = useState(false);
  const [duration, setDuration] = useState(0);
  const [playbackRate, setPlaybackRate] = useState(1);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    const handleTimeUpdate = () => {
      onTimeUpdate?.(video.currentTime);
    };

    const handleLoadedMetadata = () => {
      setDuration(video.duration);
    };

    const handleEnded = () => {
      setIsPlaying(false);
    };

    video.addEventListener('timeupdate', handleTimeUpdate);
    video.addEventListener('loadedmetadata', handleLoadedMetadata);
    video.addEventListener('ended', handleEnded);

    return () => {
      video.removeEventListener('timeupdate', handleTimeUpdate);
      video.removeEventListener('loadedmetadata', handleLoadedMetadata);
      video.removeEventListener('ended', handleEnded);
    };
  }, [onTimeUpdate]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const video = videoRef.current;
      if (!video || !videoUrl) return;

      switch (e.key) {
        case ' ':
          e.preventDefault();
          handlePlayPause();
          break;
        case 'ArrowLeft':
          e.preventDefault();
          video.currentTime = Math.max(0, video.currentTime - 1 / 30); // 1 frame at 30fps
          break;
        case 'ArrowRight':
          e.preventDefault();
          video.currentTime = Math.min(duration, video.currentTime + 1 / 30);
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [videoUrl, duration]);

  const handlePlayPause = () => {
    const video = videoRef.current;
    if (!video) return;

    if (isPlaying) {
      video.pause();
    } else {
      video.play();
    }
    setIsPlaying(!isPlaying);
  };

  const handleSeek = (value: number) => {
    const video = videoRef.current;
    if (!video) return;

    video.currentTime = value;
    onSeek?.(value);
  };

  const handleVolumeChange = (value: number) => {
    const video = videoRef.current;
    if (!video) return;

    setVolume(value);
    video.volume = value;
    if (value === 0) {
      setIsMuted(true);
    } else if (isMuted) {
      setIsMuted(false);
    }
  };

  const toggleMute = () => {
    const video = videoRef.current;
    if (!video) return;

    const newMuted = !isMuted;
    setIsMuted(newMuted);
    video.muted = newMuted;
  };

  const handlePlaybackRateChange = (rate: number) => {
    const video = videoRef.current;
    if (!video) return;

    setPlaybackRate(rate);
    video.playbackRate = rate;
  };

  const handleFullscreen = () => {
    const container = videoRef.current?.parentElement;
    if (!container) return;

    if (document.fullscreenElement) {
      document.exitFullscreen();
    } else {
      container.requestFullscreen();
    }
  };

  const formatTimecode = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    const frames = Math.floor((seconds % 1) * 30);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  };

  if (!videoUrl) {
    return (
      <div className={styles.container}>
        <div className={styles.videoContainer}>
          <div className={styles.placeholder}>
            <h3>Preview will appear after rendering</h3>
            <p>Click &quot;Generate Preview&quot; to create a preview video</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.videoContainer}>
        <video ref={videoRef} className={styles.video} src={videoUrl}>
          <track kind="captions" />
        </video>
      </div>

      <div className={styles.controls}>
        <div className={styles.controlsRow}>
          <Button
            appearance="subtle"
            icon={isPlaying ? <Pause24Regular /> : <Play24Regular />}
            onClick={handlePlayPause}
          />
          <Button
            appearance="subtle"
            icon={<Previous24Regular />}
            onClick={() => handleSeek(Math.max(0, currentTime - 1 / 30))}
          />
          <Button
            appearance="subtle"
            icon={<Next24Regular />}
            onClick={() => handleSeek(Math.min(duration, currentTime + 1 / 30))}
          />

          <div className={styles.seekBar}>
            <Slider
              value={currentTime}
              min={0}
              max={duration}
              step={0.01}
              onChange={(_, data) => handleSeek(data.value)}
            />
          </div>

          <div className={styles.timecode}>
            {formatTimecode(currentTime)} / {formatTimecode(duration)}
          </div>

          <div className={styles.volumeControl}>
            <Button
              appearance="subtle"
              icon={isMuted ? <SpeakerMute24Regular /> : <Speaker224Regular />}
              onClick={toggleMute}
            />
            <Slider
              value={isMuted ? 0 : volume}
              min={0}
              max={1}
              step={0.01}
              onChange={(_, data) => handleVolumeChange(data.value)}
            />
          </div>

          <Menu>
            <MenuTrigger>
              <Button appearance="subtle">{playbackRate}x</Button>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => handlePlaybackRateChange(0.25)}>0.25x</MenuItem>
                <MenuItem onClick={() => handlePlaybackRateChange(0.5)}>0.5x</MenuItem>
                <MenuItem onClick={() => handlePlaybackRateChange(1)}>1x</MenuItem>
                <MenuItem onClick={() => handlePlaybackRateChange(2)}>2x</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>

          <Button
            appearance="subtle"
            icon={<FullScreenMaximize24Regular />}
            onClick={handleFullscreen}
          />
        </div>
      </div>
    </div>
  );
}
