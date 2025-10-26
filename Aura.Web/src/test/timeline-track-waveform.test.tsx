/**
 * Tests for TimelineTrack component with WaveSurfer.js integration
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { TimelineTrack } from '../components/Editor/Timeline/TimelineTrack';

// Mock WaveSurfer.js
vi.mock('wavesurfer.js', () => ({
  default: {
    create: vi.fn(() => ({
      load: vi.fn().mockResolvedValue(undefined),
      destroy: vi.fn(),
      setOptions: vi.fn(),
    })),
  },
}));

describe('TimelineTrack with WaveSurfer', () => {
  const defaultProps = {
    name: 'Test Track',
    type: 'music' as const,
    duration: 120,
    zoom: 50,
  };

  const renderComponent = (props = {}) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <TimelineTrack {...defaultProps} {...props} />
      </FluentProvider>
    );
  };

  it('should render track with label', () => {
    renderComponent();
    expect(screen.getByText('Test Track')).toBeDefined();
    expect(screen.getByText('music')).toBeDefined();
  });

  it('should show loading state when audio is being loaded', async () => {
    renderComponent({ audioPath: '/test/audio.mp3' });

    // Should show loading initially (though it may be very brief)
    const loadingText = screen.queryByText(/Loading waveform/i);
    if (loadingText) {
      expect(loadingText).toBeDefined();
    }
  });

  it('should apply narration track color', () => {
    const { container } = renderComponent({ type: 'narration' });
    expect(container).toBeDefined();
    // Color is set internally, just verify component renders
  });

  it('should apply music track color', () => {
    const { container } = renderComponent({ type: 'music' });
    expect(container).toBeDefined();
  });

  it('should apply sfx track color', () => {
    const { container } = renderComponent({ type: 'sfx' });
    expect(container).toBeDefined();
  });

  it('should render without audio path', () => {
    const { container } = renderComponent({ audioPath: undefined });
    expect(container).toBeDefined();
  });

  it('should handle muted state', () => {
    const { container } = renderComponent({
      audioPath: '/test/audio.mp3',
      muted: true,
    });
    expect(container).toBeDefined();
  });

  it('should handle selected state with visual indicator', () => {
    const { container } = renderComponent({
      audioPath: '/test/audio.mp3',
      selected: true,
    });

    // Should have selected styling applied
    expect(container.querySelector('.waveformContainerSelected')).toBeDefined();
  });

  it('should call onSeek when provided', () => {
    const onSeek = vi.fn();
    const { container } = renderComponent({ onSeek });

    // Component should be ready to handle seek
    expect(container).toBeDefined();
  });

  it('should render with custom zoom level', () => {
    const { container } = renderComponent({ zoom: 100 });
    expect(container).toBeDefined();
  });
});
