/**
 * Graphics CSS Provider
 * Dynamically applies CSS custom properties based on graphics settings
 */

import type { GraphicsSettings } from '../types/graphicsSettings';

/**
 * Apply graphics settings to CSS custom properties on :root
 */
export function applyGraphicsSettings(settings: GraphicsSettings): void {
  const root = document.documentElement;

  // Animation durations (0 when disabled)
  const animationEnabled = settings.effects.animations && !settings.accessibility.reducedMotion;
  root.style.setProperty('--duration-micro', animationEnabled ? '100ms' : '0ms');
  root.style.setProperty('--duration-fast', animationEnabled ? '150ms' : '0ms');
  root.style.setProperty('--duration-normal', animationEnabled ? '250ms' : '0ms');
  root.style.setProperty('--duration-slow', animationEnabled ? '400ms' : '0ms');
  root.style.setProperty('--duration-very-slow', animationEnabled ? '600ms' : '0ms');

  // Spring physics (fallback to linear when disabled)
  const springEnabled = settings.effects.springPhysics && animationEnabled;
  root.style.setProperty(
    '--ease-spring',
    springEnabled ? 'cubic-bezier(0.68, -0.55, 0.265, 1.55)' : 'ease-out'
  );
  root.style.setProperty(
    '--ease-bounce',
    springEnabled ? 'cubic-bezier(0.68, -0.6, 0.32, 1.6)' : 'ease-out'
  );

  // Blur effects
  const blurEnabled = settings.effects.blurEffects && settings.gpuAccelerationEnabled;
  root.style.setProperty('--blur-sm', blurEnabled ? '4px' : '0px');
  root.style.setProperty('--blur-md', blurEnabled ? '8px' : '0px');
  root.style.setProperty('--blur-lg', blurEnabled ? '16px' : '0px');
  root.style.setProperty('--blur-xl', blurEnabled ? '24px' : '0px');
  root.style.setProperty('--backdrop-blur', blurEnabled ? 'blur(12px)' : 'none');

  // Shadows
  const shadowsEnabled = settings.effects.shadows;
  root.style.setProperty('--shadow-opacity', shadowsEnabled ? '0.1' : '0');
  root.style.setProperty('--shadow-sm', shadowsEnabled ? '0 1px 2px rgba(0, 0, 0, 0.05)' : 'none');
  root.style.setProperty(
    '--shadow-md',
    shadowsEnabled
      ? '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)'
      : 'none'
  );
  root.style.setProperty(
    '--shadow-lg',
    shadowsEnabled
      ? '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)'
      : 'none'
  );
  root.style.setProperty(
    '--shadow-xl',
    shadowsEnabled
      ? '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)'
      : 'none'
  );

  // Transparency
  const transparencyEnabled = settings.effects.transparency;
  root.style.setProperty('--transparency-panel', transparencyEnabled ? '0.85' : '1');
  root.style.setProperty('--transparency-overlay', transparencyEnabled ? '0.7' : '0.95');
  root.style.setProperty('--transparency-subtle', transparencyEnabled ? '0.95' : '1');

  // Glow effects
  const glowEnabled = settings.effects.glowEffects;
  root.style.setProperty(
    '--glow-focus',
    glowEnabled ? '0 0 0 3px rgba(59, 130, 246, 0.3)' : '0 0 0 2px rgba(59, 130, 246, 0.5)'
  );
  root.style.setProperty('--glow-brand', glowEnabled ? '0 0 20px rgba(59, 130, 246, 0.4)' : 'none');

  // Staggered animations
  root.style.setProperty(
    '--stagger-delay',
    settings.effects.staggeredAnimations && animationEnabled ? '50ms' : '0ms'
  );

  // Smooth scrolling
  root.style.setProperty('--scroll-behavior', settings.effects.smoothScrolling ? 'smooth' : 'auto');

  // DPI Scaling
  const scaleFactor = settings.scaling.mode === 'manual' ? settings.scaling.manualScaleFactor : 1;
  root.style.setProperty('--scale-factor', scaleFactor.toString());
  root.style.setProperty('--base-font-size', `${14 * scaleFactor}px`);

  // Scaled spacing (for DPI awareness)
  root.style.setProperty('--space-1', `${4 * scaleFactor}px`);
  root.style.setProperty('--space-2', `${8 * scaleFactor}px`);
  root.style.setProperty('--space-3', `${12 * scaleFactor}px`);
  root.style.setProperty('--space-4', `${16 * scaleFactor}px`);
  root.style.setProperty('--space-5', `${20 * scaleFactor}px`);
  root.style.setProperty('--space-6', `${24 * scaleFactor}px`);
  root.style.setProperty('--space-8', `${32 * scaleFactor}px`);

  // High contrast mode
  if (settings.accessibility.highContrast) {
    root.style.setProperty('--border-width', '2px');
    root.style.setProperty('--focus-ring-width', '3px');
    root.classList.add('high-contrast');
  } else {
    root.style.setProperty('--border-width', '1px');
    root.style.setProperty('--focus-ring-width', '2px');
    root.classList.remove('high-contrast');
  }

  // GPU acceleration hints
  if (settings.gpuAccelerationEnabled) {
    root.classList.add('gpu-enabled');
  } else {
    root.classList.remove('gpu-enabled');
  }

  // Reduced motion (for CSS queries)
  if (settings.accessibility.reducedMotion) {
    root.classList.add('reduced-motion');
  } else {
    root.classList.remove('reduced-motion');
  }

  console.log('[Graphics] CSS properties applied', {
    animations: animationEnabled,
    blur: blurEnabled,
    shadows: shadowsEnabled,
    scale: scaleFactor,
  });
}

/**
 * Get computed animation duration based on settings
 */
export function getAnimationDuration(
  type: 'micro' | 'fast' | 'normal' | 'slow' = 'normal'
): number {
  const value = getComputedStyle(document.documentElement)
    .getPropertyValue(`--duration-${type}`)
    .trim();
  return parseInt(value) || 0;
}

/**
 * Check if animations are currently enabled
 */
export function areAnimationsEnabled(): boolean {
  const duration = getComputedStyle(document.documentElement)
    .getPropertyValue('--duration-normal')
    .trim();
  return duration !== '0ms';
}
