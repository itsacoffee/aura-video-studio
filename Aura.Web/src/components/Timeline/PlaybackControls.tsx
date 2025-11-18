/**
 * Enhanced Playback Controls Component
 *
 * Professional NLE-style playback controls with J-K-L shuttle,
 * frame-by-frame navigation, and playback speed controls.
 * Industry-standard controls found in Premiere Pro and CapCut.
 */

import { makeStyles } from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Previous24Regular,
  Next24Regular,
  ArrowStepBackRegular,
  ArrowStepInRegular,
} from '@fluentui/react-icons';
import React, { useCallback, useEffect, useState } from 'react';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-sm)',
    padding: 'var(--editor-space-md)',
    backgroundColor: 'var(--editor-panel-header-bg)',
    borderBottom: `1px solid var(--editor-panel-border)`,
  },
  controlGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-xs)',
  },
  button: {
    width: '36px',
    height: '36px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    color: 'var(--editor-text-primary)',
    cursor: 'pointer',
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      color: 'var(--editor-accent)',
      transform: 'translateY(-1px)',
      boxShadow: 'var(--editor-shadow-sm)',
    },
    '&:active': {
      transform: 'scale(0.98)',
    },
    '&:disabled': {
      opacity: 0.4,
      cursor: 'not-allowed',
      transform: 'none',
    },
  },
  playButton: {
    width: '44px',
    height: '44px',
    backgroundColor: 'var(--editor-accent)',
    color: 'white',
    '&:hover': {
      backgroundColor: 'var(--editor-accent-hover)',
      color: 'white',
    },
  },
  speedSelector: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-xs)',
    padding: '0 var(--editor-space-sm)',
    height: '36px',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    cursor: 'pointer',
    transition: 'all var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      borderColor: 'var(--editor-accent)',
    },
  },
  speedLabel: {
    fontSize: 'var(--editor-font-size-sm)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
    minWidth: '48px',
    textAlign: 'center',
  },
  timecode: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--editor-space-xs)',
    padding: '0 var(--editor-space-md)',
    height: '36px',
    backgroundColor: 'var(--editor-bg-elevated)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    fontFamily: 'monospace',
    fontSize: 'var(--editor-font-size-base)',
    fontWeight: 'var(--editor-font-weight-semibold)',
    color: 'var(--editor-text-primary)',
  },
  separator: {
    width: '1px',
    height: '24px',
    backgroundColor: 'var(--editor-panel-border)',
    margin: '0 var(--editor-space-xs)',
  },
  shortcutHint: {
    fontSize: 'var(--editor-font-size-xs)',
    color: 'var(--editor-text-tertiary)',
    marginLeft: 'var(--editor-space-xs)',
  },
  speedMenu: {
    position: 'absolute',
    bottom: '100%',
    marginBottom: 'var(--editor-space-sm)',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    boxShadow: 'var(--editor-shadow-lg)',
    padding: 'var(--editor-space-xs)',
    display: 'flex',
    flexDirection: 'column',
    gap: 'var(--editor-space-xs)',
    minWidth: '100px',
    zIndex: 'var(--editor-z-dropdown)',
  },
  speedMenuItem: {
    width: '100%',
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    borderRadius: 'var(--editor-radius-sm)',
    fontSize: 'var(--editor-font-size-sm)',
    color: 'var(--editor-text-primary)',
    backgroundColor: 'transparent',
    border: 'none',
    cursor: 'pointer',
    transition: 'all var(--editor-transition-fast)',
    textAlign: 'center',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      transform: 'translateX(2px)',
    },
  },
  speedMenuItemActive: {
    backgroundColor: 'var(--editor-accent)',
    color: 'white',
    '&:hover': {
      backgroundColor: 'var(--editor-accent-hover)',
      color: 'white',
    },
  },
});

export interface PlaybackControlsProps {
  isPlaying: boolean;
  currentTime: number;
  duration: number;
  playbackSpeed: number;
  frameRate?: number;
  onPlayPause: () => void;
  onSeek: (time: number) => void;
  onSpeedChange: (speed: number) => void;
  onPreviousFrame?: () => void;
  onNextFrame?: () => void;
  onJumpToStart?: () => void;
  onJumpToEnd?: () => void;
}

const SPEED_OPTIONS = [0.25, 0.5, 1, 1.5, 2, 4];

