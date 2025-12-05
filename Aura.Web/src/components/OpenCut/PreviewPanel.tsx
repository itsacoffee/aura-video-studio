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
import { useCallback, useRef, useState } from 'react';
import type { FC } from 'react';
import { openCutTokens } from '../../styles/designTokens';
import { ExportDialog } from './Export';
import { PlaybackControls } from './PlaybackControls';

export interface PreviewPanelProps {
  className?: string;
  isLoading?: boolean;
  hasContent?: boolean;
  videoSrc?: string;
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
    padding: `${openCutTokens.spacing.xs} ${openCutTokens.spacing.sm}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground2,
    minHeight: '36px',
  },
  toolbarGroup: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  toolButton: {
    minWidth: '32px',
    minHeight: '32px',
  },
  toolButtonActive: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    color: tokens.colorBrandForeground1,
  },
  zoomText: {
    minWidth: '48px',
    textAlign: 'center',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
  },
  previewArea: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingHorizontalM,
    backgroundColor: tokens.colorNeutralBackground3,
    position: 'relative',
    overflow: 'hidden',
    minHeight: 0,
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
  },
  canvas: {
    width: '100%',
    height: '100%',
    maxWidth: '100%',
    maxHeight: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: '#000000',
    borderRadius: tokens.borderRadiusMedium,
    boxShadow: tokens.shadow16,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    position: 'relative',
    transition: 'box-shadow 300ms ease-out',
    ':hover': {
      boxShadow: tokens.shadow28,
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
    borderRadius: '4px',
  },
  safeAreaAction: {
    position: 'absolute',
    top: '5%',
    left: '5%',
    right: '5%',
    bottom: '5%',
    border: '1px dashed rgba(255, 255, 255, 0.25)',
    borderRadius: '4px',
  },
  safeAreaLabel: {
    position: 'absolute',
    fontSize: '10px',
    color: 'rgba(255, 255, 255, 0.5)',
    padding: '2px 4px',
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    borderRadius: '2px',
  },
  videoElement: {
    width: '100%',
    height: '100%',
    objectFit: 'contain',
    borderRadius: tokens.borderRadiusLarge,
  },
  placeholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground4,
    textAlign: 'center',
    zIndex: 1,
  },
  placeholderIcon: {
    width: '80px',
    height: '80px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    color: 'rgba(255, 255, 255, 0.3)',
    '& svg': {
      width: '40px',
      height: '40px',
    },
  },
  placeholderText: {
    color: 'rgba(255, 255, 255, 0.5)',
  },
  loadingOverlay: {
    position: 'absolute',
    inset: 0,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    borderRadius: tokens.borderRadiusLarge,
    gap: tokens.spacingVerticalM,
    zIndex: 2,
  },
  timecodeOverlay: {
    position: 'absolute',
    top: tokens.spacingVerticalM,
    left: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    color: 'white',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase200,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    zIndex: 3,
  },
  aspectRatioLabel: {
    position: 'absolute',
    bottom: tokens.spacingVerticalM,
    right: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: 'rgba(255, 255, 255, 0.8)',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    zIndex: 1,
  },
  qualityBadge: {
    position: 'absolute',
    top: tokens.spacingVerticalM,
    right: tokens.spacingHorizontalM,
    padding: `${tokens.spacingVerticalXXS} ${tokens.spacingHorizontalS}`,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    color: 'rgba(255, 255, 255, 0.8)',
    borderRadius: tokens.borderRadiusSmall,
    fontSize: tokens.fontSizeBase100,
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

export const PreviewPanel: FC<PreviewPanelProps> = ({
  className,
  isLoading = false,
  hasContent = false,
  videoSrc,
}) => {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);

  const [showSafeAreas, setShowSafeAreas] = useState(false);
  const [showTimecode, setShowTimecode] = useState(false);
  const [loopPlayback, setLoopPlayback] = useState(false);
  const [zoom, setZoom] = useState<ZoomLevel>('fit');
  const [quality, setQuality] = useState<PreviewQuality>('full');
  const [currentTime] = useState(0);

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
              <video className={styles.videoElement} src={videoSrc} loop={loopPlayback} />
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
              <div className={styles.timecodeOverlay}>{formatTimecode(currentTime)}</div>
            )}

            {/* Quality Badge */}
            {quality !== 'full' && <div className={styles.qualityBadge}>{getQualityLabel()}</div>}

            <div className={styles.aspectRatioLabel}>16:9</div>
          </motion.div>
        </div>
      </div>

      <PlaybackControls onFullscreen={handleFullscreen} />
    </div>
  );
};

export default PreviewPanel;
