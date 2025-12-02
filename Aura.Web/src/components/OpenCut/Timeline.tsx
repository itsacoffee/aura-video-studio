/**
 * Timeline Component
 *
 * Professional timeline with increased height, better track visualization,
 * time ruler, and premium playhead design following Apple HIG.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  Badge,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Video24Regular,
  MusicNote224Regular,
  TextT24Regular,
  Cut24Regular,
  Copy24Regular,
  Delete24Regular,
  Add24Regular,
  ZoomIn24Regular,
  ZoomOut24Regular,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useState, useCallback, useRef, useEffect } from 'react';
import type { FC, MouseEvent as ReactMouseEvent } from 'react';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useOpenCutProjectStore } from '../../stores/opencutProject';

export interface TimelineProps {
  className?: string;
  onResize?: (height: number) => void;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    position: 'relative',
  },
  resizeHandle: {
    position: 'absolute',
    top: '-4px',
    left: 0,
    right: 0,
    height: '8px',
    cursor: 'ns-resize',
    zIndex: 20,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ':hover::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
    '::after': {
      content: '""',
      width: '48px',
      height: '3px',
      backgroundColor: tokens.colorNeutralStroke3,
      borderRadius: tokens.borderRadiusMedium,
      opacity: 0,
      transition: 'opacity 150ms ease-out, background-color 150ms ease-out',
    },
  },
  resizeHandleActive: {
    '::after': {
      backgroundColor: tokens.colorBrandStroke1,
      opacity: 1,
    },
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '52px',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  zoomControls: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginRight: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalM,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  content: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    position: 'relative',
  },
  rulerArea: {
    display: 'flex',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '32px',
  },
  rulerLabels: {
    width: '120px',
    flexShrink: 0,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  ruler: {
    flex: 1,
    position: 'relative',
    overflow: 'hidden',
  },
  rulerMarkers: {
    display: 'flex',
    alignItems: 'flex-end',
    height: '100%',
    paddingTop: tokens.spacingVerticalXS,
  },
  rulerMark: {
    position: 'absolute',
    bottom: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
  },
  rulerMarkLine: {
    width: '1px',
    backgroundColor: tokens.colorNeutralStroke3,
  },
  rulerMarkLabel: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    marginTop: tokens.spacingVerticalXXS,
  },
  tracksArea: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'auto',
  },
  track: {
    display: 'flex',
    minHeight: '56px',
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  trackLabel: {
    width: '120px',
    flexShrink: 0,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `0 ${tokens.spacingHorizontalM}`,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  trackLabelIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '18px',
  },
  trackLabelText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontWeight: tokens.fontWeightMedium,
  },
  trackContent: {
    flex: 1,
    position: 'relative',
    backgroundColor: tokens.colorNeutralBackground4,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    zIndex: 15,
    pointerEvents: 'none',
  },
  playheadHandle: {
    position: 'absolute',
    top: '-2px',
    left: '-7px',
    width: '16px',
    height: '16px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    borderRadius: '4px 4px 50% 50%',
    boxShadow: tokens.shadow4,
    pointerEvents: 'auto',
    cursor: 'ew-resize',
    transition: 'transform 100ms ease-out',
    ':hover': {
      transform: 'scale(1.1)',
    },
  },
  emptyTimeline: {
    flex: 1,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  controlButton: {
    minWidth: '36px',
    minHeight: '36px',
  },
});

function formatTimeRuler(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export const Timeline: FC<TimelineProps> = ({ className, onResize }) => {
  const styles = useStyles();
  const playbackStore = useOpenCutPlaybackStore();
  const projectStore = useOpenCutProjectStore();
  const [isResizing, setIsResizing] = useState(false);
  const [zoom, setZoom] = useState(1);
  const containerRef = useRef<HTMLDivElement>(null);
  const startYRef = useRef(0);
  const startHeightRef = useRef(0);

  const tracks = [
    { id: 'video', label: 'Video', icon: <Video24Regular className={styles.trackLabelIcon} /> },
    {
      id: 'audio',
      label: 'Audio',
      icon: <MusicNote224Regular className={styles.trackLabelIcon} />,
    },
    { id: 'text', label: 'Text', icon: <TextT24Regular className={styles.trackLabelIcon} /> },
  ];

  const duration = playbackStore.duration;
  const currentTime = playbackStore.currentTime;
  const playheadPosition = duration > 0 ? (currentTime / duration) * 100 : 0;

  // Generate time ruler marks
  const rulerMarks: { time: number; position: number }[] = [];
  const markInterval = duration > 60 ? 10 : duration > 30 ? 5 : 1;
  for (let t = 0; t <= duration; t += markInterval) {
    rulerMarks.push({
      time: t,
      position: (t / duration) * 100,
    });
  }

  const handleResizeStart = useCallback((e: ReactMouseEvent) => {
    e.preventDefault();
    setIsResizing(true);
    startYRef.current = e.clientY;
    startHeightRef.current = containerRef.current?.offsetHeight || 280;
  }, []);

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: globalThis.MouseEvent) => {
      const delta = startYRef.current - e.clientY;
      const newHeight = Math.max(200, Math.min(500, startHeightRef.current + delta));
      if (containerRef.current) {
        containerRef.current.style.height = `${newHeight}px`;
      }
      onResize?.(newHeight);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isResizing, onResize]);

  const handleZoomIn = useCallback(() => {
    setZoom((prev) => Math.min(4, prev * 1.5));
  }, []);

  const handleZoomOut = useCallback(() => {
    setZoom((prev) => Math.max(0.25, prev / 1.5));
  }, []);

  return (
    <div
      ref={containerRef}
      className={mergeClasses(styles.container, className)}
      style={{ height: '280px', minHeight: '200px', maxHeight: '500px' }}
    >
      {/* Resize Handle */}
      <button
        type="button"
        className={mergeClasses(styles.resizeHandle, isResizing && styles.resizeHandleActive)}
        onMouseDown={handleResizeStart}
        aria-label="Resize timeline"
        style={{
          background: 'transparent',
          border: 'none',
          padding: 0,
          margin: 0,
        }}
      />

      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Text weight="semibold" size={400}>
            Timeline
          </Text>
          <Badge appearance="outline" size="small">
            {projectStore.activeProject?.fps || 30} fps
          </Badge>
        </div>
        <div className={styles.headerRight}>
          {/* Zoom Controls */}
          <div className={styles.zoomControls}>
            <Tooltip content="Zoom out" relationship="label">
              <Button
                appearance="subtle"
                icon={<ZoomOut24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleZoomOut}
              />
            </Tooltip>
            <Text size={200} style={{ minWidth: '40px', textAlign: 'center' }}>
              {Math.round(zoom * 100)}%
            </Text>
            <Tooltip content="Zoom in" relationship="label">
              <Button
                appearance="subtle"
                icon={<ZoomIn24Regular />}
                size="small"
                className={styles.controlButton}
                onClick={handleZoomIn}
              />
            </Tooltip>
          </div>

          {/* Edit Controls */}
          <Tooltip content="Split at playhead (S)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Cut24Regular />}
              size="small"
              className={styles.controlButton}
            />
          </Tooltip>
          <Tooltip content="Duplicate (Cmd+D)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Copy24Regular />}
              size="small"
              className={styles.controlButton}
            />
          </Tooltip>
          <Tooltip content="Delete (Del)" relationship="label">
            <Button
              appearance="subtle"
              icon={<Delete24Regular />}
              size="small"
              className={styles.controlButton}
            />
          </Tooltip>
          <Tooltip content="Add track" relationship="label">
            <Button
              appearance="subtle"
              icon={<Add24Regular />}
              size="small"
              className={styles.controlButton}
            />
          </Tooltip>
        </div>
      </div>

      {/* Timeline Content */}
      <div className={styles.content}>
        {/* Time Ruler */}
        <div className={styles.rulerArea}>
          <div className={styles.rulerLabels}>
            <Text size={100} style={{ color: tokens.colorNeutralForeground3 }}>
              Tracks
            </Text>
          </div>
          <div className={styles.ruler}>
            {rulerMarks.map((mark) => (
              <div
                key={mark.time}
                className={styles.rulerMark}
                style={{ left: `${mark.position}%` }}
              >
                <div
                  className={styles.rulerMarkLine}
                  style={{ height: mark.time % (markInterval * 2) === 0 ? '12px' : '6px' }}
                />
                {mark.time % (markInterval * 2) === 0 && (
                  <span className={styles.rulerMarkLabel}>{formatTimeRuler(mark.time)}</span>
                )}
              </div>
            ))}

            {/* Playhead in ruler */}
            <motion.div
              className={styles.playhead}
              style={{ left: `${playheadPosition}%` }}
              initial={false}
              animate={{ left: `${playheadPosition}%` }}
              transition={{ type: 'tween', duration: 0.05 }}
            >
              <div className={styles.playheadHandle} />
            </motion.div>
          </div>
        </div>

        {/* Tracks */}
        <div className={styles.tracksArea}>
          {tracks.map((track) => (
            <div key={track.id} className={styles.track}>
              <div className={styles.trackLabel}>
                {track.icon}
                <span className={styles.trackLabelText}>{track.label}</span>
              </div>
              <div className={styles.trackContent}>
                {/* Clips would render here */}
                {/* Playhead line continues through tracks */}
                <motion.div
                  className={styles.playhead}
                  style={{ left: `${playheadPosition}%` }}
                  initial={false}
                  animate={{ left: `${playheadPosition}%` }}
                  transition={{ type: 'tween', duration: 0.05 }}
                />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Timeline;
