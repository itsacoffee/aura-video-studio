/**
 * ElevatedCard Component
 * Card with dynamic shadow depth that responds to hover
 */

import { makeStyles, tokens, mergeClasses } from '@fluentui/react-components';
import { motion } from 'framer-motion';
import { ReactNode, KeyboardEvent } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  card: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    border: `var(--border-width) solid ${tokens.colorNeutralStroke1}`,
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition:
      'box-shadow var(--duration-normal) var(--ease-standard), transform var(--duration-normal) var(--ease-spring)',
  },
  resting: {
    boxShadow: 'var(--shadow-sm)',
  },
  elevated: {
    boxShadow: 'var(--shadow-lg)',
  },
});

interface ElevatedCardProps {
  children: ReactNode;
  className?: string;
  onClick?: () => void;
  elevation?: 'low' | 'medium' | 'high';
}

export function ElevatedCard({
  children,
  className,
  onClick,
  elevation = 'medium',
}: ElevatedCardProps) {
  const styles = useStyles();
  const { animationsEnabled, shadowsEnabled, settings } = useGraphics();

  const elevationScale = {
    low: { hover: 1.01, shadow: 'var(--shadow-md)' },
    medium: { hover: 1.02, shadow: 'var(--shadow-lg)' },
    high: { hover: 1.03, shadow: 'var(--shadow-xl)' },
  }[elevation];

  const handleKeyDown = (e: KeyboardEvent) => {
    if (onClick && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault();
      onClick();
    }
  };

  if (!animationsEnabled) {
    return (
      <div
        role="button"
        tabIndex={onClick ? 0 : undefined}
        className={mergeClasses(styles.card, styles.resting, className)}
        onClick={onClick}
        onKeyDown={handleKeyDown}
      >
        {children}
      </div>
    );
  }

  return (
    <motion.div
      role="button"
      tabIndex={onClick ? 0 : undefined}
      className={mergeClasses(styles.card, className)}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      initial={{ scale: 1, boxShadow: 'var(--shadow-sm)' }}
      whileHover={{
        scale: settings.effects.springPhysics ? elevationScale.hover : 1,
        boxShadow: shadowsEnabled ? elevationScale.shadow : 'none',
        y: -4,
      }}
      whileTap={{ scale: 0.98 }}
      transition={{
        type: settings.effects.springPhysics ? 'spring' : 'tween',
        stiffness: 400,
        damping: 25,
      }}
    >
      {children}
    </motion.div>
  );
}
