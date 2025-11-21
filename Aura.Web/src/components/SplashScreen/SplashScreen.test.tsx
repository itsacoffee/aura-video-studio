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
    expect(screen.getByText('Video Studio')).toBeInTheDocument();
    expect(screen.getByText('Initializing Aura Video Studio')).toBeInTheDocument();
    expect(screen.getByText('Version 1.0.0')).toBeInTheDocument();
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

  it('renders animated particles', () => {
    const onComplete = vi.fn();
    render(<SplashScreen onComplete={onComplete} />);

    const particles = document.querySelectorAll('.splash-particle');
    expect(particles.length).toBe(20);
  });

  it('renders logo with gradient', () => {
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

    // Check footer
    expect(document.querySelector('.splash-footer')).toBeInTheDocument();
    expect(document.querySelector('.splash-version')).toBeInTheDocument();
  });
});
