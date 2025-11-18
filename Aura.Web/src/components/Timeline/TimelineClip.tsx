import { makeStyles, tokens, Tooltip, type GriffelStyle } from '@fluentui/react-components';
import React, { useState, useRef, useCallback } from 'react';
import { snapToFrame } from '../../services/timelineEngine';
import { AppliedEffect } from '../../types/effects';
import '../../styles/video-editor-theme.css';

const useStyles = makeStyles({
  clip: {
    position: 'absolute',
    top: '10px',
    height: '40px',
    borderRadius: 'var(--editor-radius-sm)',
    border: `1px solid var(--clip-border)`,
    cursor: 'grab',
    padding: '0 var(--editor-space-sm)',
    color: 'white',
    fontSize: 'var(--editor-font-size-sm)',
    fontWeight: 'var(--editor-font-weight-medium)',
    overflow: 'hidden',
    whiteSpace: 'nowrap',
    textOverflow: 'ellipsis',
    display: 'flex',
    alignItems: 'center',
    userSelect: 'none',
    transition: 'all var(--editor-transition-fast)',
    boxShadow: 'var(--editor-shadow-sm)',
    '&.video': {
      background:
        'linear-gradient(135deg, var(--clip-video-bg) 0%, color-mix(in srgb, var(--clip-video-bg) 80%, black) 100%)',
    },
    '&.audio': {
      background:
        'linear-gradient(135deg, var(--clip-audio-bg) 0%, color-mix(in srgb, var(--clip-audio-bg) 80%, black) 100%)',
    },
    '&.image': {
      background:
        'linear-gradient(135deg, var(--clip-image-bg) 0%, color-mix(in srgb, var(--clip-image-bg) 80%, black) 100%)',
    },
    '&:hover': {
      borderColor: 'var(--editor-accent)',
      boxShadow: '0 0 0 1px var(--editor-accent), var(--editor-shadow-md)',
      transform: 'translateY(-1px)',
    } as GriffelStyle,
  },
  clipSelected: {
    border: `2px solid var(--clip-selected)`,
    boxShadow: '0 0 0 1px var(--clip-selected), var(--editor-shadow-lg)',
    zIndex: 'var(--editor-z-panel)',
  },
  clipDragging: {
    cursor: 'grabbing',
    opacity: 0.85,
    zIndex: 100,
    boxShadow: 'var(--editor-shadow-xl)',
    transform: 'scale(1.02)',
  },
  clipTrimming: {
    cursor: 'ew-resize',
  },
  clipThumbnails: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    opacity: 0.3,
    pointerEvents: 'none',
    borderRadius: 'var(--editor-radius-sm)',
    overflow: 'hidden',
  },
  clipThumbnail: {
    height: '100%',
    objectFit: 'cover',
    borderRight: `1px solid rgb(255 255 255 / 10%)`,
  },
  clipWaveform: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    display: 'flex',
    alignItems: 'center',
    gap: '1px',
    padding: 'var(--editor-space-xs)',
    opacity: 0.5,
    pointerEvents: 'none',
  },
  waveformBar: {
    flex: 1,
    backgroundColor: 'white',
    minWidth: '2px',
    borderRadius: '1px',
    boxShadow: '0 1px 2px rgb(0 0 0 / 30%)',
  },
  clipLabel: {
    position: 'relative',
    zIndex: 1,
    textShadow: '0 1px 3px rgb(0 0 0 / 60%)',
    fontWeight: 'var(--editor-font-weight-medium)',
  },
  effectIndicator: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    width: '8px',
    height: '8px',
    borderRadius: '50%',
    backgroundColor: 'var(--editor-accent)',
    border: `1px solid white`,
    boxShadow: '0 1px 3px rgb(0 0 0 / 50%)',
    zIndex: 2,
  },
  trimHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '8px',
    cursor: 'ew-resize',
    zIndex: 10,
    transition: 'background-color var(--editor-transition-fast)',
    '&:hover': {
      backgroundColor: 'rgb(255 255 255 / 20%)',
    },
  },
  trimHandleLeft: {
    left: 0,
  },
  trimHandleRight: {
    right: 0,
  },
  trimPreview: {
    position: 'absolute',
    top: '-40px',
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    whiteSpace: 'nowrap',
    boxShadow: tokens.shadow8,
    zIndex: 100,
  },
});

