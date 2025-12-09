/**
 * AcrylicPanel Component
 * Windows 11-style acrylic blur effect with noise texture
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { ReactNode } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  panel: {
    position: 'relative',
    borderRadius: tokens.borderRadiusMedium,
    overflow: 'hidden',
  },
  backdrop: {
    position: 'absolute',
    inset: '0',
    backdropFilter: 'var(--backdrop-blur)',
    WebkitBackdropFilter: 'var(--backdrop-blur)',
    backgroundColor: 'rgba(255, 255, 255, var(--transparency-panel))',
  },
  backdropDark: {
    backgroundColor: 'rgba(32, 32, 32, var(--transparency-panel))',
  },
  noise: {
    position: 'absolute',
    inset: '0',
    opacity: 0.02,
    pointerEvents: 'none',
    backgroundImage: `url("data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='noise'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23noise)'/%3E%3C/svg%3E")`,
  },
  content: {
    position: 'relative',
    zIndex: 1,
  },
  border: {
    position: 'absolute',
    inset: '0',
    borderRadius: 'inherit',
    border: '1px solid rgba(255, 255, 255, 0.1)',
    pointerEvents: 'none',
  },
  solid: {
    backgroundColor: tokens.colorNeutralBackground1,
  },
});

interface AcrylicPanelProps {
  children: ReactNode;
  className?: string;
  dark?: boolean;
  intensity?: 'subtle' | 'medium' | 'strong';
  padding?: string;
}

export function AcrylicPanel({
  children,
  className,
  dark = false,
  intensity = 'medium',
  padding = tokens.spacingVerticalL,
}: AcrylicPanelProps) {
  const styles = useStyles();
  const { blurEnabled, settings } = useGraphics();

  const blurIntensity = {
    subtle: '8px',
    medium: '12px',
    strong: '20px',
  }[intensity];

  if (!blurEnabled) {
    return (
      <div className={mergeClasses(styles.panel, styles.solid, className)} style={{ padding }}>
        {children}
      </div>
    );
  }

  return (
    <div className={mergeClasses(styles.panel, className)}>
      <div
        className={mergeClasses(styles.backdrop, dark && styles.backdropDark)}
        style={{ backdropFilter: `blur(${blurIntensity})` }}
      />
      {settings.effects.microInteractions && <div className={styles.noise} />}
      <div className={styles.border} />
      <div className={styles.content} style={{ padding }}>
        {children}
      </div>
    </div>
  );
}
