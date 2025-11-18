/**
 * Timeline Mini-Map Component
 *
 * Provides bird's-eye view navigation of the entire timeline, similar to
 * Adobe Premiere Pro and CapCut. Shows overview of all clips and allows
 * click-to-jump navigation.
 */

import { makeStyles, type GriffelStyle } from '@fluentui/react-components';
import React, { useRef, useCallback, useEffect, useState, useMemo } from 'react';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  container: {
    width: '100%',
    height: '48px',
    backgroundColor: 'var(--editor-bg-secondary)',
    borderTop: `1px solid var(--editor-panel-border)`,
    position: 'relative',
    cursor: 'pointer',
    overflow: 'hidden',
    transition: 'height var(--editor-transition-base)',
  },
  containerExpanded: {
    height: '80px',
  },
  canvas: {
    width: '100%',
    height: '100%',
    display: 'block',
  },
  viewport: {
    position: 'absolute',
    top: '8px',
    height: 'calc(100% - 16px)',
    border: `2px solid var(--editor-accent)`,
    backgroundColor: 'var(--editor-focus-ring)',
    borderRadius: 'var(--editor-radius-sm)',
    cursor: 'grab',
    transition: 'border-color var(--editor-transition-fast)',
    pointerEvents: 'none',
    '&:hover': {
      borderColor: 'var(--editor-accent-hover)',
    } as GriffelStyle,
  },
  viewportDragging: {
    cursor: 'grabbing',
    borderColor: 'var(--editor-accent-active)',
  } as GriffelStyle,
  toggleButton: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    width: '24px',
    height: '24px',
    backgroundColor: 'var(--editor-panel-bg)',
    border: `1px solid var(--editor-panel-border)`,
    borderRadius: 'var(--editor-radius-sm)',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    fontSize: 'var(--editor-font-size-xs)',
    color: 'var(--editor-text-secondary)',
    padding: 0,
    transition: 'all var(--editor-transition-fast)',
    zIndex: 'var(--editor-z-toolbar)',
    '&:hover': {
      backgroundColor: 'var(--editor-panel-hover)',
      color: 'var(--editor-accent)',
      transform: 'scale(1.1)',
    },
  },
  tooltip: {
    position: 'absolute',
    bottom: '100%',
    left: '50%',
    transform: 'translateX(-50%) translateY(-4px)',
    backgroundColor: 'var(--editor-bg-elevated)',
    color: 'var(--editor-text-primary)',
    padding: 'var(--editor-space-xs) var(--editor-space-sm)',
    borderRadius: 'var(--editor-radius-sm)',
    fontSize: 'var(--editor-font-size-xs)',
    whiteSpace: 'nowrap',
    pointerEvents: 'none',
    opacity: 0,
    transition: 'opacity var(--editor-transition-fast)',
    boxShadow: 'var(--editor-shadow-md)',
    zIndex: 'var(--editor-z-dropdown)',
  },
  tooltipVisible: {
    opacity: 1,
  },
});

export interface TimelineMiniMapClip {
  id: string;
  startTime: number;
  duration: number;
  type: 'video' | 'audio' | 'image';
  trackIndex: number;
}

export interface TimelineMiniMapProps {
  clips: TimelineMiniMapClip[];
  currentTime: number;
  duration: number;
  viewportStart: number;
  viewportEnd: number;
  onSeek: (time: number) => void;
  onViewportChange?: (start: number, end: number) => void;
  pixelsPerSecond?: number;
  trackCount?: number;
  expanded?: boolean;
  onToggleExpand?: () => void;
}

