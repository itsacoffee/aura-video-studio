/**
 * Logo Component Tests
 * Tests for Logo component with Electron support and fallback mechanism
 */

import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Logo } from '../Logo';

describe('Logo', () => {
  const originalUserAgent = navigator.userAgent;
  const originalLocation = window.location;

  beforeEach(() => {
    // Reset console mocks
    vi.spyOn(console, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    // Restore original values
    Object.defineProperty(window.navigator, 'userAgent', {
      value: originalUserAgent,
      configurable: true,
    });
    Object.defineProperty(window, 'location', {
      value: originalLocation,
      configurable: true,
    });
    vi.restoreAllMocks();
  });

  it('renders logo image with default size', () => {
    render(<Logo />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('width', '64');
    expect(img).toHaveAttribute('height', '64');
  });

  it('renders logo image with custom size', () => {
    render(<Logo size={128} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img).toHaveAttribute('width', '128');
    expect(img).toHaveAttribute('height', '128');
  });

  it('renders logo image with custom alt text', () => {
    render(<Logo alt="Custom Alt Text" />);

    const img = screen.getByRole('img', { name: 'Custom Alt Text' });
    expect(img).toBeInTheDocument();
  });

  it('uses absolute path for http protocol', () => {
    render(<Logo size={64} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img.getAttribute('src')).toMatch(/^\//);
  });

  it('selects correct image size for small logos', () => {
    render(<Logo size={16} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    const src = img.getAttribute('src');
    expect(src).toContain('favicon-16x16.png');
  });

  it('selects correct image size for medium logos', () => {
    render(<Logo size={64} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    const src = img.getAttribute('src');
    expect(src).toContain('logo256.png');
  });

  it('selects correct image size for large logos', () => {
    render(<Logo size={256} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    const src = img.getAttribute('src');
    expect(src).toContain('logo512.png');
  });

  it('shows fallback when image fails to load', async () => {
    const { container } = render(<Logo />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });

    // Trigger error
    img.dispatchEvent(new Event('error'));

    await waitFor(() => {
      // Check that the fallback div with "A" text is rendered
      const fallback = container.querySelector('div[style*="font-size"]');
      expect(fallback).toBeInTheDocument();
      expect(fallback).toHaveTextContent('A');
    });

    expect(console.warn).toHaveBeenCalledWith(
      expect.stringContaining('[Logo] Failed to load image')
    );
  });

  it('applies custom className', () => {
    const { container } = render(<Logo className="custom-class" />);

    const span = container.querySelector('.custom-class');
    expect(span).toBeInTheDocument();
  });

  it('sets draggable to false', () => {
    render(<Logo />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img).toHaveAttribute('draggable', 'false');
  });

  it('uses eager loading', () => {
    render(<Logo />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img).toHaveAttribute('loading', 'eager');
  });

  it('uses relative path in Electron environment', () => {
    // Mock Electron user agent
    Object.defineProperty(window.navigator, 'userAgent', {
      value:
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Electron/28.0.0 Safari/537.36',
      configurable: true,
    });

    render(<Logo size={64} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    const src = img.getAttribute('src');

    // In Electron, should use relative path (starts with ./)
    expect(src).toMatch(/^\.?\//);
  });

  it('uses relative path for file:// protocol', () => {
    // Mock file:// protocol
    Object.defineProperty(window, 'location', {
      value: {
        ...originalLocation,
        protocol: 'file:',
      },
      configurable: true,
    });

    render(<Logo size={64} />);

    const img = screen.getByRole('img', { name: 'Aura Video Studio' });
    const src = img.getAttribute('src');

    // For file:// protocol, should use relative path
    expect(src).toMatch(/^\.?\//);
  });

  it('updates image path when size prop changes', () => {
    const { rerender } = render(<Logo size={16} />);

    let img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img.getAttribute('src')).toContain('favicon-16x16.png');

    rerender(<Logo size={256} />);

    img = screen.getByRole('img', { name: 'Aura Video Studio' });
    expect(img.getAttribute('src')).toContain('logo512.png');
  });
});
