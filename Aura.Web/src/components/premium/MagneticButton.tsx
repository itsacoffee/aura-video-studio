/**
 * MagneticButton Component
 * Button that subtly follows cursor position (Apple-style)
 */

import { Button, type ButtonProps, makeStyles, mergeClasses } from '@fluentui/react-components';
import { motion, useMotionValue, useSpring } from 'framer-motion';
import { useRef, MouseEvent, ReactNode } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  wrapper: {
    display: 'inline-block',
  },
  button: {
    transition: 'box-shadow var(--duration-fast) var(--ease-standard)',
  },
  glow: {
    ':hover': {
      boxShadow: 'var(--glow-brand)',
    },
  },
});

interface MagneticButtonProps {
  children?: ReactNode;
  className?: string;
  magneticStrength?: number;
  enableGlow?: boolean;
  appearance?: ButtonProps['appearance'];
  size?: ButtonProps['size'];
  disabled?: boolean;
  onClick?: () => void;
}

export function MagneticButton({
  children,
  className,
  magneticStrength = 0.3,
  enableGlow = true,
  appearance,
  size,
  disabled,
  onClick,
}: MagneticButtonProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();
  const ref = useRef<HTMLDivElement>(null);

  const x = useMotionValue(0);
  const y = useMotionValue(0);

  const springConfig = { stiffness: 400, damping: 30 };
  const xSpring = useSpring(x, springConfig);
  const ySpring = useSpring(y, springConfig);

  if (!animationsEnabled || !settings.effects.microInteractions) {
    return (
      <Button
        className={mergeClasses(styles.button, className)}
        appearance={appearance}
        size={size}
        disabled={disabled}
        onClick={onClick}
      >
        {children}
      </Button>
    );
  }

  const handleMouseMove = (e: MouseEvent) => {
    if (!ref.current) return;

    const rect = ref.current.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const deltaX = (e.clientX - centerX) * magneticStrength;
    const deltaY = (e.clientY - centerY) * magneticStrength;

    x.set(deltaX);
    y.set(deltaY);
  };

  const handleMouseLeave = () => {
    x.set(0);
    y.set(0);
  };

  return (
    <motion.div
      ref={ref}
      className={styles.wrapper}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      style={{ x: xSpring, y: ySpring }}
    >
      <Button
        className={mergeClasses(
          styles.button,
          enableGlow && settings.effects.glowEffects && styles.glow,
          className
        )}
        appearance={appearance}
        size={size}
        disabled={disabled}
        onClick={onClick}
      >
        {children}
      </Button>
    </motion.div>
  );
}
