import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { SplashScreen } from './SplashScreen';

describe('SplashScreen', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it('renders with initial loading state', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    expect(screen.getByText('Aura')).toBeInTheDocument();
    expect(screen.getByText('AI Video Generation Suite')).toBeInTheDocument();
    expect(screen.getByText('Starting backend server...')).toBeInTheDocument();
  });

  it('calls onComplete after animation sequence', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    expect(onComplete).not.toHaveBeenCalled();

    // Run through all stages and completion delays
    // Total: 300 + 400 + 350 + 300 + 400 (stages) + 500 (complete delay) + 600 (fadeout)
    vi.runAllTimers();

    expect(onComplete).toHaveBeenCalledTimes(1);
  });

  it('renders progress bar', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    const progressFill = document.querySelector('.splash-progress-fill');
    expect(progressFill).toBeInTheDocument();
  });

  it('renders animated particles canvas', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    const canvas = document.querySelector('.splash-particles-canvas');
    expect(canvas).toBeInTheDocument();
  });

  it('renders logo with white text', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    const title = screen.getByText('Aura');
    expect(title).toHaveClass('splash-title');
  });

  it('renders all UI elements', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    // Check logo elements
    expect(document.querySelector('.splash-logo')).toBeInTheDocument();
    expect(document.querySelector('.splash-logo-icon')).toBeInTheDocument();

    // Check progress elements
    expect(document.querySelector('.splash-progress')).toBeInTheDocument();
    expect(document.querySelector('.splash-progress-track')).toBeInTheDocument();
    expect(document.querySelector('.splash-progress-fill')).toBeInTheDocument();
  });
});