export const TimelineMiniMap: React.FC<TimelineMiniMapProps> = ({
  clips,
  currentTime,
  duration,
  viewportStart,
  viewportEnd,
  onSeek,
  onViewportChange: _onViewportChange,
  trackCount = 4,
  expanded = false,
  onToggleExpand,
}) => {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isDraggingViewport, _setIsDraggingViewport] = useState(false);
  const [showTooltip, setShowTooltip] = useState(false);
  const [tooltipTime, setTooltipTime] = useState(0);
  const [tooltipX, setTooltipX] = useState(0);

  // Calculate viewport position and width as percentages
  const viewportLeft = useMemo(() => (viewportStart / duration) * 100, [viewportStart, duration]);
  const viewportWidth = useMemo(
    () => ((viewportEnd - viewportStart) / duration) * 100,
    [viewportStart, viewportEnd, duration]
  );

  // Draw mini-map on canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || clips.length === 0) {
      return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    const dpr = window.devicePixelRatio || 1;
    const rect = canvas.getBoundingClientRect();

    canvas.width = rect.width * dpr;
    canvas.height = rect.height * dpr;

    ctx.scale(dpr, dpr);

    const width = rect.width;
    const height = rect.height;
    const trackHeight = height / trackCount;

    ctx.clearRect(0, 0, width, height);

    clips.forEach((clip) => {
      const x = (clip.startTime / duration) * width;
      const clipWidth = (clip.duration / duration) * width;
      const y = clip.trackIndex * trackHeight;

      let color: string;
      switch (clip.type) {
        case 'video':
          color = 'var(--clip-video-bg)';
          break;
        case 'audio':
          color = 'var(--clip-audio-bg)';
          break;
        case 'image':
          color = 'var(--clip-image-bg)';
          break;
        default:
          color = 'var(--editor-panel-bg)';
      }

      const computedColor = getComputedStyle(document.documentElement)
        .getPropertyValue(color.replace('var(', '').replace(')', ''))
        .trim();

      ctx.fillStyle = computedColor || '#4a5568';
      ctx.fillRect(x, y, Math.max(clipWidth, 2), trackHeight - 2);

      ctx.strokeStyle = 'var(--clip-border)';
      ctx.lineWidth = 0.5;
      ctx.strokeRect(x, y, Math.max(clipWidth, 2), trackHeight - 2);
    });

    const playheadX = (currentTime / duration) * width;
    ctx.strokeStyle = getComputedStyle(document.documentElement)
      .getPropertyValue('--playhead-color')
      .trim();
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(playheadX, 0);
    ctx.lineTo(playheadX, height);
    ctx.stroke();
  }, [clips, currentTime, duration, trackCount]);

  const handleClick = useCallback(
    (event: React.MouseEvent<HTMLDivElement>) => {
      if (!containerRef.current || isDraggingViewport) {
        return;
      }

      const rect = containerRef.current.getBoundingClientRect();
      const x = event.clientX - rect.left;
      const ratio = x / rect.width;
      const time = ratio * duration;

      onSeek(Math.max(0, Math.min(time, duration)));
    },
    [duration, onSeek, isDraggingViewport]
  );

  const handleMouseMove = useCallback(
    (event: React.MouseEvent<HTMLDivElement>) => {
      if (!containerRef.current) {
        return;
      }

      const rect = containerRef.current.getBoundingClientRect();
      const x = event.clientX - rect.left;
      const ratio = x / rect.width;
      const time = ratio * duration;

      setTooltipTime(time);
      setTooltipX(x);
      setShowTooltip(true);
    },
    [duration]
  );

  const handleMouseLeave = useCallback(() => {
    setShowTooltip(false);
  }, []);

  const formatTime = useCallback((time: number): string => {
    const totalSeconds = Math.floor(time);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    const frames = Math.floor((time - totalSeconds) * 30);
    return `${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
  }, []);

  return (
    <div
      ref={containerRef}
      className={`${styles.container} ${expanded ? styles.containerExpanded : ''}`}
      onClick={handleClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          handleClick(e as unknown as React.MouseEvent<HTMLDivElement>);
        }
      }}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      role="slider"
      tabIndex={0}
      aria-label="Timeline mini-map"
      aria-valuemin={0}
      aria-valuemax={duration}
      aria-valuenow={currentTime}
    >
      <canvas ref={canvasRef} className={styles.canvas} />

      <div
        className={`${styles.viewport} ${isDraggingViewport ? styles.viewportDragging : ''}`}
        style={{
          left: `${viewportLeft}%`,
          width: `${viewportWidth}%`,
        }}
      />

      {showTooltip && (
        <div
          className={`${styles.tooltip} ${styles.tooltipVisible}`}
          style={{ left: `${tooltipX}px` }}
        >
          {formatTime(tooltipTime)}
        </div>
      )}

      {onToggleExpand && (
        <button
          type="button"
          className={styles.toggleButton}
          onClick={(e) => {
            e.stopPropagation();
            onToggleExpand();
          }}
          aria-label={expanded ? 'Collapse mini-map' : 'Expand mini-map'}
        >
          {expanded ? '−' : '+'}
        </button>
      )}
    </div>
  );
};
