/**
 * Transport Bar Component
 *
 * Provides timeline scrubbing with:
 * - Frame preview during scrub
 * - In/Out point markers
 * - Precise time display
 * - Optimized scrubbing performance
 */

import { makeStyles, tokens, Text, Button, Tooltip } from '@fluentui/react-components';
import { Flag24Regular, FlagOff24Regular } from '@fluentui/react-icons';
import { useState, useRef, useCallback, memo, useEffect } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  timeline: {
    position: 'relative',
    height: '40px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    userSelect: 'none',
  },
  timelineTrack: {
    position: 'absolute',
    top: '50%',
    left: '8px',
    right: '8px',
    height: '4px',
    backgroundColor: tokens.colorNeutralStroke2,
    borderRadius: '2px',
    transform: 'translateY(-50%)',
  },
  timelineProgress: {
    position: 'absolute',
    top: 0,
    left: 0,
    height: '100%',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: '2px',
    transition: 'width 0.05s linear',
  },
  playhead: {
    position: 'absolute',
    top: '50%',
    width: '12px',
    height: '12px',
    backgroundColor: tokens.colorBrandBackground,
    border: `2px solid ${tokens.colorNeutralBackground1}`,
    borderRadius: '50%',
    transform: 'translate(-50%, -50%)',
    cursor: 'grab',
    transition: 'transform 0.1s ease',
    ':hover': {
      transform: 'translate(-50%, -50%) scale(1.2)',
    },
    ':active': {
      cursor: 'grabbing',
      transform: 'translate(-50%, -50%) scale(1.3)',
    },
  },
  marker: {
    position: 'absolute',
    top: 0,
    width: '2px',
    height: '100%',
    backgroundColor: tokens.colorPaletteYellowBackground3,
    cursor: 'pointer',
    ':hover': {
      width: '4px',
    },
  },
  inMarker: {
    backgroundColor: tokens.colorPaletteGreenBackground3,
  },
  outMarker: {
    backgroundColor: tokens.colorPaletteRedBackground3,
  },
  markerLabel: {
    position: 'absolute',
    top: '-20px',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    whiteSpace: 'nowrap',
    transform: 'translateX(-50%)',
  },
  controls: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  timeDisplay: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
  },
  markerControls: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  loopRegion: {
    position: 'absolute',
    top: '50%',
    height: '8px',
    backgroundColor: 'rgba(0, 120, 212, 0.2)',
    borderTop: `1px solid ${tokens.colorBrandBackground}`,
    borderBottom: `1px solid ${tokens.colorBrandBackground}`,
    transform: 'translateY(-50%)',
    pointerEvents: 'none',
  },
});

interface TransportBarProps {
  currentTime: number;
  duration: number;
  inPoint: number | null;
  outPoint: number | null;
  onSeek: (time: number) => void;
  onSetInPoint: () => void;
  onSetOutPoint: () => void;
  onClearInOutPoints: () => void;
  disabled?: boolean;
}

