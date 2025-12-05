/**
 * CaptionTrack Component
 *
 * Timeline track for captions:
 * - Visual caption blocks
 * - Drag to adjust timing
 * - Resize handles for duration
 * - Click to select and edit
 * - Double-click to add new caption
 */

import { makeStyles, tokens, mergeClasses, Tooltip, Button } from '@fluentui/react-components';
import {
  ClosedCaption24Regular,
  Eye24Regular,
  EyeOff24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
} from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback, useRef } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { openCutTokens } from '../../../styles/designTokens';
import type { CaptionTrack } from '../../../types/opencut';

export interface CaptionTrackComponentProps {
  track: CaptionTrack;
  pixelsPerSecond: number;
  totalWidth: number;
  currentTime: number;
  className?: string;
}

const useStyles = makeStyles({
  track: {
    display: 'flex',
    minHeight: '48px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  trackSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
  },
  trackLocked: {
    opacity: 0.6,
  },
  trackLabel: {
    width: '140px',
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    padding: `0 ${tokens.spacingHorizontalS}`,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  trackLabelIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '16px',
  },
  trackLabelText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontWeight: tokens.fontWeightMedium,
    flex: 1,
    minWidth: 0,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  trackControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  trackControlButton: {
    minWidth: '20px',
    minHeight: '20px',
    padding: '2px',
  },
  trackContentScrollable: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
  },
  trackContent: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    left: 0,
    backgroundColor: tokens.colorNeutralBackground4,
  },
  captionBlock: {
    position: 'absolute',
    top: '4px',
    bottom: '4px',
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    padding: `0 ${tokens.spacingHorizontalXS}`,
    backgroundColor: '#F97316',
    border: '1px solid rgba(255, 255, 255, 0.2)',
    transition: 'box-shadow 100ms ease-out, transform 100ms ease-out',
    ':hover': {
      boxShadow: tokens.shadow8,
      transform: 'translateY(-1px)',
    },
  },
  captionBlockSelected: {
    boxShadow: `0 0 0 2px ${tokens.colorBrandStroke1}`,
    transform: 'translateY(-1px)',
  },
  captionBlockDragging: {
    opacity: 0.8,
    cursor: 'grabbing',
  },
  captionText: {
    fontSize: tokens.fontSizeBase100,
    color: 'white',
    fontWeight: tokens.fontWeightMedium,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  captionTrimHandle: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '6px',
    backgroundColor: 'rgba(255,255,255,0.3)',
    cursor: 'ew-resize',
    opacity: 0,
    transition: 'opacity 100ms ease-out',
    ':hover': {
      opacity: 1,
      backgroundColor: 'rgba(255,255,255,0.5)',
    },
  },
  captionTrimHandleLeft: {
    left: 0,
    borderRadius: `${tokens.borderRadiusMedium} 0 0 ${tokens.borderRadiusMedium}`,
  },
  captionTrimHandleRight: {
    right: 0,
    borderRadius: `0 ${tokens.borderRadiusMedium} ${tokens.borderRadiusMedium} 0`,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: openCutTokens.colors.playhead,
    zIndex: openCutTokens.zIndex.sticky - 5,
    pointerEvents: 'none',
  },
});

