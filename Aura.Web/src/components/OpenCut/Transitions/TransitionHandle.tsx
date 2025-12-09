/**
 * TransitionHandle Component
 *
 * Timeline transition indicator displayed at clip boundaries.
 * Shows a bowtie/hourglass shape that can be clicked to select,
 * dragged to adjust duration, and displays transition info.
 */

import { makeStyles, tokens, Tooltip, mergeClasses } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { useState, useCallback, useRef, useEffect } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import {
  useOpenCutTransitionsStore,
  type ClipTransition,
} from '../../../stores/opencutTransitions';
import { openCutTokens } from '../../../styles/designTokens';

export interface TransitionHandleProps {
  /** The applied transition */
  transition: ClipTransition;
  /** Position in pixels from left of timeline */
  position: number;
  /** Pixels per second for duration calculation */
  pixelsPerSecond: number;
  /** Whether this transition is selected */
  isSelected?: boolean;
  /** Callback when transition is clicked */
  onClick?: (transitionId: string) => void;
  /** Callback when transition duration is changed by dragging */
  onDurationChange?: (transitionId: string, newDuration: number) => void;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: '0',
    bottom: '0',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    zIndex: openCutTokens.zIndex.sticky - 4,
  },
  handle: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    minWidth: '20px',
  },
  bowtie: {
    width: '100%',
    height: '32px',
    minWidth: '20px',
    maxHeight: '80%',
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'all 150ms ease-out',
  },
  bowtieSvg: {
    width: '100%',
    height: '100%',
    filter: 'drop-shadow(0 1px 2px rgba(0,0,0,0.3))',
  },
  bowtieSelected: {
    filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.4))',
  },
  durationLabel: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    fontSize: '9px',
    color: 'white',
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    fontWeight: 600,
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
    pointerEvents: 'none',
    whiteSpace: 'nowrap',
  },
  dragHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '6px',
    cursor: 'ew-resize',
    backgroundColor: 'transparent',
    ':hover': {
      backgroundColor: 'rgba(255,255,255,0.3)',
    },
  },
  dragHandleLeft: {
    left: 0,
    borderRadius: `${tokens.borderRadiusSmall} 0 0 ${tokens.borderRadiusSmall}`,
  },
  dragHandleRight: {
    right: 0,
    borderRadius: `0 ${tokens.borderRadiusSmall} ${tokens.borderRadiusSmall} 0`,
  },
});

// Get color based on transition category
function getTransitionColor(transitionId: string): string {
  if (transitionId.includes('dissolve') || transitionId.includes('fade')) {
    return '#8B5CF6'; // Purple for dissolves
  } else if (transitionId.includes('wipe')) {
    return '#F59E0B'; // Amber for wipes
  } else if (transitionId.includes('slide')) {
    return '#3B82F6'; // Blue for slides
  } else if (transitionId.includes('zoom')) {
    return '#22C55E'; // Green for zooms
  } else if (transitionId.includes('blur')) {
    return '#EC4899'; // Pink for blur
  }
  return '#6B7280'; // Gray default
}

export const TransitionHandle: FC<TransitionHandleProps> = ({
  transition,
  position,
  pixelsPerSecond,
  isSelected = false,
  onClick,
  onDurationChange,
}) => {
  const styles = useStyles();
  const transitionsStore = useOpenCutTransitionsStore();
  const [isDragging, setIsDragging] = useState(false);
  const [dragSide, setDragSide] = useState<'left' | 'right' | null>(null);
  const startXRef = useRef(0);
  const startDurationRef = useRef(0);

  const definition = transitionsStore.getTransitionDefinition(transition.transitionId);
  const color = getTransitionColor(transition.transitionId);
  const width = Math.max(transition.duration * pixelsPerSecond, 20);

  const handleClick = useCallback(
    (e: ReactMouseEvent) => {
      e.stopPropagation();
      onClick?.(transition.id);
    },
    [onClick, transition.id]
  );

  const handleDragStart = useCallback(
    (e: ReactMouseEvent, side: 'left' | 'right') => {
      e.stopPropagation();
      e.preventDefault();
      setIsDragging(true);
      setDragSide(side);
      startXRef.current = e.clientX;
      startDurationRef.current = transition.duration;
    },
    [transition.duration]
  );

  useEffect(() => {
    if (!isDragging || !dragSide) return;

    const handleMouseMove = (e: globalThis.MouseEvent) => {
      const deltaX = e.clientX - startXRef.current;
      const deltaDuration = deltaX / pixelsPerSecond;
      const multiplier = dragSide === 'right' ? 1 : -1;
      const newDuration = Math.max(
        0.1,
        Math.min(5, startDurationRef.current + deltaDuration * multiplier)
      );
      onDurationChange?.(transition.id, newDuration);
    };

    const handleMouseUp = () => {
      setIsDragging(false);
      setDragSide(null);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging, dragSide, pixelsPerSecond, onDurationChange, transition.id]);

  const formatDuration = (seconds: number): string => {
    if (seconds < 1) {
      return `${Math.round(seconds * 1000)}ms`;
    }
    return `${seconds.toFixed(1)}s`;
  };

  return (
    <Tooltip
      content={`${definition?.name || 'Transition'} - ${formatDuration(transition.duration)}`}
      relationship="label"
    >
      <motion.div
        className={styles.container}
        style={{
          left: position - width / 2,
          width,
        }}
        onClick={handleClick}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.8 }}
        transition={{ duration: 0.15 }}
      >
        <div className={styles.handle}>
          <div className={mergeClasses(styles.bowtie, isSelected && styles.bowtieSelected)}>
            <svg
              className={styles.bowtieSvg}
              viewBox="0 0 40 24"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
              preserveAspectRatio="none"
            >
              {/* Bowtie/hourglass shape */}
              <path d="M0 0 L20 12 L0 24 Z" fill={color} opacity={isSelected ? 1 : 0.85} />
              <path d="M40 0 L20 12 L40 24 Z" fill={color} opacity={isSelected ? 1 : 0.85} />
              {isSelected && (
                <path
                  d="M0 0 L20 12 L0 24 M40 0 L20 12 L40 24"
                  stroke="white"
                  strokeWidth="2"
                  fill="none"
                />
              )}
            </svg>
            <span className={styles.durationLabel}>{formatDuration(transition.duration)}</span>
          </div>

          {/* Drag handles for resizing */}
          <div
            className={mergeClasses(styles.dragHandle, styles.dragHandleLeft)}
            onMouseDown={(e) => handleDragStart(e, 'left')}
            role="slider"
            aria-label="Resize transition start"
            aria-valuemin={0.1}
            aria-valuemax={5}
            aria-valuenow={transition.duration}
            tabIndex={0}
          />
          <div
            className={mergeClasses(styles.dragHandle, styles.dragHandleRight)}
            onMouseDown={(e) => handleDragStart(e, 'right')}
            role="slider"
            aria-label="Resize transition end"
            aria-valuemin={0.1}
            aria-valuemax={5}
            aria-valuenow={transition.duration}
            tabIndex={0}
          />
        </div>
      </motion.div>
    </Tooltip>
  );
};

export default TransitionHandle;