export const TransportBar = memo(function TransportBar({
  currentTime,
  duration,
  inPoint,
  outPoint,
  onSeek,
  onSetInPoint,
  onSetOutPoint,
  onClearInOutPoints,
  disabled = false,
}: TransportBarProps) {
  const styles = useStyles();
  const timelineRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [hoverTime, setHoverTime] = useState<number | null>(null);

  const formatTime = useCallback((seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    const frames = Math.floor((seconds % 1) * 30); // Assuming 30fps
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  }, []);

  const getTimeFromPosition = useCallback(
    (clientX: number): number => {
      if (!timelineRef.current) return 0;

      const rect = timelineRef.current.getBoundingClientRect();
      const padding = 8;
      const x = clientX - rect.left - padding;
      const width = rect.width - padding * 2;
      const progress = Math.max(0, Math.min(1, x / width));

      return progress * duration;
    },
    [duration]
  );

  const handleMouseDown = useCallback(
    (event: React.MouseEvent) => {
      if (disabled) return;

      event.preventDefault();
      setIsDragging(true);

      const time = getTimeFromPosition(event.clientX);
      onSeek(time);
    },
    [disabled, getTimeFromPosition, onSeek]
  );

  const handleMouseMove = useCallback(
    (event: MouseEvent) => {
      if (isDragging && !disabled) {
        const time = getTimeFromPosition(event.clientX);
        onSeek(time);
      }

      // Show hover time
      if (timelineRef.current && !disabled) {
        const time = getTimeFromPosition(event.clientX);
        setHoverTime(time);
      }
    },
    [isDragging, disabled, getTimeFromPosition, onSeek]
  );

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  const handleMouseLeave = useCallback(() => {
    setHoverTime(null);
  }, []);

  // Set up global mouse event listeners for dragging
  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);

      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, handleMouseMove, handleMouseUp]);

  const progressPercent = duration > 0 ? (currentTime / duration) * 100 : 0;
  const inPointPercent = inPoint !== null && duration > 0 ? (inPoint / duration) * 100 : null;
  const outPointPercent = outPoint !== null && duration > 0 ? (outPoint / duration) * 100 : null;

  const hasInOutPoints = inPoint !== null && outPoint !== null;
  const loopRegionLeft = inPointPercent !== null ? inPointPercent : 0;
  const loopRegionWidth =
    hasInOutPoints && inPointPercent !== null && outPointPercent !== null
      ? outPointPercent - inPointPercent
      : 0;

  return (
    <div className={styles.container}>
      <div
        ref={timelineRef}
        className={styles.timeline}
        onMouseDown={handleMouseDown}
        onMouseLeave={handleMouseLeave}
      >
        <div className={styles.timelineTrack}>
          <div className={styles.timelineProgress} style={{ width: `${progressPercent}%` }} />
        </div>

        {/* Loop region highlight */}
        {hasInOutPoints && (
          <div
            className={styles.loopRegion}
            style={{
              left: `calc(8px + ${loopRegionLeft}% * (100% - 16px) / 100)`,
              width: `calc(${loopRegionWidth}% * (100% - 16px) / 100)`,
            }}
          />
        )}

        {/* In point marker */}
        {inPointPercent !== null && (
          <div
            className={`${styles.marker} ${styles.inMarker}`}
            style={{ left: `calc(8px + ${inPointPercent}% * (100% - 16px) / 100)` }}
          >
            <div className={styles.markerLabel}>In</div>
          </div>
        )}

        {/* Out point marker */}
        {outPointPercent !== null && (
          <div
            className={`${styles.marker} ${styles.outMarker}`}
            style={{ left: `calc(8px + ${outPointPercent}% * (100% - 16px) / 100)` }}
          >
            <div className={styles.markerLabel}>Out</div>
          </div>
        )}

        {/* Playhead */}
        <div
          className={styles.playhead}
          style={{ left: `calc(8px + ${progressPercent}% * (100% - 16px) / 100)` }}
        />
      </div>

      <div className={styles.controls}>
        <div className={styles.timeDisplay}>
          <Text weight="semibold">{formatTime(currentTime)}</Text>
          <Text>/</Text>
          <Text>{formatTime(duration)}</Text>
          {hoverTime !== null && (
            <>
              <Text>•</Text>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                {formatTime(hoverTime)}
              </Text>
            </>
          )}
        </div>

        <div className={styles.markerControls}>
          <Tooltip content="Set In Point (I)" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              onClick={onSetInPoint}
              disabled={disabled}
              icon={<Flag24Regular />}
            >
              In
            </Button>
          </Tooltip>

          <Tooltip content="Set Out Point (O)" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              onClick={onSetOutPoint}
              disabled={disabled}
              icon={<Flag24Regular />}
            >
              Out
            </Button>
          </Tooltip>

          {hasInOutPoints && (
            <Tooltip content="Clear In/Out Points (Ctrl+Shift+X)" relationship="label">
              <Button
                appearance="subtle"
                size="small"
                onClick={onClearInOutPoints}
                disabled={disabled}
                icon={<FlagOff24Regular />}
              >
                Clear
              </Button>
            </Tooltip>
          )}
        </div>
      </div>
    </div>
  );
});
