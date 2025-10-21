/**
 * Timeline track component with waveform display and scrubbing
 */

import { useEffect, useRef, useState, useCallback } from 'react';
import { makeStyles, tokens, Spinner } from '@fluentui/react-components';

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
  duration,
  zoom,
  onSeek,
  muted = false,
  selected = false,
}: TimelineTrackProps) {
  const styles = useStyles();
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const trackContentRef = useRef<HTMLDivElement>(null);
  const [waveformData, setWaveformData] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isScrubbing, setIsScrubbing] = useState(false);
  const [scrubPosition, setScrubPosition] = useState(0);
  const [scrubTime, setScrubTime] = useState(0);

  // Track color based on type
  const trackColor = type === 'narration' 
    ? 'rgba(68, 114, 196, 0.8)'  // Blue
    : type === 'music'
    ? 'rgba(112, 173, 71, 0.8)'  // Green
    : 'rgba(237, 125, 49, 0.8)';  // Orange

  // Load waveform data
  useEffect(() => {
    if (!audioPath) return;

    const loadWaveform = async () => {
      setIsLoading(true);
      try {
        // In a real implementation, this would call the backend API
        // For now, generate mock data
        const samples = Math.floor(duration * zoom);
        const mockData = Array.from({ length: samples }, () => Math.random() * 0.5 + 0.2);
        setWaveformData(mockData);
      } catch (error) {
        console.error('Failed to load waveform:', error);
      } finally {
        setIsLoading(false);
      }
    };

    loadWaveform();
  }, [audioPath, duration, zoom]);

  // Draw waveform
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || waveformData.length === 0) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const width = canvas.width;
    const height = canvas.height;

    // Clear canvas
    ctx.clearRect(0, 0, width, height);

    // Draw background
    ctx.fillStyle = muted ? 'rgba(0, 0, 0, 0.3)' : 'rgba(0, 0, 0, 0.1)';
    ctx.fillRect(0, 0, width, height);

    // Draw waveform
    const barWidth = Math.max(1, width / waveformData.length);
    const centerY = height / 2;

    ctx.fillStyle = muted ? 'rgba(128, 128, 128, 0.5)' : trackColor;

    for (let i = 0; i < waveformData.length; i++) {
      const x = i * barWidth;
      const amplitude = waveformData[i];
      const barHeight = amplitude * centerY;

      ctx.fillRect(x, centerY - barHeight, barWidth, barHeight * 2);
    }

    // Draw selection highlight if selected
    if (selected) {
      ctx.strokeStyle = tokens.colorBrandBackground;
      ctx.lineWidth = 2;
      ctx.strokeRect(0, 0, width, height);
    }
  }, [waveformData, muted, selected, trackColor]);

  // Handle scrubbing
  const handleMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    if (!trackContentRef.current || !onSeek) return;

    setIsScrubbing(true);
    const rect = trackContentRef.current.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const time = x / zoom;

    setScrubPosition(x);
    setScrubTime(time);
    onSeek(time);
  }, [zoom, onSeek]);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (!isScrubbing || !trackContentRef.current || !onSeek) return;

    const rect = trackContentRef.current.getBoundingClientRect();
    const x = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
    const time = x / zoom;

    setScrubPosition(x);
    setScrubTime(time);
    onSeek(time);
  }, [isScrubbing, zoom, onSeek]);

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
      <div
        ref={trackContentRef}
        className={styles.trackContent}
        onMouseDown={handleMouseDown}
      >
        <canvas
          ref={canvasRef}
          className={styles.canvas}
          width={duration * zoom}
          height={80}
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
