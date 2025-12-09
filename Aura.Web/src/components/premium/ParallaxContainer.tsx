/**
 * ParallaxContainer Component
 * Creates depth effect on scroll or mouse movement
 */

import { makeStyles } from '@fluentui/react-components';
import { motion, useScroll, useTransform, useMotionValue, useSpring } from 'framer-motion';
import { ReactNode, useRef, MouseEvent } from 'react';
import { useGraphics } from '../../contexts/GraphicsContext';

const useStyles = makeStyles({
  container: {
    position: 'relative',
    overflow: 'hidden',
  },
  layer: {
    position: 'absolute',
    inset: '0',
  },
});

interface ParallaxContainerProps {
  children: ReactNode;
  className?: string;
  depth?: number;
  type?: 'scroll' | 'mouse';
}

export function ParallaxContainer({
  children,
  className,
  depth = 0.2,
  type = 'scroll',
}: ParallaxContainerProps) {
  const styles = useStyles();
  const { animationsEnabled, settings } = useGraphics();
  const ref = useRef<HTMLDivElement>(null);

  const { scrollYProgress } = useScroll({
    target: ref,
    offset: ['start end', 'end start'],
  });

  const scrollY = useTransform(scrollYProgress, [0, 1], [-50 * depth, 50 * depth]);

  const mouseX = useMotionValue(0);
  const mouseY = useMotionValue(0);
  const springConfig = { stiffness: 100, damping: 20 };
  const mouseXSpring = useSpring(mouseX, springConfig);
  const mouseYSpring = useSpring(mouseY, springConfig);

  if (!animationsEnabled || !settings.effects.parallaxEffects) {
    return <div className={className}>{children}</div>;
  }

  const handleMouseMove = (e: MouseEvent) => {
    if (!ref.current || type !== 'mouse') return;

    const rect = ref.current.getBoundingClientRect();
    const centerX = rect.width / 2;
    const centerY = rect.height / 2;

    const deltaX = (e.clientX - rect.left - centerX) * depth * 0.1;
    const deltaY = (e.clientY - rect.top - centerY) * depth * 0.1;

    mouseX.set(deltaX);
    mouseY.set(deltaY);
  };

  const handleMouseLeave = () => {
    mouseX.set(0);
    mouseY.set(0);
  };

  return (
    <motion.div
      ref={ref}
      className={`${styles.container} ${className ?? ''}`}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      style={{
        y: type === 'scroll' ? scrollY : mouseYSpring,
        x: type === 'mouse' ? mouseXSpring : 0,
      }}
    >
      {children}
    </motion.div>
  );
}
