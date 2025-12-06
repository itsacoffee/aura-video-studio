/**
 * PreviewPanel Component
 *
 * Enhanced preview area with professional features:
 * - Safe area guides toggle (title safe, action safe)
 * - Zoom controls (fit, 100%, custom zoom)
 * - Quality selector for preview
 * - Timecode overlay option
 * - Loop playback toggle
 */

import {
  makeStyles,
  tokens,
  Text,
  Spinner,
  mergeClasses,
  Button,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
} from '@fluentui/react-components';
import {
  Video24Regular,
  ZoomIn24Regular,
  ZoomOut24Regular,
  ZoomFit24Regular,
  Settings24Regular,
  Grid24Regular,
  Timer24Regular,
  ArrowRepeatAll24Regular,
  ArrowExportLtr24Regular,
} from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { FC } from 'react';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useOpenCutTimelineStore } from '../../stores/opencutTimeline';
import { openCutTokens } from '../../styles/designTokens';
import { CaptionPreview } from './Captions';
import { ExportDialog } from './Export';
import { PlaybackControls } from './PlaybackControls';

export interface PreviewPanelProps {
  className?: string;
  isLoading?: boolean;
}

type PreviewQuality = 'quarter' | 'half' | 'full';
type ZoomLevel = 'fit' | '50' | '100' | '200';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    minWidth: 0,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.xs} ${openCutTokens.spacing.md}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground2,
    minHeight: openCutTokens.layout.toolbarHeight,
  },
  toolbarGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
  },
  toolButton: {
    minWidth: openCutTokens.layout.controlButtonSizeCompact,
    minHeight: openCutTokens.layout.controlButtonSizeCompact,
  },
  toolButtonActive: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    color: tokens.colorBrandForeground1,
  },
  zoomText: {
    minWidth: '44px',
    textAlign: 'center',
    fontSize: openCutTokens.typography.fontSize.sm,
    color: tokens.colorNeutralForeground2,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  previewArea: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground3,
    position: 'relative',
    overflow: 'hidden',
    minHeight: 0,
    minWidth: 0,
  },
  canvasWrapper: {
    position: 'relative',
    width: '100%',
    height: '100%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flex: 1,
    minHeight: 0,
    minWidth: 0,
  },
  canvas: {
    width: 'auto',
    height: 'auto',
    maxWidth: '100%',
    maxHeight: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: '#000000',
    borderRadius: openCutTokens.radius.md,
    boxShadow: openCutTokens.shadows.md,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    position: 'relative',
    transition: `box-shadow ${openCutTokens.animation.duration.normal} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      boxShadow: openCutTokens.shadows.lg,
    },
  },
  canvasGlow: {
    position: 'absolute',
    inset: '-20px',
    background: 'radial-gradient(ellipse at center, rgba(0, 120, 212, 0.08) 0%, transparent 70%)',
    pointerEvents: 'none',
    zIndex: 0,
  },
  safeAreaGuides: {
    position: 'absolute',
    inset: 0,
    pointerEvents: 'none',
    zIndex: 5,
  },
  safeAreaTitle: {
    position: 'absolute',
    top: '10%',
    left: '10%',
    right: '10%',
    bottom: '10%',
    border: '1px dashed rgba(255, 255, 255, 0.4)',
    borderRadius: openCutTokens.radius.xs,
  },
  safeAreaAction: {
    position: 'absolute',
    top: '5%',
    left: '5%',
    right: '5%',
    bottom: '5%',
    border: '1px dashed rgba(255, 255, 255, 0.25)',
    borderRadius: openCutTokens.radius.xs,
  },
  safeAreaLabel: {
    position: 'absolute',
    fontSize: openCutTokens.typography.fontSize.xs,
    color: 'rgba(255, 255, 255, 0.5)',
    padding: `2px ${openCutTokens.spacing.xs}`,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    borderRadius: openCutTokens.radius.xs,
  },
  videoElement: {
    width: '100%',
    height: '100%',
    objectFit: 'contain',
    borderRadius: openCutTokens.radius.lg,
  },
  videoElementFit: {
    objectFit: 'contain',
    maxWidth: '100%',
    maxHeight: '100%',
    width: 'auto',
    height: 'auto',
  },
  videoElement50: {
    objectFit: 'none',
    transform: 'scale(0.5)',
  },
  videoElement100: {
    objectFit: 'none',
  },
  videoElement200: {
    objectFit: 'none',
    transform: 'scale(2)',
  },
  placeholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: openCutTokens.spacing.lg,
    padding: openCutTokens.spacing.xxl,
    color: tokens.colorNeutralForeground4,
    textAlign: 'center',
    zIndex: 1,
  },
  placeholderIcon: {
    width: '64px',
    height: '64px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    color: 'rgba(255, 255, 255, 0.3)',
    '& svg': {
      width: '32px',
      height: '32px',
    },
  },
  placeholderText: {
    color: 'rgba(255, 255, 255, 0.5)',
    fontSize: openCutTokens.typography.fontSize.md,
  },
  loadingOverlay: {
    position: 'absolute',
    inset: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    borderRadius: openCutTokens.radius.lg,
    gap: openCutTokens.spacing.md,
    zIndex: 2,
  },
  timecodeOverlay: {
    position: 'absolute',
    top: openCutTokens.spacing.md,
    left: openCutTokens.spacing.md,
    padding: `${openCutTokens.spacing.xxs} ${openCutTokens.spacing.sm}`,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    color: 'white',
    borderRadius: openCutTokens.radius.sm,
    fontSize: openCutTokens.typography.fontSize.sm,
    fontFamily: openCutTokens.typography.fontFamily.mono,
    zIndex: 3,
  },
  aspectRatioLabel: {
    position: 'absolute',
    bottom: openCutTokens.spacing.md,
    right: openCutTokens.spacing.md,
    padding: `${openCutTokens.spacing.xxs} ${openCutTokens.spacing.sm}`,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: 'rgba(255, 255, 255, 0.8)',
    borderRadius: openCutTokens.radius.sm,
    fontSize: openCutTokens.typography.fontSize.xs,
    fontFamily: openCutTokens.typography.fontFamily.mono,
    zIndex: 1,
  },
  qualityBadge: {
    position: 'absolute',
    top: openCutTokens.spacing.md,
    right: openCutTokens.spacing.md,
    padding: `${openCutTokens.spacing.xxs} ${openCutTokens.spacing.sm}`,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: 'rgba(255, 255, 255, 0.8)',
    borderRadius: openCutTokens.radius.sm,
    fontSize: openCutTokens.typography.fontSize.xs,
    zIndex: 1,
  },
});

function formatTimecode(seconds: number, fps: number = 30): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);
  const f = Math.floor((seconds % 1) * fps);
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}:${f.toString().padStart(2, '0')}`;
}

