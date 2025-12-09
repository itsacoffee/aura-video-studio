/**
 * GlowBorder Component
 * Container with animated glowing border effect
 */

import { makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { ReactNode } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  wrapper: {
    position: 'relative',
    borderRadius: tokens.borderRadiusMedium,
    padding: '2px',
    overflow: 'hidden',
  },
  glowActive: {
    background:
      'linear-gradient(90deg, var(--colorBrandBackground), var(--colorPaletteGreenBackground3), var(--colorBrandBackground))',
    backgroundSize: '200% 100%',
    animationName: {
      '0%': { backgroundPosition: '200% 0' },
      '100%': { backgroundPosition: '-200% 0' },
    },
    animationDuration: '3s',
    animationTimingFunction: 'linear',
    animationIterationCount: 'infinite',
  },
  glowInactive: {
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    padding: '1px',
  },
  content: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: `calc(${tokens.borderRadiusMedium} - 2px)`,
    width: '100%',
    height: '100%',
    position: 'relative',
    zIndex: 1,
  },
});

interface GlowBorderProps {
  children: ReactNode;
  className?: string;
  active?: boolean;
}

export function GlowBorder({ children, className, active = true }: GlowBorderProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();

  const showGlow = active && animationsEnabled && settings.effects.glowEffects;

  return (
    <div
      className={mergeClasses(
        styles.wrapper,
        showGlow ? styles.glowActive : styles.glowInactive,
        className
      )}
    >
      <div className={styles.content}>{children}</div>
    </div>
  );
}
