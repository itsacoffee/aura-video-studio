/**
 * Panel Animation System
 *
 * Spring-based animation system for smooth panel transitions, similar to
 * Adobe Premiere Pro's workspace management. Provides fluid panel collapse,
 * expand, resize, and swap animations.
 */

import { useEffect, useRef, useState, useCallback } from 'react';

interface SpringConfig {
  stiffness: number;
  damping: number;
  mass: number;
  precision: number;
}

interface SpringState {
  current: number;
  target: number;
  velocity: number;
}

const DEFAULT_CONFIG: SpringConfig = {
  stiffness: 170,
  damping: 26,
  mass: 1,
  precision: 0.01,
};

const PRESETS: Record<string, SpringConfig> = {
  gentle: {
    stiffness: 120,
    damping: 14,
    mass: 1,
    precision: 0.01,
  },
  wobbly: {
    stiffness: 180,
    damping: 12,
    mass: 1,
    precision: 0.01,
  },
  stiff: {
    stiffness: 210,
    damping: 20,
    mass: 1,
    precision: 0.01,
  },
  slow: {
    stiffness: 280,
    damping: 60,
    mass: 1,
    precision: 0.01,
  },
  molasses: {
    stiffness: 280,
    damping: 120,
    mass: 1,
    precision: 0.01,
  },
};

function springPhysics(state: SpringState, config: SpringConfig, deltaTime: number): SpringState {
  const { stiffness, damping, mass } = config;
  const { current, target, velocity } = state;

  const displacement = current - target;
  const springForce = -stiffness * displacement;
  const dampingForce = -damping * velocity;
  const acceleration = (springForce + dampingForce) / mass;

  const newVelocity = velocity + acceleration * deltaTime;
  const newValue = current + newVelocity * deltaTime;

  return {
    current: newValue,
    target,
    velocity: newVelocity,
  };
}

export function useSpring(
  target: number,
  configOrPreset: SpringConfig | keyof typeof PRESETS = DEFAULT_CONFIG
): [number, boolean] {
  const config = typeof configOrPreset === 'string' ? PRESETS[configOrPreset] : configOrPreset;

  const [state, setState] = useState<SpringState>({
    current: target,
    target,
    velocity: 0,
  });

  const [isAnimating, setIsAnimating] = useState(false);
  const rafRef = useRef<number>();
  const lastTimeRef = useRef<number>();

  const animate = useCallback(
    (timestamp: number) => {
      if (!lastTimeRef.current) {
        lastTimeRef.current = timestamp;
      }

      const deltaTime = Math.min((timestamp - lastTimeRef.current) / 1000, 0.064);
      lastTimeRef.current = timestamp;

      setState((prevState) => {
        const newState = springPhysics(prevState, config, deltaTime);

        const isAtRest =
          Math.abs(newState.velocity) < config.precision &&
          Math.abs(newState.current - newState.target) < config.precision;

        if (isAtRest) {
          setIsAnimating(false);
          return {
            current: newState.target,
            target: newState.target,
            velocity: 0,
          };
        }

        rafRef.current = requestAnimationFrame(animate);
        return newState;
      });
    },
    [config]
  );

  useEffect(() => {
    setState((prevState) => ({
      ...prevState,
      target,
    }));

    setIsAnimating(true);
    lastTimeRef.current = undefined;

    if (rafRef.current) {
      cancelAnimationFrame(rafRef.current);
    }

    rafRef.current = requestAnimationFrame(animate);

    return () => {
      if (rafRef.current) {
        cancelAnimationFrame(rafRef.current);
      }
    };
  }, [target, animate]);

  return [state.current, isAnimating];
}

export interface PanelAnimationConfig {
  duration?: number;
  easing?: string;
  preset?: keyof typeof PRESETS;
}

export function usePanelAnimation(
  isVisible: boolean,
  config: PanelAnimationConfig = {}
): {
  opacity: number;
  transform: string;
  width: string;
  isAnimating: boolean;
} {
  const preset = config.preset || 'gentle';
  const [opacity] = useSpring(isVisible ? 1 : 0, preset);
  const [scale] = useSpring(isVisible ? 1 : 0.95, preset);
  const [widthMultiplier, isAnimating] = useSpring(isVisible ? 1 : 0, preset);

  return {
    opacity: Math.max(0, Math.min(1, opacity)),
    transform: `scale(${Math.max(0, Math.min(1, scale))})`,
    width: `${Math.max(0, Math.min(100, widthMultiplier * 100))}%`,
    isAnimating,
  };
}

export function usePanelResize(
  targetWidth: number,
  config: PanelAnimationConfig = {}
): [number, boolean] {
  const preset = config.preset || 'stiff';
  return useSpring(targetWidth, preset);
}

export function usePanelCollapse(
  isCollapsed: boolean,
  expandedWidth: number,
  collapsedWidth: number = 48,
  config: PanelAnimationConfig = {}
): [number, boolean] {
  const preset = config.preset || 'stiff';
  const targetWidth = isCollapsed ? collapsedWidth : expandedWidth;
  return useSpring(targetWidth, preset);
}

export const panelTransitions = {
  collapse: 'width 250ms cubic-bezier(0.4, 0, 0.2, 1)',
  expand: 'width 250ms cubic-bezier(0.4, 0, 0.2, 1)',
  fade: 'opacity 250ms cubic-bezier(0.4, 0, 0.2, 1)',
  slideIn: 'transform 250ms cubic-bezier(0.4, 0, 0.2, 1)',
  slideOut: 'transform 250ms cubic-bezier(0.4, 0, 0.2, 1)',
  swap: 'all 350ms cubic-bezier(0.4, 0, 0.2, 1)',
};

export function getPanelTransition(type: keyof typeof panelTransitions): string {
  return panelTransitions[type];
}

export interface PanelSwapAnimationState {
  isSwapping: boolean;
  phase: 'idle' | 'fadeOut' | 'swap' | 'fadeIn';
  progress: number;
}

export function usePanelSwap(): [PanelSwapAnimationState, (callback: () => void) => void] {
  const [state, setState] = useState<PanelSwapAnimationState>({
    isSwapping: false,
    phase: 'idle',
    progress: 0,
  });

  const swap = useCallback((callback: () => void) => {
    setState({ isSwapping: true, phase: 'fadeOut', progress: 0 });

    setTimeout(() => {
      setState({ isSwapping: true, phase: 'swap', progress: 0.33 });
      callback();
    }, 150);

    setTimeout(() => {
      setState({ isSwapping: true, phase: 'fadeIn', progress: 0.66 });
    }, 200);

    setTimeout(() => {
      setState({ isSwapping: false, phase: 'idle', progress: 1 });
    }, 350);
  }, []);

  return [state, swap];
}

export const springPresets = PRESETS;

export type SpringPreset = keyof typeof PRESETS;
