/**
 * PreviewPanel Component
 *
 * Enhanced preview area with subtle shadows, proper aspect ratio handling,
 * and elegant loading states following Apple HIG.
 */

import { makeStyles, tokens, Text, Spinner, mergeClasses } from '@fluentui/react-components';
import { Video24Regular } from '@fluentui/react-icons';
import { motion } from 'framer-motion';
import { useCallback, useRef } from 'react';
import type { FC } from 'react';
import { PlaybackControls } from './PlaybackControls';

export interface PreviewPanelProps {
  className?: string;
  isLoading?: boolean;
  hasContent?: boolean;
  videoSrc?: string;
}

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    flex: 1,
    minWidth: 0,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  previewArea: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingHorizontalXXL,
    backgroundColor: tokens.colorNeutralBackground3,
    position: 'relative',
    overflow: 'hidden',
  },
  canvasWrapper: {
    position: 'relative',
    maxWidth: '100%',
    maxHeight: '100%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  canvas: {
    maxWidth: '100%',
    maxHeight: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: '#000000',
    borderRadius: tokens.borderRadiusLarge,
    boxShadow: tokens.shadow16,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    position: 'relative',
    minHeight: '280px',
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
});

export const PreviewPanel: FC<PreviewPanelProps> = ({
  className,
  isLoading = false,
  hasContent = false,
  videoSrc,
}) => {
  const styles = useStyles();
  const containerRef = useRef<HTMLDivElement>(null);

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

  return (
    <div ref={containerRef} className={mergeClasses(styles.container, className)}>
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
              <video className={styles.videoElement} src={videoSrc} />
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
            <div className={styles.aspectRatioLabel}>16:9</div>
          </motion.div>
        </div>
      </div>

      <PlaybackControls onFullscreen={handleFullscreen} />
    </div>
  );
};

export default PreviewPanel;
