/**
 * Timeline track component with waveform display and scrubbing
 */

import { makeStyles, tokens, Spinner } from '@fluentui/react-components';
import { useEffect, useRef, useState, useCallback } from 'react';
import WaveSurfer from 'wavesurfer.js';

const useStyles = makeStyles({
  track: {
    position: 'relative',
    height: '80px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
  },
  trackLabel: {
    width: '120px',
    padding: tokens.spacingHorizontalM,
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'center',
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground3,
    fontWeight: tokens.fontWeightSemibold,
  },
  trackContent: {
    flex: 1,
    position: 'relative',
    cursor: 'grab',
    ':active': {
      cursor: 'grabbing',
    },
  },
  waveformContainer: {
    width: '100%',
    height: '100%',
    position: 'relative',
    border: `2px solid transparent`,
    transition: 'border-color 0.2s',
  },
  waveformContainerSelected: {
    border: `2px solid ${tokens.colorBrandBackground}`,
  },
  canvas: {
    width: '100%',
    height: '100%',
    display: 'block',
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
  },
  scrubLine: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteYellowBackground3,
    pointerEvents: 'none',
    zIndex: 5,
  },
  timeTooltip: {
    position: 'absolute',
    top: '-25px',
    transform: 'translateX(-50%)',
    padding: '4px 8px',
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: 'nowrap',
    pointerEvents: 'none',
    zIndex: 6,
  },
});

export interface TimelineTrackProps {
  name: string;
  type: 'narration' | 'music' | 'sfx';
  audioPath?: string;
  duration: number;
  zoom: number; // pixels per second
  onSeek?: (time: number) => void;
  muted?: boolean;
  selected?: boolean;
}

export function TimelineTrack({
  name,
  type,
  audioPath,
  duration: _duration, // Not used - WaveSurfer.js automatically determines duration from audio file
  zoom,
  onSeek,
  muted = false,
  selected = false,
}: TimelineTrackProps) {
  const styles = useStyles();
  const waveformRef = useRef<HTMLDivElement>(null);
  const waveSurferRef = useRef<WaveSurfer | null>(null);
  const trackContentRef = useRef<HTMLDivElement>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isScrubbing, setIsScrubbing] = useState(false);
  const [scrubPosition, setScrubPosition] = useState(0);
  const [scrubTime, setScrubTime] = useState(0);

  // Track color based on type
  const trackColor =
    type === 'narration'
      ? 'rgba(68, 114, 196, 0.8)' // Blue
      : type === 'music'
        ? 'rgba(112, 173, 71, 0.8)' // Green
        : 'rgba(237, 125, 49, 0.8)'; // Orange

  // Initialize WaveSurfer.js
  useEffect(() => {
    if (!waveformRef.current || !audioPath) return;

    setIsLoading(true);

    const waveSurfer = WaveSurfer.create({
      container: waveformRef.current,
      height: 80,
      waveColor: trackColor,
      progressColor: muted ? 'rgba(128, 128, 128, 0.5)' : trackColor,
      cursorColor: 'transparent',
      barWidth: 2,
      barGap: 1,
      barRadius: 2,
      normalize: true,
      interact: false, // We handle interactions manually for better control
    });

    waveSurferRef.current = waveSurfer;

    // Load audio file
    waveSurfer
      .load(audioPath)
      .then(() => {
        setIsLoading(false);
      })
      .catch((error) => {
        console.error('Failed to load waveform:', error);
        setIsLoading(false);
      });

    // Cleanup
    return () => {
      waveSurfer?.destroy();
    };
  }, [audioPath, trackColor, muted]);

  // Update waveform color when muted state changes
  useEffect(() => {
    if (!waveSurferRef.current) return;

    const color = muted ? 'rgba(128, 128, 128, 0.5)' : trackColor;
    waveSurferRef.current.setOptions({
      waveColor: color,
      progressColor: color,
    });
  }, [muted, trackColor]);

  // Handle scrubbing
  const handleMouseDown = useCallback(
    (e: React.MouseEvent<HTMLDivElement>) => {
      if (!trackContentRef.current || !onSeek) return;

      setIsScrubbing(true);
      const rect = trackContentRef.current.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = x / zoom;

      setScrubPosition(x);
      setScrubTime(time);
      onSeek(time);
    },
    [zoom, onSeek]
  );

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      if (!isScrubbing || !trackContentRef.current || !onSeek) return;

      const rect = trackContentRef.current.getBoundingClientRect();
      const x = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
      const time = x / zoom;

      setScrubPosition(x);
      setScrubTime(time);
      onSeek(time);
    },
    [isScrubbing, zoom, onSeek]
  );

  const handleMouseUp = useCallback(() => {
    setIsScrubbing(false);
  }, []);

  useEffect(() => {
    if (isScrubbing) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isScrubbing, handleMouseMove, handleMouseUp]);

  // Format time for tooltip
  const formatTime = (time: number): string => {
    const minutes = Math.floor(time / 60);
    const seconds = Math.floor(time % 60);
    const frames = Math.floor((time % 1) * 30); // Assuming 30fps
    return `${minutes}:${seconds.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  };

  return (
    <div className={styles.track}>
      <div className={styles.trackLabel}>
        <div>{name}</div>
        <div style={{ fontSize: tokens.fontSizeBase100, color: tokens.colorNeutralForeground3 }}>
          {type}
        </div>
      </div>
      <div ref={trackContentRef} className={styles.trackContent} onMouseDown={handleMouseDown}>
        <div
          ref={waveformRef}
          className={`${styles.waveformContainer} ${selected ? styles.waveformContainerSelected : ''}`}
        />
        {isLoading && (
          <div className={styles.loadingOverlay}>
            <Spinner size="small" label="Loading waveform..." />
          </div>
        )}
        {isScrubbing && (
          <>
            <div className={styles.scrubLine} style={{ left: `${scrubPosition}px` }} />
            <div className={styles.timeTooltip} style={{ left: `${scrubPosition}px` }}>
              {formatTime(scrubTime)}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
