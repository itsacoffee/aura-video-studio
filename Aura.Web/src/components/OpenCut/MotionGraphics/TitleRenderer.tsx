/**
 * TitleRenderer Component
 *
 * Specialized renderer for title graphics including
 * cinematic reveals, glitch effects, and elegant animations.
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

export interface TitleRendererProps {
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
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    textAlign: 'center',
    gap: '16px',
  },
  cinematic: {
    position: 'relative',
  },
  cinematicTitle: {
    fontSize: '64px',
    fontWeight: 700,
    color: '#FFFFFF',
    textTransform: 'uppercase',
    letterSpacing: '8px',
    margin: 0,
    textShadow: '0 4px 24px rgba(0, 0, 0, 0.5)',
  },
  cinematicSubtitle: {
    fontSize: '20px',
    fontWeight: 300,
    color: 'rgba(255, 255, 255, 0.85)',
    letterSpacing: '4px',
    margin: 0,
  },
  lensFlare: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: '200%',
    height: '4px',
    background: 'linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.8), transparent)',
    opacity: 0.6,
    pointerEvents: 'none',
  },
  glitch: {
    position: 'relative',
  },
  glitchTitle: {
    fontSize: '72px',
    fontWeight: 900,
    color: '#FFFFFF',
    textTransform: 'uppercase',
    letterSpacing: '4px',
    margin: 0,
    position: 'relative',
  },
  glitchLayer: {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    pointerEvents: 'none',
  },
  elegant: {
    position: 'relative',
  },
  elegantTitle: {
    fontSize: '48px',
    fontWeight: 400,
    color: '#FFFFFF',
    fontFamily: 'Georgia, serif',
    letterSpacing: '2px',
    margin: 0,
  },
  elegantDivider: {
    width: '100px',
    height: '1px',
    backgroundColor: 'rgba(255, 255, 255, 0.4)',
    margin: '12px 0',
  },
  elegantSubtitle: {
    fontSize: '18px',
    fontWeight: 300,
    color: 'rgba(255, 255, 255, 0.9)',
    fontFamily: 'Georgia, serif',
    letterSpacing: '3px',
    margin: 0,
  },
  particle: {
    position: 'absolute',
    width: '4px',
    height: '4px',
    borderRadius: '50%',
    backgroundColor: 'rgba(255, 255, 255, 0.6)',
    pointerEvents: 'none',
  },
});

export const TitleRenderer: FC<TitleRendererProps> = ({ asset, graphic, animationState }) => {
  const styles = useStyles();

  // Get all customized values at the top level
  const mainTitle = String(
    graphic.customValues['mainTitle'] ?? graphic.customValues['title'] ?? 'TITLE'
  );
  const subtitle = String(graphic.customValues['subtitle'] ?? '');
  const textColor = String(graphic.customValues['textColor'] ?? '#FFFFFF');
  const showLensFlare = graphic.customValues['showLensFlare'] !== false;
  const flareIntensity = Number(graphic.customValues['flareIntensity'] ?? 0.7);
  const glitchIntensity = Number(graphic.customValues['glitchIntensity'] ?? 0.5);
  const rgbSplit = Number(graphic.customValues['rgbSplit'] ?? 5);
  const showParticles = graphic.customValues['showParticles'] !== false;

  // Calculate animation at top level
  const eased = evaluateEasing('easeOutCubic', animationState.progress);

  // Base animated style - always called
  const baseAnimatedStyle = useMemo((): React.CSSProperties => {
    if (animationState.phase === 'entry') {
      return { opacity: eased };
    }
    if (animationState.phase === 'exit') {
      return { opacity: animationState.progress };
    }
    return { opacity: 1 };
  }, [animationState, eased]);

  // Cinematic title style - always called
  const cinematicTitleStyle = useMemo((): React.CSSProperties => {
    if (animationState.phase === 'entry') {
      return {
        transform: `scale(${0.95 + eased * 0.05})`,
        opacity: eased,
        filter: `blur(${(1 - eased) * 4}px)`,
      };
    }
    if (animationState.phase === 'exit') {
      return {
        transform: `scale(${0.95 + animationState.progress * 0.05})`,
        opacity: animationState.progress,
      };
    }
    return { transform: 'scale(1)', opacity: 1 };
  }, [animationState, eased]);

  // Elegant title style - always called
  const elegantTitleStyle = useMemo((): React.CSSProperties => {
    if (animationState.phase === 'entry') {
      return {
        transform: `translateY(${(1 - eased) * 20}px)`,
        opacity: eased,
      };
    }
    if (animationState.phase === 'exit') {
      return {
        transform: `translateY(${(1 - animationState.progress) * 20}px)`,
        opacity: animationState.progress,
      };
    }
    return {};
  }, [animationState, eased]);

  // Generate floating particles - always called
  const particles = useMemo(() => {
    if (!showParticles) return [];
    const result = [];
    for (let i = 0; i < 8; i++) {
      result.push({
        x: Math.random() * 200 - 100,
        y: Math.random() * 100 - 50,
        size: 2 + Math.random() * 4,
        delay: i * 0.1,
      });
    }
    return result;
  }, [showParticles]);

  // Calculate glitch offset
  const glitchOffset = animationState.phase === 'hold' ? rgbSplit * glitchIntensity : 0;

  // Cinematic Reveal
  if (asset.id.includes('cinematic-reveal')) {
    return (
      <div className={`${styles.container} ${styles.cinematic}`}>
        {showLensFlare && animationState.phase === 'entry' && (
          <div className={styles.lensFlare} style={{ opacity: eased * flareIntensity }} />
        )}
        <h1 className={styles.cinematicTitle} style={{ ...cinematicTitleStyle, color: textColor }}>
          {mainTitle}
        </h1>
        {subtitle && (
          <p
            className={styles.cinematicSubtitle}
            style={{
              ...baseAnimatedStyle,
              color: `${textColor}DD`,
              transitionDelay: '0.2s',
            }}
          >
            {subtitle}
          </p>
        )}
      </div>
    );
  }

  // Glitch Distort
  if (asset.id.includes('glitch')) {
    return (
      <div className={`${styles.container} ${styles.glitch}`} style={baseAnimatedStyle}>
        <h1
          className={styles.glitchTitle}
          style={{
            color: textColor,
            position: 'relative',
          }}
        >
          {/* Red layer */}
          <span
            className={styles.glitchLayer}
            style={{
              color: '#FF0000',
              left: -glitchOffset,
              opacity: 0.8,
              mixBlendMode: 'screen',
            }}
          >
            {mainTitle}
          </span>
          {/* Cyan layer */}
          <span
            className={styles.glitchLayer}
            style={{
              color: '#00FFFF',
              left: glitchOffset,
              opacity: 0.8,
              mixBlendMode: 'screen',
            }}
          >
            {mainTitle}
          </span>
          {/* Main text */}
          {mainTitle}
        </h1>
      </div>
    );
  }

  // Elegant Serif
  if (asset.id.includes('elegant')) {
    return (
      <div className={`${styles.container} ${styles.elegant}`}>
        {/* Particles */}
        {particles.map((p, i) => (
          <div
            key={i}
            className={styles.particle}
            style={{
              left: `calc(50% + ${p.x}px)`,
              top: `calc(50% + ${p.y}px)`,
              width: p.size,
              height: p.size,
              opacity: eased * 0.5,
            }}
          />
        ))}

        <h1 className={styles.elegantTitle} style={{ ...elegantTitleStyle, color: textColor }}>
          {mainTitle}
        </h1>

        <div
          className={styles.elegantDivider}
          style={{
            transform: `scaleX(${eased})`,
            opacity: eased,
          }}
        />

        {subtitle && (
          <p
            className={styles.elegantSubtitle}
            style={{
              ...elegantTitleStyle,
              color: `${textColor}EE`,
              transitionDelay: '0.15s',
            }}
          >
            {subtitle}
          </p>
        )}
      </div>
    );
  }

  // Default title rendering
  return (
    <div className={styles.container} style={baseAnimatedStyle}>
      <h1
        style={{
          fontSize: '48px',
          fontWeight: 700,
          color: textColor,
          margin: 0,
        }}
      >
        {mainTitle}
      </h1>
      {subtitle && (
        <p
          style={{
            fontSize: '18px',
            color: `${textColor}CC`,
            margin: 0,
          }}
        >
          {subtitle}
        </p>
      )}
    </div>
  );
};

export default TitleRenderer;
