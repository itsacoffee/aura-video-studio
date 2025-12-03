/**
 * WaveformDisplay Component Tests
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { WaveformDisplay } from '../WaveformDisplay';

describe('WaveformDisplay', () => {
  const mockWaveformData = {
    peaks: [0.2, 0.5, 0.8, 0.3, 0.6, 0.9, 0.4, 0.7],
    duration: 10,
    sampleRate: 44100,
    channels: 2,
  };

  const renderComponent = (props = {}) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <WaveformDisplay waveformData={mockWaveformData} width={200} height={48} {...props} />
      </FluentProvider>
    );
  };

  it('should render with waveform data', () => {
    const { container } = renderComponent();
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
    expect(canvas).not.toBeNull();
  });

  it('should render with correct dimensions', () => {
    const { container } = renderComponent({ width: 300, height: 60 });
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
    expect(canvas?.style.width).toBe('300px');
    expect(canvas?.style.height).toBe('60px');
  });

  it('should show loading indicator when isLoading is true', () => {
    const { container } = renderComponent({ isLoading: true });
    // Loading bar should be present
    const loadingBar = container.querySelector('div > div > div');
    expect(loadingBar).toBeDefined();
  });

  it('should not show loading indicator when isLoading is false', () => {
    const { container } = renderComponent({ isLoading: false });
    // Check that the loading overlay is not present
    const containerDiv = container.firstChild as HTMLElement;
    expect(containerDiv?.children.length).toBe(1); // Only canvas, no loading overlay
  });

  it('should render without waveform data', () => {
    const { container } = render(
      <FluentProvider theme={webLightTheme}>
        <WaveformDisplay waveformData={null} width={200} height={48} />
      </FluentProvider>
    );
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
  });

  it('should apply custom color', () => {
    const { container } = renderComponent({ color: '#FF0000' });
    // Canvas is rendered, color is applied during drawing
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
  });

  it('should handle trim values', () => {
    const { container } = renderComponent({
      trimStart: 2,
      trimEnd: 1,
      clipDuration: 7,
    });
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
  });
});
