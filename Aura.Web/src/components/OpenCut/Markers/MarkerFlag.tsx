/**
 * MarkerFlag Component
 *
 * Visual marker flag displayed on the timeline. Shows a colored flag icon
 * based on marker type with tooltip, drag support, and click handling.
 */

import { makeStyles, tokens, mergeClasses, Tooltip } from '@fluentui/react-components';
import {
  Flag24Filled,
  BookmarkMultiple24Filled,
  CheckmarkCircle24Filled,
  MusicNote124Filled,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useState, useCallback, useRef } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { openCutTokens } from '../../../styles/designTokens';
import type { Marker, MarkerType } from '../../../types/opencut';

export interface MarkerFlagProps {
  marker: Marker;
  position: number;
  isSelected: boolean;
  onClick: (markerId: string, event: ReactMouseEvent) => void;
  onDoubleClick: (markerId: string) => void;
  onDragStart: (markerId: string, startX: number) => void;
  onDrag: (markerId: string, deltaX: number) => void;
  onDragEnd: (markerId: string) => void;
}

const useStyles = makeStyles({
  container: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    cursor: 'pointer',
    zIndex: openCutTokens.zIndex.dropdown,
    transform: 'translateX(-50%)',
    ':hover': {
      zIndex: openCutTokens.zIndex.sticky,
    },
  },
  selected: {
    zIndex: openCutTokens.zIndex.sticky + 10,
  },
  flag: {
    width: '16px',
    height: '16px',
    borderRadius: '2px 4px 4px 2px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    boxShadow: openCutTokens.shadows.sm,
    transition: `transform ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      transform: 'scale(1.15)',
    },
  },
  flagSelected: {
    boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}, ${openCutTokens.shadows.md}`,
    transform: 'scale(1.1)',
  },
  flagIcon: {
    fontSize: '10px',
    color: 'white',
  },
  line: {
    width: '1px',
    flex: 1,
    opacity: 0.8,
  },
  lineSelected: {
    width: '2px',
    opacity: 1,
  },
  durationBar: {
    position: 'absolute',
    top: '16px',
    height: '4px',
    opacity: 0.5,
    borderRadius: openCutTokens.radius.xs,
  },
  completed: {
    opacity: 0.5,
  },
  completedIcon: {
    position: 'absolute',
    top: '-2px',
    right: '-2px',
    fontSize: '8px',
    color: tokens.colorPaletteGreenForeground1,
  },
});

const MARKER_ICON: Record<MarkerType, React.ReactNode> = {
  standard: <Flag24Filled />,
  chapter: <BookmarkMultiple24Filled />,
  todo: <CheckmarkCircle24Filled />,
  beat: <MusicNote124Filled />,
};

const COLOR_MAP: Record<string, string> = {
  red: '#EF4444',
  orange: '#F97316',
  yellow: '#EAB308',
  green: '#22C55E',
  blue: '#3B82F6',
  purple: '#A855F7',
  pink: '#EC4899',
};

export const MarkerFlag: FC<MarkerFlagProps> = ({
  marker,
  position,
  isSelected,
  onClick,
  onDoubleClick,
  onDragStart,
  onDrag,
  onDragEnd,
}) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const dragStartXRef = useRef(0);

  const markerColor = COLOR_MAP[marker.color] || COLOR_MAP.blue;

  const handleMouseDown = useCallback(
    (e: ReactMouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(true);
      dragStartXRef.current = e.clientX;
      onDragStart(marker.id, e.clientX);

      const handleMouseMove = (moveEvent: MouseEvent) => {
        const deltaX = moveEvent.clientX - dragStartXRef.current;
        onDrag(marker.id, deltaX);
      };

      const handleMouseUp = () => {
        setIsDragging(false);
        onDragEnd(marker.id);
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [marker.id, onDragStart, onDrag, onDragEnd]
  );

  const handleClick = useCallback(
    (e: ReactMouseEvent) => {
      if (!isDragging) {
        onClick(marker.id, e);
      }
    },
    [isDragging, marker.id, onClick]
  );

  const handleDoubleClick = useCallback(() => {
    onDoubleClick(marker.id);
  }, [marker.id, onDoubleClick]);

  const tooltipContent = (
    <div>
      <strong>{marker.name}</strong>
      {marker.notes && <div style={{ fontSize: '11px', opacity: 0.8 }}>{marker.notes}</div>}
      {marker.type === 'todo' && (
        <div style={{ fontSize: '11px', opacity: 0.8 }}>
          Status: {marker.completed ? 'Completed' : 'Pending'}
        </div>
      )}
    </div>
  );

  return (
    <Tooltip content={tooltipContent} relationship="label" positioning="above">
      <motion.div
        className={mergeClasses(
          styles.container,
          isSelected && styles.selected,
          marker.type === 'todo' && marker.completed && styles.completed
        )}
        style={{ left: position }}
        onMouseDown={handleMouseDown}
        onClick={handleClick}
        onDoubleClick={handleDoubleClick}
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.8 }}
        transition={{ duration: 0.15 }}
        role="button"
        tabIndex={0}
        aria-label={`${marker.type} marker: ${marker.name}`}
        aria-pressed={isSelected}
      >
        {/* Flag head */}
        <div
          className={mergeClasses(styles.flag, isSelected && styles.flagSelected)}
          style={{ backgroundColor: markerColor }}
        >
          <span className={styles.flagIcon}>{MARKER_ICON[marker.type]}</span>
        </div>

        {/* Vertical line */}
        <div
          className={mergeClasses(styles.line, isSelected && styles.lineSelected)}
          style={{ backgroundColor: markerColor }}
        />

        {/* Duration bar for range markers - uses fixed multiplier; parent component handles zoom */}
        {marker.duration && marker.duration > 0 && (
          <div
            className={styles.durationBar}
            style={{
              backgroundColor: markerColor,
              width: `${marker.duration * 100}px`,
            }}
          />
        )}
      </motion.div>
    </Tooltip>
  );
};

export default MarkerFlag;