export const CaptionTrackComponent: FC<CaptionTrackComponentProps> = ({
  track,
  pixelsPerSecond,
  totalWidth,
  currentTime,
  className,
}) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();

  const {
    selectedCaptionId,
    selectedTrackId,
    selectCaption,
    selectTrack,
    setTrackVisibility,
    setTrackLocked,
    setCaptionTiming,
    addCaption,
  } = captionsStore;

  const [isDragging, setIsDragging] = useState(false);
  const [dragCaptionId, setDragCaptionId] = useState<string | null>(null);
  const [_dragStartTime, setDragStartTime] = useState(0);
  const [dragCurrentTime, setDragCurrentTime] = useState(0);
  const [dragType, setDragType] = useState<'move' | 'resize-start' | 'resize-end'>('move');
  const dragStartXRef = useRef(0);
  const dragOriginalStartRef = useRef(0);
  const dragOriginalEndRef = useRef(0);

  const isSelected = selectedTrackId === track.id;
  const playheadPosition = currentTime * pixelsPerSecond;

  const handleTrackClick = useCallback(() => {
    selectTrack(track.id);
  }, [track.id, selectTrack]);

  const handleTrackDoubleClick = useCallback(
    (e: ReactMouseEvent) => {
      if (track.locked) return;
      const rect = e.currentTarget.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const time = x / pixelsPerSecond;
      const captionId = addCaption(track.id, time, time + 2, 'New caption');
      selectCaption(captionId);
    },
    [track.id, track.locked, pixelsPerSecond, addCaption, selectCaption]
  );

  const handleCaptionClick = useCallback(
    (e: ReactMouseEvent, captionId: string, startTime: number) => {
      e.stopPropagation();
      selectCaption(captionId);
      playbackStore.seek(startTime);
    },
    [selectCaption, playbackStore]
  );

  const handleCaptionMouseDown = useCallback(
    (e: ReactMouseEvent, captionId: string, type: 'move' | 'resize-start' | 'resize-end') => {
      if (track.locked) return;
      e.stopPropagation();

      const caption = track.captions.find((c) => c.id === captionId);
      if (!caption) return;

      setIsDragging(true);
      setDragCaptionId(captionId);
      setDragType(type);
      setDragStartTime(caption.startTime);
      setDragCurrentTime(caption.startTime);
      dragStartXRef.current = e.clientX;
      dragOriginalStartRef.current = caption.startTime;
      dragOriginalEndRef.current = caption.endTime;

      const handleMouseMove = (moveEvent: MouseEvent) => {
        const deltaX = moveEvent.clientX - dragStartXRef.current;
        const deltaTime = deltaX / pixelsPerSecond;

        if (type === 'move') {
          const newStart = Math.max(0, dragOriginalStartRef.current + deltaTime);
          const duration = dragOriginalEndRef.current - dragOriginalStartRef.current;
          setDragCurrentTime(newStart);
          setCaptionTiming(captionId, newStart, newStart + duration);
        } else if (type === 'resize-start') {
          const newStart = Math.max(
            0,
            Math.min(dragOriginalEndRef.current - 0.1, dragOriginalStartRef.current + deltaTime)
          );
          setCaptionTiming(captionId, newStart, dragOriginalEndRef.current);
        } else if (type === 'resize-end') {
          const newEnd = Math.max(
            dragOriginalStartRef.current + 0.1,
            dragOriginalEndRef.current + deltaTime
          );
          setCaptionTiming(captionId, dragOriginalStartRef.current, newEnd);
        }
      };

      const handleMouseUp = () => {
        setIsDragging(false);
        setDragCaptionId(null);
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };

      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
    },
    [track.locked, track.captions, pixelsPerSecond, setCaptionTiming]
  );

  return (
    <div
      className={mergeClasses(
        styles.track,
        isSelected && styles.trackSelected,
        track.locked && styles.trackLocked,
        className
      )}
      onClick={handleTrackClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          handleTrackClick();
        }
      }}
      role="button"
      tabIndex={0}
      aria-pressed={isSelected}
    >
      <div className={styles.trackLabel}>
        <span className={styles.trackLabelIcon}>
          <ClosedCaption24Regular />
        </span>
        <span className={styles.trackLabelText}>{track.name}</span>
        <div className={styles.trackControls}>
          <Tooltip content={track.visible ? 'Hide' : 'Show'} relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={styles.trackControlButton}
              icon={track.visible ? <Eye24Regular /> : <EyeOff24Regular />}
              onClick={(e) => {
                e.stopPropagation();
                setTrackVisibility(track.id, !track.visible);
              }}
            />
          </Tooltip>
          <Tooltip content={track.locked ? 'Unlock' : 'Lock'} relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={styles.trackControlButton}
              icon={track.locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
              onClick={(e) => {
                e.stopPropagation();
                setTrackLocked(track.id, !track.locked);
              }}
            />
          </Tooltip>
        </div>
      </div>
      <div className={styles.trackContentScrollable}>
        <div
          className={styles.trackContent}
          style={{ width: totalWidth }}
          onDoubleClick={handleTrackDoubleClick}
        >
          <AnimatePresence>
            {track.captions.map((caption) => {
              const isBeingDragged = isDragging && dragCaptionId === caption.id;
              const displayTime =
                isBeingDragged && dragType === 'move' ? dragCurrentTime : caption.startTime;
              const left = displayTime * pixelsPerSecond;
              const width = (caption.endTime - caption.startTime) * pixelsPerSecond;

              return (
                <motion.div
                  key={caption.id}
                  className={mergeClasses(
                    styles.captionBlock,
                    selectedCaptionId === caption.id && styles.captionBlockSelected,
                    isBeingDragged && styles.captionBlockDragging
                  )}
                  style={{
                    left,
                    width: Math.max(width, 30),
                  }}
                  onClick={(e) => handleCaptionClick(e, caption.id, caption.startTime)}
                  onMouseDown={(e) => handleCaptionMouseDown(e, caption.id, 'move')}
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.95 }}
                  transition={{ duration: 0.15 }}
                >
                  <span className={styles.captionText}>{caption.text}</span>
                  {/* Resize handles */}
                  <div
                    className={mergeClasses(styles.captionTrimHandle, styles.captionTrimHandleLeft)}
                    onMouseDown={(e) => {
                      e.stopPropagation();
                      handleCaptionMouseDown(e, caption.id, 'resize-start');
                    }}
                  />
                  <div
                    className={mergeClasses(
                      styles.captionTrimHandle,
                      styles.captionTrimHandleRight
                    )}
                    onMouseDown={(e) => {
                      e.stopPropagation();
                      handleCaptionMouseDown(e, caption.id, 'resize-end');
                    }}
                  />
                </motion.div>
              );
            })}
          </AnimatePresence>

          {/* Playhead */}
          <motion.div
            className={styles.playhead}
            style={{ left: playheadPosition }}
            initial={false}
            animate={{ left: playheadPosition }}
            transition={{ type: 'tween', duration: 0.05 }}
          />
        </div>
      </div>
    </div>
  );
};

export default CaptionTrackComponent;
