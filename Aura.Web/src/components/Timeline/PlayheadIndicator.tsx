import { makeStyles, tokens } from '@fluentui/react-components';
import { useRef, useCallback } from 'react';
import { snapToFrame } from '../../services/timelineEngine';

const useStyles = makeStyles({
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    zIndex: 20,
    pointerEvents: 'none',
  },
  playheadLine: {
    width: '100%',
    height: '100%',
    backgroundColor: tokens.colorPaletteRedBackground3,
  },
  playheadHandle: {
    position: 'absolute',
    top: '-8px',
    left: '-8px',
    width: '18px',
    height: '18px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    borderRadius: '50%',
    border: `2px solid ${tokens.colorNeutralBackground1}`,
    cursor: 'ew-resize',
    pointerEvents: 'auto',
    boxShadow: tokens.shadow4,
    '&:hover': {
      transform: 'scale(1.2)',
    },
  },
  timeTooltip: {
    position: 'absolute',
    top: '-30px',
    left: '50%',
    transform: 'translateX(-50%)',
    backgroundColor: tokens.colorNeutralBackground1,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    whiteSpace: 'nowrap',
    pointerEvents: 'none',
    boxShadow: tokens.shadow8,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
  },
});

interface PlayheadIndicatorProps {
  currentTime: number;
  pixelsPerSecond: number;
  maxTime: number;
  onTimeChange: (time: number) => void;
  showTooltip?: boolean;
  timeTooltip?: string;
  snapping?: boolean;
  frameRate?: number;
  containerRef?: React.RefObject<HTMLElement>;
}

export function PlayheadIndicator({
  currentTime,
  pixelsPerSecond,
  maxTime,
  onTimeChange,
  showTooltip = false,
  timeTooltip,
  snapping = true,
  frameRate = 30,
  containerRef,
}: PlayheadIndicatorProps) {
  const styles = useStyles();
  const isDraggingRef = useRef(false);

  const handlePlayheadDrag = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      isDraggingRef.current = true;

      const handleMouseMove = (moveEvent: MouseEvent) => {
        if (!isDraggingRef.current) return;

        const container = containerRef?.current;
        if (!container) return;

        const rect = container.getBoundingClientRect();
        const x = moveEvent.clientX - rect.left - 100; // Account for track label width
        let time = Math.max(0, Math.min(maxTime, x / pixelsPerSecond));

        if (snapping) {
          time = snapToFrame(time, frameRate);
        }

        onTimeChange(time);
      };

      const handleMouseUp = () => {
        isDraggingRef.current = false;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [pixelsPerSecond, maxTime, snapping, frameRate, onTimeChange, containerRef]
  );

  const leftPosition = currentTime * pixelsPerSecond + 100; // Account for track label width

  return (
    <div className={styles.playhead} style={{ left: `${leftPosition}px` }}>
      <div className={styles.playheadLine} />
      <div
        className={styles.playheadHandle}
        onMouseDown={handlePlayheadDrag}
        role="slider"
        aria-label="Playhead"
        aria-valuenow={currentTime}
        aria-valuemin={0}
        aria-valuemax={maxTime}
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'ArrowLeft') {
            e.preventDefault();
            const newTime = Math.max(0, currentTime - 1 / frameRate);
            onTimeChange(snapping ? snapToFrame(newTime, frameRate) : newTime);
          } else if (e.key === 'ArrowRight') {
            e.preventDefault();
            const newTime = Math.min(maxTime, currentTime + 1 / frameRate);
            onTimeChange(snapping ? snapToFrame(newTime, frameRate) : newTime);
          }
        }}
      />
      {showTooltip && timeTooltip && <div className={styles.timeTooltip}>{timeTooltip}</div>}
    </div>
  );
}