export const PreviewPanel: FC<PreviewPanelProps> = ({ className, isLoading = false }) => {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);

  // Connect to stores
  const playbackStore = useOpenCutPlaybackStore();
  const timelineStore = useOpenCutTimelineStore();
  const mediaStore = useOpenCutMediaStore();

  const [showSafeAreas, setShowSafeAreas] = useState(false);
  const [showTimecode, setShowTimecode] = useState(false);
  const [loopPlayback, setLoopPlayback] = useState(false);
  const [zoom, setZoom] = useState<ZoomLevel>('fit');
  const [quality, setQuality] = useState<PreviewQuality>('full');

  // Generate video source from timeline clips
  const videoSrc = useMemo(() => {
    // Get all video clips from Video 1 track
    const videoTrack = timelineStore.tracks.find((t) => t.type === 'video');
    if (!videoTrack) return undefined;

    const videoClips = timelineStore.clips
      .filter((c) => c.trackId === videoTrack.id && c.type === 'video')
      .sort((a, b) => a.startTime - b.startTime);

    if (videoClips.length === 0) return undefined;

    // For now, use the first video clip's media file
    const firstClip = videoClips[0];
    if (!firstClip.mediaId) return undefined;

    const mediaFile = mediaStore.getMediaById(firstClip.mediaId);
    return mediaFile?.url;
  }, [timelineStore.tracks, timelineStore.clips, mediaStore]);

  // Check if timeline has content
  const hasContent = timelineStore.clips.length > 0;

  // Sync video playback with playback store
  useEffect(() => {
    const video = videoRef.current;
    if (!video || !videoSrc) return;

    const handleLoadedMetadata = () => {
      playbackStore.setDuration(video.duration);
    };

    const handleTimeUpdate = () => {
      // ARCHITECTURAL FIX: Always sync time updates, not just when playing
      // This was causing the bug where seeking while paused didn't update the preview
      // The check for isPlaying prevented video preview from updating during playhead drag
      playbackStore.setCurrentTime(video.currentTime);
    };

    video.addEventListener('loadedmetadata', handleLoadedMetadata);
    video.addEventListener('timeupdate', handleTimeUpdate);

    return () => {
      video.removeEventListener('loadedmetadata', handleLoadedMetadata);
      video.removeEventListener('timeupdate', handleTimeUpdate);
    };
  }, [videoSrc, playbackStore]);

  // Sync playback state changes to video element
  useEffect(() => {
    const video = videoRef.current;
    if (!video || !videoSrc) return;

    if (playbackStore.isPlaying) {
      video.play().catch((err: unknown) => {
        console.error('Failed to play video:', err);
        playbackStore.pause();
      });
    } else {
      video.pause();
    }
  }, [playbackStore.isPlaying, videoSrc, playbackStore]);

  // ARCHITECTURAL FIX: Sync video position from playback store (for seek/playhead drag)
  // This ensures the video element updates when the playhead is dragged while paused
  useEffect(() => {
    const video = videoRef.current;
    if (!video || !videoSrc) return;
    
    // Only sync if there's a significant difference (avoid feedback loop)
    // Use 0.1 second threshold to avoid excessive updates
    if (Math.abs(video.currentTime - playbackStore.currentTime) > 0.1) {
      video.currentTime = playbackStore.currentTime;
    }
  }, [playbackStore.currentTime, videoSrc]);

  // Sync seek events to video element
  useEffect(() => {
    const video = videoRef.current;
    if (!video || !videoSrc) return;

    const handleSeek = (e: Event) => {
      const customEvent = e as CustomEvent<{ time: number }>;
      video.currentTime = customEvent.detail.time;
    };

    window.addEventListener('opencut-playback-seek', handleSeek);

    return () => {
      window.removeEventListener('opencut-playback-seek', handleSeek);
    };
  }, [videoSrc]);

  // Sync volume and muted state to video element
  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    video.volume = playbackStore.volume;
    video.muted = playbackStore.muted;
  }, [playbackStore.volume, playbackStore.muted]);

  const handleFullscreen = useCallback(() => {
    if (!containerRef.current) return;

    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen().catch(() => {
        // Fullscreen request failed - browser may not support or user declined
      });
    } else {
      document.exitFullscreen().catch(() => {
        // Exit fullscreen failed - already not in fullscreen
      });
    }
  }, []);

  const handleZoomIn = useCallback(() => {
    switch (zoom) {
      case 'fit':
        setZoom('50');
        break;
      case '50':
        setZoom('100');
        break;
      case '100':
        setZoom('200');
        break;
    }
  }, [zoom]);

  const handleZoomOut = useCallback(() => {
    switch (zoom) {
      case '200':
        setZoom('100');
        break;
      case '100':
        setZoom('50');
        break;
      case '50':
        setZoom('fit');
        break;
    }
  }, [zoom]);

  const getZoomPercent = () => {
    switch (zoom) {
      case 'fit':
        return 'Fit';
      case '50':
        return '50%';
      case '100':
        return '100%';
      case '200':
        return '200%';
    }
  };

  const getQualityLabel = () => {
    switch (quality) {
      case 'quarter':
        return '1/4';
      case 'half':
        return '1/2';
      case 'full':
        return 'Full';
    }
  };

  return (
    <div ref={containerRef} className={mergeClasses(styles.container, className)}>
      {/* Toolbar */}
      <div className={styles.toolbar}>
        <div className={styles.toolbarGroup}>
          {/* Zoom Controls */}
          <Tooltip content="Zoom out" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={styles.toolButton}
              icon={<ZoomOut24Regular />}
              onClick={handleZoomOut}
              disabled={zoom === 'fit'}
            />
          </Tooltip>
          <Text className={styles.zoomText}>{getZoomPercent()}</Text>
          <Tooltip content="Zoom in" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={styles.toolButton}
              icon={<ZoomIn24Regular />}
              onClick={handleZoomIn}
              disabled={zoom === '200'}
            />
          </Tooltip>
          <Tooltip content="Fit to window" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(styles.toolButton, zoom === 'fit' && styles.toolButtonActive)}
              icon={<ZoomFit24Regular />}
              onClick={() => setZoom('fit')}
            />
          </Tooltip>
        </div>

        <div className={styles.toolbarGroup}>
          {/* Safe Areas Toggle */}
          <Tooltip content="Toggle safe area guides" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(styles.toolButton, showSafeAreas && styles.toolButtonActive)}
              icon={<Grid24Regular />}
              onClick={() => setShowSafeAreas(!showSafeAreas)}
            />
          </Tooltip>

          {/* Timecode Toggle */}
          <Tooltip content="Toggle timecode overlay" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(styles.toolButton, showTimecode && styles.toolButtonActive)}
              icon={<Timer24Regular />}
              onClick={() => setShowTimecode(!showTimecode)}
            />
          </Tooltip>

          {/* Loop Toggle */}
          <Tooltip content="Toggle loop playback" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(styles.toolButton, loopPlayback && styles.toolButtonActive)}
              icon={<ArrowRepeatAll24Regular />}
              onClick={() => setLoopPlayback(!loopPlayback)}
            />
          </Tooltip>

          {/* Quality Menu */}
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <Tooltip content="Preview quality" relationship="label">
                <Button
                  appearance="subtle"
                  size="small"
                  className={styles.toolButton}
                  icon={<Settings24Regular />}
                />
              </Tooltip>
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => setQuality('full')}>
                  Full Quality {quality === 'full' && '✓'}
                </MenuItem>
                <MenuItem onClick={() => setQuality('half')}>
                  Half Quality {quality === 'half' && '✓'}
                </MenuItem>
                <MenuItem onClick={() => setQuality('quarter')}>
                  Quarter Quality {quality === 'quarter' && '✓'}
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>

          {/* Export Button */}
          <ExportDialog
            trigger={
              <Tooltip content="Export video" relationship="label">
                <Button appearance="primary" size="small" icon={<ArrowExportLtr24Regular />}>
                  Export
                </Button>
              </Tooltip>
            }
          />
        </div>
      </div>

      <div className={styles.previewArea}>
        <div className={styles.canvasWrapper}>
          <div className={styles.canvasGlow} />
          <motion.div
            className={styles.canvas}
            initial={{ scale: 0.98, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ duration: 0.3, ease: 'easeOut' }}
          >
            {isLoading ? (
              <div className={styles.loadingOverlay}>
                <Spinner size="large" />
                <Text size={200} style={{ color: 'rgba(255, 255, 255, 0.7)' }}>
                  Loading preview...
                </Text>
              </div>
            ) : hasContent && videoSrc ? (
              /* eslint-disable-next-line jsx-a11y/media-has-caption */
              <video
                ref={videoRef}
                className={mergeClasses(
                  styles.videoElement,
                  zoom === 'fit' && styles.videoElementFit,
                  zoom === '50' && styles.videoElement50,
                  zoom === '100' && styles.videoElement100,
                  zoom === '200' && styles.videoElement200
                )}
                src={videoSrc}
                loop={loopPlayback}
              />
            ) : (
              <div className={styles.placeholder}>
                <motion.div
                  className={styles.placeholderIcon}
                  initial={{ scale: 0.8 }}
                  animate={{ scale: 1 }}
                  transition={{ duration: 0.4, ease: 'easeOut', delay: 0.1 }}
                >
                  <Video24Regular />
                </motion.div>
                <motion.div
                  initial={{ y: 8, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  transition={{ duration: 0.3, delay: 0.2 }}
                >
                  <Text size={400} className={styles.placeholderText}>
                    Add media to the timeline to preview
                  </Text>
                </motion.div>
              </div>
            )}

            {/* Safe Area Guides */}
            {showSafeAreas && (
              <div className={styles.safeAreaGuides}>
                <div className={styles.safeAreaAction}>
                  <span className={styles.safeAreaLabel} style={{ top: '-16px', left: '4px' }}>
                    Action Safe (95%)
                  </span>
                </div>
                <div className={styles.safeAreaTitle}>
                  <span className={styles.safeAreaLabel} style={{ top: '-16px', left: '4px' }}>
                    Title Safe (90%)
                  </span>
                </div>
              </div>
            )}

            {/* Timecode Overlay */}
            {showTimecode && (
              <div className={styles.timecodeOverlay}>
                {formatTimecode(playbackStore.currentTime)}
              </div>
            )}

            {/* Quality Badge */}
            {quality !== 'full' && <div className={styles.qualityBadge}>{getQualityLabel()}</div>}

            {/* Caption Preview Overlay */}
            <CaptionPreview />

            <div className={styles.aspectRatioLabel}>16:9</div>
          </motion.div>
        </div>
      </div>

      <PlaybackControls onFullscreen={handleFullscreen} />
    </div>
  );
};

export default PreviewPanel;
