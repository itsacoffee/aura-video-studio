/**
 * CalloutRenderer Component
 *
 * Specialized renderer for callout graphics including
 * arrows, pointers, circles, and tooltip animations.
 */

import { makeStyles } from '@fluentui/react-components';
import { useMemo } from 'react';
import type { FC } from 'react';
import type {
  MotionGraphicAsset,
  AppliedGraphic,
  AnimationState,
} from '../../../types/motionGraphics';
import { evaluateEasing } from '../../../utils/motionGraphicsAnimation';

export interface CalloutRendererProps {
  /** The motion graphic asset definition */
  asset: MotionGraphicAsset;
  /** The applied graphic instance with customizations */
  graphic: AppliedGraphic;
  /** Current animation state */
  animationState: AnimationState;
  /** Canvas width */
  width: number;
  /** Canvas height */
  height: number;
}

const useStyles = makeStyles({
  container: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  circle: {
    position: 'absolute',
    borderRadius: '50%',
    border: '3px solid',
    boxSizing: 'border-box',
  },
  arrow: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
  },
  arrowLine: {
    height: '3px',
    borderRadius: '2px',
  },
  arrowHead: {
    width: 0,
    height: 0,
    borderTop: '8px solid transparent',
    borderBottom: '8px solid transparent',
    borderLeft: '12px solid',
  },
  arrowLabel: {
    fontSize: '16px',
    fontWeight: 500,
    color: '#FFFFFF',
    whiteSpace: 'nowrap',
  },
  tooltip: {
    backgroundColor: 'rgba(0, 0, 0, 0.9)',
    color: '#FFFFFF',
    padding: '12px 16px',
    borderRadius: '8px',
    fontSize: '14px',
    maxWidth: '250px',
    position: 'relative',
  },
  tooltipPointer: {
    position: 'absolute',
    bottom: '-8px',
    left: '50%',
    transform: 'translateX(-50%)',
    width: 0,
    height: 0,
    borderLeft: '8px solid transparent',
    borderRight: '8px solid transparent',
    borderTop: '8px solid rgba(0, 0, 0, 0.9)',
  },
});

export const CalloutRenderer: FC<CalloutRendererProps> = ({ asset, graphic, animationState }) => {
  const styles = useStyles();

  // Get customized values
  const ringColor = String(
    graphic.customValues['ringColor'] ?? graphic.customValues['arrowColor'] ?? '#EF4444'
  );
  const labelText = String(graphic.customValues['label'] ?? graphic.customValues['text'] ?? '');
  const size = Number(graphic.customValues['size'] ?? 80);
  const showLabel = graphic.customValues['showLabel'] !== false;

  // Calculate animation
  const eased = evaluateEasing('easeOutCubic', animationState.progress);

  const animatedStyle = useMemo((): React.CSSProperties => {
    if (animationState.phase === 'entry') {
      return {
        transform: `scale(${0.5 + eased * 0.5})`,
        opacity: eased,
      };
    }
    if (animationState.phase === 'exit') {
      return {
        transform: `scale(${0.5 + animationState.progress * 0.5})`,
        opacity: animationState.progress,
      };
    }
    return {
      transform: 'scale(1)',
      opacity: 1,
    };
  }, [animationState, eased]);

  // Focus Circle
  if (asset.id.includes('focus-circle')) {
    const pulseScale = 1 + Math.sin(Date.now() / 500) * 0.05; // Subtle pulse
    return (
      <div className={styles.container} style={animatedStyle}>
        {/* Outer ring */}
        <div
          className={styles.circle}
          style={{
            width: size,
            height: size,
            borderColor: ringColor,
            opacity: 0.5,
            transform: `scale(${pulseScale * 1.2})`,
          }}
        />
        {/* Inner ring */}
        <div
          className={styles.circle}
          style={{
            width: size * 0.75,
            height: size * 0.75,
            borderColor: ringColor,
          }}
        />
      </div>
    );
  }

  // Arrow Pointer
  if (asset.id.includes('arrow')) {
    return (
      <div className={styles.arrow} style={animatedStyle}>
        {showLabel && labelText && <span className={styles.arrowLabel}>{labelText}</span>}
        <div
          className={styles.arrowLine}
          style={{
            width: 60,
            backgroundColor: ringColor,
          }}
        />
        <div
          className={styles.arrowHead}
          style={{
            borderLeftColor: ringColor,
          }}
        />
      </div>
    );
  }

  // Tooltip Box
  if (asset.id.includes('tooltip')) {
    const bgColor = String(graphic.customValues['bgColor'] ?? 'rgba(0, 0, 0, 0.9)');
    return (
      <div
        className={styles.tooltip}
        style={{
          ...animatedStyle,
          backgroundColor: bgColor,
        }}
      >
        {labelText || 'This is an important feature'}
        <div className={styles.tooltipPointer} style={{ borderTopColor: bgColor }} />
      </div>
    );
  }

  // Default fallback
  return (
    <div className={styles.container} style={animatedStyle}>
      <div
        className={styles.circle}
        style={{
          width: size,
          height: size,
          borderColor: ringColor,
        }}
      />
    </div>
  );
};

export default CalloutRenderer;
