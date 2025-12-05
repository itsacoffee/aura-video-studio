/**
 * CaptionPreview Component
 *
 * Caption preview overlay on video:
 * - Positioned text with styles applied
 * - Live updates during editing
 */

import { makeStyles, mergeClasses } from '@fluentui/react-components';
import { motion, AnimatePresence } from 'framer-motion';
import { useMemo } from 'react';
import type { FC, CSSProperties } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import type { Caption, CaptionStyle, VerticalPosition } from '../../../types/opencut';

export interface CaptionPreviewProps {
  className?: string;
}

const useStyles = makeStyles({
  root: {
    position: 'absolute',
    inset: 0,
    pointerEvents: 'none',
    display: 'flex',
    flexDirection: 'column',
    zIndex: 10,
  },
  positionTop: {
    justifyContent: 'flex-start',
    paddingTop: '5%',
  },
  positionMiddle: {
    justifyContent: 'center',
  },
  positionBottom: {
    justifyContent: 'flex-end',
    paddingBottom: '5%',
  },
  captionContainer: {
    display: 'flex',
    justifyContent: 'center',
    padding: '0 10%',
  },
  caption: {
    maxWidth: '80%',
    textAlign: 'center',
    wordWrap: 'break-word',
    whiteSpace: 'pre-wrap',
  },
});

function getPositionClass(
  position: VerticalPosition,
  styles: ReturnType<typeof useStyles>
): string {
  switch (position) {
    case 'top':
      return styles.positionTop;
    case 'middle':
      return styles.positionMiddle;
    case 'bottom':
    default:
      return styles.positionBottom;
  }
}

function getCaptionStyles(style: CaptionStyle): CSSProperties {
  const styles: CSSProperties = {
    fontFamily: style.fontFamily,
    fontSize: `${style.fontSize}px`,
    fontWeight: style.fontWeight,
    color: style.color,
    textAlign: style.textAlign,
    letterSpacing: style.letterSpacing ? `${style.letterSpacing}px` : undefined,
    lineHeight: style.lineHeight ?? 1.4,
    padding: style.backgroundPadding ? `${style.backgroundPadding}px` : '8px 16px',
    borderRadius: '4px',
  };

  if (style.backgroundColor) {
    styles.backgroundColor = style.backgroundColor;
  }

  if (style.strokeColor && style.strokeWidth) {
    styles.WebkitTextStroke = `${style.strokeWidth}px ${style.strokeColor}`;
    styles.paintOrder = 'stroke fill';
  }

  if (style.shadow) {
    styles.textShadow = `${style.shadow.offsetX}px ${style.shadow.offsetY}px ${style.shadow.blur}px ${style.shadow.color}`;
  }

  return styles;
}

function getMergedStyle(
  defaultStyle: CaptionStyle,
  overrideStyle?: Partial<CaptionStyle>
): CaptionStyle {
  if (!overrideStyle) return defaultStyle;
  return { ...defaultStyle, ...overrideStyle };
}

export const CaptionPreview: FC<CaptionPreviewProps> = ({ className }) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();

  const { tracks, getVisibleTracks } = captionsStore;
  const currentTime = playbackStore.currentTime;

  // Get active captions at current time from visible tracks
  const activeCaptions = useMemo(() => {
    const visibleTracks = getVisibleTracks();
    const captions: Array<{ caption: Caption; track: (typeof tracks)[0] }> = [];

    visibleTracks.forEach((track) => {
      const activeCaption = track.captions.find(
        (c) => currentTime >= c.startTime && currentTime < c.endTime
      );
      if (activeCaption) {
        captions.push({ caption: activeCaption, track });
      }
    });

    return captions;
  }, [tracks, getVisibleTracks, currentTime]);

  if (activeCaptions.length === 0) {
    return null;
  }

  return (
    <div className={mergeClasses(styles.root, className)}>
      <AnimatePresence>
        {activeCaptions.map(({ caption, track }) => {
          const mergedStyle = getMergedStyle(track.defaultStyle, caption.style);
          const captionStyles = getCaptionStyles(mergedStyle);
          const positionClass = getPositionClass(track.position, styles);

          return (
            <motion.div
              key={caption.id}
              className={mergeClasses(styles.root, positionClass)}
              initial={{ opacity: 0, y: track.position === 'top' ? -10 : 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: track.position === 'top' ? -10 : 10 }}
              transition={{ duration: 0.15 }}
            >
              <div className={styles.captionContainer}>
                <div className={styles.caption} style={captionStyles}>
                  {caption.text}
                </div>
              </div>
            </motion.div>
          );
        })}
      </AnimatePresence>
    </div>
  );
};

export default CaptionPreview;