export interface TimelineClipData {
  id: string;
  trackId: string;
  startTime: number;
  duration: number;
  label: string;
  type: 'video' | 'audio' | 'image';
  effects?: AppliedEffect[];
  mediaId?: string;
  file?: File;
  thumbnails?: Array<{ dataUrl: string; timestamp: number }>;
  waveform?: { peaks: number[]; duration: number };
  preview?: string;
  keyframes?: Record<string, Array<{ time: number; value: number | string | boolean }>>;
}

interface TimelineClipProps {
  clip: TimelineClipData;
  pixelsPerSecond: number;
  isSelected: boolean;
  onSelect: () => void;
  onMove: (clipId: string, newStartTime: number) => void;
  onTrim: (clipId: string, newStartTime: number, newDuration: number) => void;
  onDragStart?: () => void;
  onDragEnd?: () => void;
  snapping?: boolean;
  frameRate?: number;
}

export function TimelineClip({
  clip,
  pixelsPerSecond,
  isSelected,
  onSelect,
  onMove,
  onTrim,
  onDragStart,
  onDragEnd,
  snapping = true,
  frameRate = 30,
}: TimelineClipProps) {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const [isTrimming, setIsTrimming] = useState<'left' | 'right' | null>(null);
  const [showTrimPreview, setShowTrimPreview] = useState(false);
  const [trimDelta, setTrimDelta] = useState(0);
  const dragStartRef = useRef<{ startTime: number; duration: number; mouseX: number } | null>(null);

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onSelect();

      setIsDragging(true);
      onDragStart?.();

      const startX = e.clientX;
      dragStartRef.current = {
        startTime: clip.startTime,
        duration: clip.duration,
        mouseX: startX,
      };

      const handleMouseMove = (moveEvent: MouseEvent) => {
        if (!dragStartRef.current) return;

        const deltaX = moveEvent.clientX - dragStartRef.current.mouseX;
        const deltaTime = deltaX / pixelsPerSecond;
        let newStartTime = Math.max(0, dragStartRef.current.startTime + deltaTime);

        if (snapping) {
          newStartTime = snapToFrame(newStartTime, frameRate);
        }

        onMove(clip.id, newStartTime);
      };

      const handleMouseUp = () => {
        setIsDragging(false);
        dragStartRef.current = null;
        onDragEnd?.();
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [clip, pixelsPerSecond, onSelect, onMove, onDragStart, onDragEnd, snapping, frameRate]
  );

  const handleTrimMouseDown = useCallback(
    (e: React.MouseEvent, side: 'left' | 'right') => {
      e.stopPropagation();
      onSelect();

      setIsTrimming(side);
      setShowTrimPreview(true);

      const startX = e.clientX;
      dragStartRef.current = {
        startTime: clip.startTime,
        duration: clip.duration,
        mouseX: startX,
      };

      const handleMouseMove = (moveEvent: MouseEvent) => {
        if (!dragStartRef.current) return;

        const deltaX = moveEvent.clientX - dragStartRef.current.mouseX;
        const deltaTime = deltaX / pixelsPerSecond;

        let newStartTime = dragStartRef.current.startTime;
        let newDuration = dragStartRef.current.duration;

        if (side === 'left') {
          newStartTime = Math.max(0, dragStartRef.current.startTime + deltaTime);
          newDuration = Math.max(0.1, dragStartRef.current.duration - deltaTime);
        } else {
          newDuration = Math.max(0.1, dragStartRef.current.duration + deltaTime);
        }

        if (snapping) {
          newStartTime = snapToFrame(newStartTime, frameRate);
          newDuration = snapToFrame(newDuration, frameRate);
        }

        setTrimDelta(
          side === 'left'
            ? newStartTime - dragStartRef.current.startTime
            : newDuration - dragStartRef.current.duration
        );
        onTrim(clip.id, newStartTime, newDuration);
      };

      const handleMouseUp = () => {
        setIsTrimming(null);
        setShowTrimPreview(false);
        setTrimDelta(0);
        dragStartRef.current = null;
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [clip, pixelsPerSecond, onSelect, onTrim, snapping, frameRate]
  );

  const clipStyle = {
    left: `${clip.startTime * pixelsPerSecond}px`,
    width: `${clip.duration * pixelsPerSecond}px`,
  };

  const formatTrimDelta = (delta: number): string => {
    const frames = Math.round(Math.abs(delta) * frameRate);
    const sign = delta >= 0 ? '+' : '-';
    return `${sign}${frames}f`;
  };

  return (
    <div
      className={`${styles.clip} ${isSelected ? styles.clipSelected : ''} ${
        isDragging ? styles.clipDragging : ''
      } ${isTrimming ? styles.clipTrimming : ''}`}
      style={clipStyle}
      onMouseDown={handleMouseDown}
      role="button"
      tabIndex={0}
      aria-label={`${clip.label} clip`}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onSelect();
        } else if ((e.key === 'ArrowLeft' || e.key === 'ArrowRight') && isSelected) {
          e.preventDefault();
          const step = e.shiftKey ? 1 : 1 / frameRate;
          const delta = e.key === 'ArrowLeft' ? -step : step;
          const newStartTime = Math.max(0, clip.startTime + delta);
          onMove(clip.id, snapping ? snapToFrame(newStartTime, frameRate) : newStartTime);
        }
      }}
    >
      {/* Thumbnails for video clips */}
      {clip.type === 'video' && clip.thumbnails && clip.thumbnails.length > 0 && (
        <div className={styles.clipThumbnails}>
          {clip.thumbnails.map((thumbnail, idx) => (
            <img
              key={idx}
              src={thumbnail.dataUrl}
              alt=""
              className={styles.clipThumbnail}
              style={{ flex: 1 }}
            />
          ))}
        </div>
      )}

      {/* Waveform for audio clips */}
      {(clip.type === 'audio' || (clip.type === 'video' && !clip.thumbnails)) &&
        clip.waveform &&
        clip.waveform.peaks.length > 0 && (
          <div className={styles.clipWaveform}>
            {clip.waveform.peaks.map((peak, idx) => (
              <div
                key={idx}
                className={styles.waveformBar}
                style={{ height: `${Math.max(2, peak * 100)}%` }}
              />
            ))}
          </div>
        )}

      <span className={styles.clipLabel}>{clip.label}</span>

      {/* Effect indicator */}
      {clip.effects && clip.effects.length > 0 && (
        <Tooltip content={`${clip.effects.length} effect(s)`} relationship="label">
          <div className={styles.effectIndicator} />
        </Tooltip>
      )}

      {/* Trim handles */}
      <div
        className={`${styles.trimHandle} ${styles.trimHandleLeft}`}
        role="button"
        tabIndex={0}
        onMouseDown={(e) => handleTrimMouseDown(e, 'left')}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            // Trim operations are primarily mouse-based
          }
        }}
      />
      <div
        className={`${styles.trimHandle} ${styles.trimHandleRight}`}
        role="button"
        tabIndex={0}
        onMouseDown={(e) => handleTrimMouseDown(e, 'right')}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            // Trim operations are primarily mouse-based
          }
        }}
      />

      {/* Trim preview tooltip */}
      {showTrimPreview && (
        <div
          className={styles.trimPreview}
          style={{
            left: isTrimming === 'left' ? 0 : 'auto',
            right: isTrimming === 'right' ? 0 : 'auto',
          }}
        >
          {formatTrimDelta(trimDelta)}
        </div>
      )}
    </div>
  );
}
