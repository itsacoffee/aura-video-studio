/**
 * Tests for LazyImage component
 */

import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { LazyImage } from './LazyImage';

// Mock IntersectionObserver globally
const mockIntersectionObserverCallback = vi.fn();
const mockObserverInstance = {
  observe: vi.fn(),
  disconnect: vi.fn(),
  unobserve: vi.fn(),
};

const mockIntersectionObserver = vi.fn((callback: IntersectionObserverCallback) => {
  mockIntersectionObserverCallback.mockImplementation(callback);
  return mockObserverInstance;
});

global.IntersectionObserver = mockIntersectionObserver as unknown as typeof IntersectionObserver;

describe('LazyImage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render placeholder initially', () => {
    render(<LazyImage src="/test.jpg" alt="Test image" placeholderText="Loading..." />);

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('should render placeholder with default text when not provided', () => {
    render(<LazyImage src="/test.jpg" alt="Test image" />);

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('should use IntersectionObserver to detect when in view', async () => {
    render(<LazyImage src="/test.jpg" alt="Test image" />);

    expect(mockIntersectionObserver).toHaveBeenCalled();
    expect(mockObserverInstance.observe).toHaveBeenCalled();

    // Simulate intersection
    mockIntersectionObserverCallback(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver
    );

    await waitFor(() => {
      const img = screen.getByAltText('Test image');
      expect(img).toBeInTheDocument();
    });
  });

  it('should show error message when image fails to load', async () => {
    const onError = vi.fn();
    const { container } = render(
      <LazyImage src="/invalid.jpg" alt="Test image" onError={onError} />
    );

    // Trigger intersection
    mockIntersectionObserverCallback(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver
    );

    await waitFor(() => {
      const img = container.querySelector('img');
      expect(img).toBeInTheDocument();
    });

    const img = container.querySelector('img');
    if (img) {
      const event = new Event('error', { bubbles: true });
      img.dispatchEvent(event);
    }

    await waitFor(() => {
      expect(screen.getByText('Failed to load')).toBeInTheDocument();
    });

    expect(onError).toHaveBeenCalled();
  });

  it('should call onLoad when image loads successfully', async () => {
    const onLoad = vi.fn();
    const { container } = render(<LazyImage src="/test.jpg" alt="Test image" onLoad={onLoad} />);

    // Trigger intersection
    mockIntersectionObserverCallback(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver
    );

    await waitFor(() => {
      const img = container.querySelector('img');
      expect(img).toBeInTheDocument();
    });

    const img = container.querySelector('img');
    if (img) {
      const event = new Event('load', { bubbles: true });
      img.dispatchEvent(event);
    }

    await waitFor(() => {
      expect(onLoad).toHaveBeenCalled();
    });
  });

  it('should apply custom className', () => {
    const { container } = render(
      <LazyImage src="/test.jpg" alt="Test image" className="custom-class" />
    );

    const containerDiv = container.firstChild as HTMLElement;
    expect(containerDiv).toHaveClass('custom-class');
  });

  it('should set width and height on container', () => {
    const { container } = render(
      <LazyImage src="/test.jpg" alt="Test image" width="200px" height="150px" />
    );

    const containerDiv = container.firstChild as HTMLElement;
    const style = containerDiv.getAttribute('style');
    expect(style).toContain('width');
    expect(style).toContain('height');
  });

  it('should disconnect observer on unmount', () => {
    const { unmount } = render(<LazyImage src="/test.jpg" alt="Test image" />);

    unmount();

    expect(mockObserverInstance.disconnect).toHaveBeenCalled();
  });
});
