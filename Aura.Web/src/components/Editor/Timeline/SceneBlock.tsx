/**
 * Scene block component with trim handles
 */

import { useState, useCallback, useRef } from 'react';
import { makeStyles, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
  sceneBlock: {
    position: 'absolute',
    height: '60px',
    backgroundColor: tokens.colorBrandBackground,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalS,
    cursor: 'grab',
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between',
    border: `2px solid ${tokens.colorNeutralStroke1}`,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
  sceneBlockSelected: {
    border: `3px solid ${tokens.colorBrandForeground1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  sceneBlockDragging: {
    opacity: 0.5,
    cursor: 'grabbing',
  },
  sceneContent: {
    color: tokens.colorNeutralForegroundOnBrand,
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    fontWeight: tokens.fontWeightSemibold,
  },
  sceneDuration: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForegroundOnBrand,
    opacity: 0.8,
  },
  trimHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '8px',
    cursor: 'ew-resize',
    backgroundColor: 'transparent',
    zIndex: 10,
    ':hover': {
      backgroundColor: tokens.colorBrandBackgroundHover,
    },
  },
  trimHandleLeft: {
    left: 0,
  },
  trimHandleRight: {
    right: 0,
  },
  trimHandleActive: {
    backgroundColor: tokens.colorPaletteYellowBackground3,
  },
  tooltip: {
    position: 'absolute',
    top: '-30px',
    left: '50%',
    transform: 'translateX(-50%)',
    padding: '4px 8px',
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: 'nowrap',
    pointerEvents: 'none',
    zIndex: 11,
  },
});

export interface SceneBlockProps {
  index: number;
  heading: string;
  start: number;
  duration: number;
  zoom: number; // pixels per second
  selected?: boolean;
  onSelect?: () => void;
  onTrim?: (newStart: number, newDuration: number) => void;
  onMove?: (newStart: number) => void;
}

export function SceneBlock({
  index,
  heading,
  start,
  duration,
  zoom,
  selected = false,
  onSelect,
  onTrim,
  onMove,
}: SceneBlockProps) {
  const styles = useStyles();
  const blockRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isTrimming, setIsTrimming] = useState<'left' | 'right' | null>(null);
  const [showTooltip, setShowTooltip] = useState(false);
  const [tooltipText, setTooltipText] = useState('');
  const [dragStartX, setDragStartX] = useState(0);
  const [originalStart, setOriginalStart] = useState(start);
  const [originalDuration, setOriginalDuration] = useState(duration);

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (e.button !== 0) return; // Only left click

      e.stopPropagation();
      onSelect?.();

      setIsDragging(true);
      setDragStartX(e.clientX);
      setOriginalStart(start);
    },
    [start, onSelect]
  );

  const handleTrimMouseDown = useCallback(
    (side: 'left' | 'right', e: React.MouseEvent) => {
      e.stopPropagation();
      onSelect?.();

      setIsTrimming(side);
      setDragStartX(e.clientX);
      setOriginalStart(start);
      setOriginalDuration(duration);
      setShowTooltip(true);
    },
    [start, duration, onSelect]
  );

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      if (isDragging) {
        // Handle scene movement
        const deltaX = e.clientX - dragStartX;
        const deltaTime = deltaX / zoom;
        const newStart = Math.max(0, originalStart + deltaTime);

        setTooltipText(formatDuration(newStart));
        setShowTooltip(true);

        // Would call onMove in a throttled manner in production
      } else if (isTrimming) {
        // Handle trim
        const deltaX = e.clientX - dragStartX;
        const deltaTime = deltaX / zoom;

        let newStart = originalStart;
        let newDuration = originalDuration;

        if (isTrimming === 'left') {
          // Trim from left (adjust in point)
          newStart = Math.max(0, originalStart + deltaTime);
          newDuration = originalDuration - (newStart - originalStart);
          newDuration = Math.max(0.1, newDuration); // Minimum duration
        } else {
          // Trim from right (adjust out point)
          newDuration = Math.max(0.1, originalDuration + deltaTime);
        }

        setTooltipText(
          `${formatDuration(newDuration)} (${deltaTime > 0 ? '+' : ''}${formatDuration(Math.abs(deltaTime))})`
        );
        setShowTooltip(true);

        // Would call onTrim in a throttled manner in production
      }
    },
    [isDragging, isTrimming, dragStartX, originalStart, originalDuration, zoom]
  );

  const handleMouseUp = useCallback(
    (e: MouseEvent) => {
      if (isDragging) {
        const deltaX = e.clientX - dragStartX;
        const deltaTime = deltaX / zoom;
        const newStart = Math.max(0, originalStart + deltaTime);
        onMove?.(newStart);
      } else if (isTrimming) {
        const deltaX = e.clientX - dragStartX;
        const deltaTime = deltaX / zoom;

        let newStart = originalStart;
        let newDuration = originalDuration;

        if (isTrimming === 'left') {
          newStart = Math.max(0, originalStart + deltaTime);
          newDuration = originalDuration - (newStart - originalStart);
          newDuration = Math.max(0.1, newDuration);
        } else {
          newDuration = Math.max(0.1, originalDuration + deltaTime);
        }

        onTrim?.(newStart, newDuration);
      }

      setIsDragging(false);
      setIsTrimming(null);
      setShowTooltip(false);
    },
    [isDragging, isTrimming, dragStartX, originalStart, originalDuration, zoom, onMove, onTrim]
  );

  // Add mouse event listeners
  useState(() => {
    if (isDragging || isTrimming) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  });

  const leftPos = start * zoom;
  const width = duration * zoom;

  return (
    <div
      ref={blockRef}
      className={`${styles.sceneBlock} ${selected ? styles.sceneBlockSelected : ''} ${
        isDragging ? styles.sceneBlockDragging : ''
      }`}
      style={{
        left: `${leftPos}px`,
        width: `${width}px`,
      }}
      onMouseDown={handleMouseDown}
    >
      <div className={styles.sceneContent}>
        {index + 1}. {heading}
      </div>
      <div className={styles.sceneDuration}>{formatDuration(duration)}</div>

      {/* Trim handles */}
      <div
        className={`${styles.trimHandle} ${styles.trimHandleLeft} ${
          isTrimming === 'left' ? styles.trimHandleActive : ''
        }`}
        onMouseDown={(e) => handleTrimMouseDown('left', e)}
      />
      <div
        className={`${styles.trimHandle} ${styles.trimHandleRight} ${
          isTrimming === 'right' ? styles.trimHandleActive : ''
        }`}
        onMouseDown={(e) => handleTrimMouseDown('right', e)}
      />

      {/* Tooltip */}
      {showTooltip && <div className={styles.tooltip}>{tooltipText}</div>}
    </div>
  );
}
