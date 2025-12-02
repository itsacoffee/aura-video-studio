/**
 * Graphics Context Provider
 * Provides graphics settings to entire application via React Context
 */

import React, { createContext, useContext, ReactNode } from 'react';
import { useGraphicsSettings } from '../hooks/useGraphicsSettings';
import type { GraphicsSettings, PerformanceProfile } from '../types/graphicsSettings';

interface GraphicsContextType {
  settings: GraphicsSettings;
  loading: boolean;

  // Convenience booleans for common checks
  animationsEnabled: boolean;
  blurEnabled: boolean;
  shadowsEnabled: boolean;
  gpuEnabled: boolean;
  reducedMotion: boolean;

  // Actions
  updateSettings: (updates: Partial<GraphicsSettings>) => Promise<void>;
  applyProfile: (profile: PerformanceProfile) => Promise<void>;
  resetToDefaults: () => Promise<void>;
}

const GraphicsContext = createContext<GraphicsContextType | null>(null);

interface GraphicsProviderProps {
  children: ReactNode;
}

export function GraphicsProvider({ children }: GraphicsProviderProps): React.JSX.Element {
  const graphicsState = useGraphicsSettings();

  return <GraphicsContext.Provider value={graphicsState}>{children}</GraphicsContext.Provider>;
}

/**
 * Hook to access graphics settings from context
 * Must be used within GraphicsProvider
 */
export function useGraphics(): GraphicsContextType {
  const context = useContext(GraphicsContext);
  if (!context) {
    throw new Error('useGraphics must be used within a GraphicsProvider');
  }
  return context;
}

/**
 * Hook for checking if specific effect is enabled
 * Useful for conditional rendering
 */
export function useGraphicsEffect(effect: keyof GraphicsSettings['effects']): boolean {
  const { settings, reducedMotion } = useGraphics();

  // Animation-related effects should be disabled when reduced motion is on
  const animationEffects: Array<keyof GraphicsSettings['effects']> = [
    'animations',
    'springPhysics',
    'parallaxEffects',
    'microInteractions',
    'staggeredAnimations',
  ];

  if (reducedMotion && animationEffects.includes(effect)) {
    return false;
  }

  return settings.effects[effect];
}