export const PlaybackControls: React.FC<PlaybackControlsProps> = ({
  isPlaying,
  currentTime,
  duration,
  playbackSpeed,
  frameRate = 30,
  onPlayPause,
  onSeek,
  onSpeedChange,
  onPreviousFrame,
  onNextFrame,
  onJumpToStart,
  onJumpToEnd,
}) => {
  const styles = useStyles();
  const [showSpeedMenu, setShowSpeedMenu] = useState(false);
  const [shuttleSpeed, setShuttleSpeed] = useState(0);

  const formatTimecode = useCallback(
    (time: number): string => {
      const totalSeconds = Math.floor(time);
      const minutes = Math.floor(totalSeconds / 60);
      const seconds = totalSeconds % 60;
      const frames = Math.floor((time - totalSeconds) * frameRate);
      return `${minutes.toString().padStart(2, '0')}:${seconds
        .toString()
        .padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
    },
    [frameRate]
  );

  const handlePreviousFrame = useCallback(() => {
    if (onPreviousFrame) {
      onPreviousFrame();
    } else {
      const frameDuration = 1 / frameRate;
      onSeek(Math.max(0, currentTime - frameDuration));
    }
  }, [currentTime, frameRate, onPreviousFrame, onSeek]);

  const handleNextFrame = useCallback(() => {
    if (onNextFrame) {
      onNextFrame();
    } else {
      const frameDuration = 1 / frameRate;
      onSeek(Math.min(duration, currentTime + frameDuration));
    }
  }, [currentTime, duration, frameRate, onNextFrame, onSeek]);

  const handleJumpToStart = useCallback(() => {
    if (onJumpToStart) {
      onJumpToStart();
    } else {
      onSeek(0);
    }
  }, [onJumpToStart, onSeek]);

  const handleJumpToEnd = useCallback(() => {
    if (onJumpToEnd) {
      onJumpToEnd();
    } else {
      onSeek(duration);
    }
  }, [duration, onJumpToEnd, onSeek]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      switch (event.key.toLowerCase()) {
        case 'j':
          event.preventDefault();
          setShuttleSpeed((prev) => {
            const newSpeed = Math.max(prev - 1, -4);
            if (newSpeed < 0 && !isPlaying) {
              onPlayPause();
            }
            return newSpeed;
          });
          break;
        case 'k':
          event.preventDefault();
          if (isPlaying) {
            onPlayPause();
          }
          setShuttleSpeed(0);
          break;
        case 'l':
          event.preventDefault();
          setShuttleSpeed((prev) => {
            const newSpeed = Math.min(prev + 1, 4);
            if (newSpeed > 0 && !isPlaying) {
              onPlayPause();
            }
            return newSpeed;
          });
          break;
        case ' ':
          event.preventDefault();
          onPlayPause();
          setShuttleSpeed(0);
          break;
        case ',':
          event.preventDefault();
          handlePreviousFrame();
          break;
        case '.':
          event.preventDefault();
          handleNextFrame();
          break;
        case 'home':
          event.preventDefault();
          handleJumpToStart();
          break;
        case 'end':
          event.preventDefault();
          handleJumpToEnd();
          break;
        default:
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [
    isPlaying,
    onPlayPause,
    handlePreviousFrame,
    handleNextFrame,
    handleJumpToStart,
    handleJumpToEnd,
  ]);

  useEffect(() => {
    if (shuttleSpeed === 0) {
      onSpeedChange(1);
    } else {
      const speed = Math.abs(shuttleSpeed) * 0.5;
      onSpeedChange(speed);
    }
  }, [shuttleSpeed, onSpeedChange]);

  return (
    <div className={styles.container}>
      <div className={styles.controlGroup}>
        <button
          className={styles.button}
          onClick={handleJumpToStart}
          aria-label="Jump to start (Home)"
          title="Jump to start (Home)"
        >
          <Previous24Regular />
        </button>

        <button
          className={styles.button}
          onClick={handlePreviousFrame}
          aria-label="Previous frame (,)"
          title="Previous frame (,)"
        >
          <ArrowStepBackRegular />
        </button>

        <button
          className={`${styles.button} ${styles.playButton}`}
          onClick={onPlayPause}
          aria-label={isPlaying ? 'Pause (Space/K)' : 'Play (Space/K)'}
          title={isPlaying ? 'Pause (Space/K)' : 'Play (Space/K)'}
        >
          {isPlaying ? <Pause24Regular /> : <Play24Regular />}
        </button>

        <button
          className={styles.button}
          onClick={handleNextFrame}
          aria-label="Next frame (.)"
          title="Next frame (.)"
        >
          <ArrowStepInRegular />
        </button>

        <button
          className={styles.button}
          onClick={handleJumpToEnd}
          aria-label="Jump to end (End)"
          title="Jump to end (End)"
        >
          <Next24Regular />
        </button>
      </div>

      <div className={styles.separator} />

      <div className={styles.timecode}>
        {formatTimecode(currentTime)} / {formatTimecode(duration)}
      </div>

      <div className={styles.separator} />

      <div style={{ position: 'relative' }}>
        <div
          className={styles.speedSelector}
          onClick={() => setShowSpeedMenu(!showSpeedMenu)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              setShowSpeedMenu(!showSpeedMenu);
            }
          }}
          role="button"
          tabIndex={0}
          aria-label="Playback speed"
          aria-expanded={showSpeedMenu}
          title="Click to change playback speed"
        >
          <span className={styles.speedLabel}>{playbackSpeed}x</span>
        </div>

        {showSpeedMenu && (
          <div className={styles.speedMenu}>
            {SPEED_OPTIONS.map((speed) => (
              <button
                key={speed}
                type="button"
                className={`${styles.speedMenuItem} ${
                  speed === playbackSpeed ? styles.speedMenuItemActive : ''
                }`}
                onClick={() => {
                  onSpeedChange(speed);
                  setShowSpeedMenu(false);
                }}
              >
                {speed}x
              </button>
            ))}
          </div>
        )}
      </div>

      <span className={styles.shortcutHint}>
        J-K-L: Shuttle • Space: Play/Pause • , .: Frame Step
      </span>
    </div>
  );
};
