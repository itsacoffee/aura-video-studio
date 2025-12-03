/**
 * MarkerFlag Component
 *
 * Visual marker flag on the timeline. Displays a colored flag icon
 * that can be dragged to reposition, clicked to select, and shows
 * a tooltip with marker details on hover.
 */

import { makeStyles, tokens, Tooltip, mergeClasses } from '@fluentui/react-components';
import {
  Flag24Filled,
  BookmarkMultiple24Filled,
  CheckmarkCircle24Filled,
  MusicNote224Filled,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { useState, useCallback, useRef } from 'react';
import type { Marker, MarkerType } from '../../../types/opencut';
import { getMarkerColorHex } from './MarkerColorPicker';

export interface MarkerFlagProps {
  marker: Marker;
  isSelected: boolean;
  pixelsPerSecond: number;
  onSelect: (markerId: string) => void;
  onMove: (markerId: string, newTime: number) => void;
  onClick?: (marker: Marker) => void;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '20px',
    marginLeft: '-10px',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    cursor: 'pointer',
    zIndex: 10,
    ':hover': {
      zIndex: 15,
    },
  },
  containerSelected: {
    zIndex: 20,
  },
  flag: {
    width: '20px',
    height: '20px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: tokens.borderRadiusSmall,
    transition: 'transform 100ms ease-out, box-shadow 100ms ease-out',
    ':hover': {
      transform: 'scale(1.15)',
      boxShadow: tokens.shadow8,
    },
  },
  flagSelected: {
    transform: 'scale(1.15)',
    boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
  },
  stem: {
    width: '2px',
    flex: 1,
    opacity: 0.6,
    minHeight: '100%',
  },
  stemSelected: {
    opacity: 1,
  },
  icon: {
    fontSize: '14px',
    color: 'white',
  },
  todoCompleted: {
    textDecoration: 'line-through',
    opacity: 0.6,
  },
  rangeBar: {
    position: 'absolute',
    top: '22px',
    height: '4px',
    borderRadius: tokens.borderRadiusSmall,
    opacity: 0.5,
  },
});

const MARKER_TYPE_ICONS: Record<MarkerType, React.ReactNode> = {
  standard: <Flag24Filled />,
  chapter: <BookmarkMultiple24Filled />,
  todo: <CheckmarkCircle24Filled />,
  beat: <MusicNote224Filled />,
};

export const MarkerFlag: FC<MarkerFlagProps> = ({
  marker,
  isSelected,
  pixelsPerSecond,
  onSelect,
  onMove,
  onClick,
}) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const dragStartX = useRef(0);
  const dragStartTime = useRef(0);

  const markerColor = getMarkerColorHex(marker.color);
  const leftPosition = marker.time * pixelsPerSecond;

  const handleMouseDown = useCallback(
    (e: ReactMouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(true);
      dragStartX.current = e.clientX;
      dragStartTime.current = marker.time;
      onSelect(marker.id);
    },
    [marker.id, marker.time, onSelect]
  );

  const handleMouseMove = useCallback(
    (e: globalThis.MouseEvent) => {
      if (!isDragging) return;

      const deltaX = e.clientX - dragStartX.current;
      const deltaTime = deltaX / pixelsPerSecond;
      const newTime = Math.max(0, dragStartTime.current + deltaTime);
      onMove(marker.id, newTime);
    },
    [isDragging, pixelsPerSecond, marker.id, onMove]
  );

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  // Attach global mouse listeners when dragging
  useState(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };
    }
  });

  const handleClick = useCallback(
    (e: ReactMouseEvent) => {
      e.stopPropagation();
      onSelect(marker.id);
      onClick?.(marker);
    },
    [marker, onSelect, onClick]
  );

  const tooltipContent = (
    <div>
      <strong>{marker.name}</strong>
      {marker.notes && <p style={{ margin: 0, fontSize: '12px' }}>{marker.notes}</p>}
      <p style={{ margin: 0, fontSize: '11px', opacity: 0.7 }}>
        {formatTime(marker.time)} • {marker.type}
        {marker.type === 'todo' && marker.completed && ' (completed)'}
      </p>
    </div>
  );

  return (
    <Tooltip content={tooltipContent} relationship="description" positioning="above">
      <motion.div
        ref={containerRef}
        className={mergeClasses(styles.container, isSelected && styles.containerSelected)}
        style={{ left: leftPosition }}
        onMouseDown={handleMouseDown}
        onClick={handleClick}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.8 }}
        transition={{ duration: 0.15 }}
        role="button"
        tabIndex={0}
        aria-label={`Marker: ${marker.name} at ${formatTime(marker.time)}`}
        aria-pressed={isSelected}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            onSelect(marker.id);
            onClick?.(marker);
          }
        }}
      >
        <div
          className={mergeClasses(styles.flag, isSelected && styles.flagSelected)}
          style={{ backgroundColor: markerColor }}
        >
          <span className={styles.icon}>{MARKER_TYPE_ICONS[marker.type]}</span>
        </div>
        <div
          className={mergeClasses(styles.stem, isSelected && styles.stemSelected)}
          style={{ backgroundColor: markerColor }}
        />
        {marker.duration && marker.duration > 0 && (
          <div
            className={styles.rangeBar}
            style={{
              backgroundColor: markerColor,
              width: marker.duration * pixelsPerSecond,
            }}
          />
        )}
      </motion.div>
    </Tooltip>
  );
};

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

export default MarkerFlag;
