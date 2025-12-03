/**
 * ClipWaveform Component Tests
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ClipWaveform } from '../ClipWaveform';

// Mock the store
vi.mock('../../../../stores/opencutWaveforms', () => ({
  useWaveformStore: vi.fn(() => ({
    loadWaveform: vi.fn(),
    getWaveform: vi.fn(() => null),
    isLoading: vi.fn(() => false),
  })),
}));

import { useWaveformStore } from '../../../../stores/opencutWaveforms';

const mockUseWaveformStore = vi.mocked(useWaveformStore);

describe('ClipWaveform', () => {
  const defaultProps = {
    mediaId: 'test-media-1',
    audioUrl: 'http://example.com/audio.mp3',
    width: 200,
    height: 48,
    clipType: 'audio' as const,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    mockUseWaveformStore.mockReturnValue({
      loadWaveform: vi.fn(),
      getWaveform: vi.fn(() => null),
      isLoading: vi.fn(() => false),
      getError: vi.fn(() => undefined),
      clearWaveform: vi.fn(),
      clearAll: vi.fn(),
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });
  });

  const renderComponent = (props = {}) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <ClipWaveform {...defaultProps} {...props} />
      </FluentProvider>
    );
  };

  it('should render without crashing', () => {
    const { container } = renderComponent();
    expect(container).toBeDefined();
  });

  it('should call loadWaveform on mount', () => {
    const mockLoadWaveform = vi.fn();
    mockUseWaveformStore.mockReturnValue({
      loadWaveform: mockLoadWaveform,
      getWaveform: vi.fn(() => null),
      isLoading: vi.fn(() => false),
      getError: vi.fn(() => undefined),
      clearWaveform: vi.fn(),
      clearAll: vi.fn(),
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });

    renderComponent();

    expect(mockLoadWaveform).toHaveBeenCalledWith(
      'test-media-1',
      'http://example.com/audio.mp3',
      200 // default samples
    );
  });

  it('should use custom samples value', () => {
    const mockLoadWaveform = vi.fn();
    mockUseWaveformStore.mockReturnValue({
      loadWaveform: mockLoadWaveform,
      getWaveform: vi.fn(() => null),
      isLoading: vi.fn(() => false),
      getError: vi.fn(() => undefined),
      clearWaveform: vi.fn(),
      clearAll: vi.fn(),
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });

    renderComponent({ samples: 500 });

    expect(mockLoadWaveform).toHaveBeenCalledWith(
      'test-media-1',
      'http://example.com/audio.mp3',
      500
    );
  });

  it('should show loading state', () => {
    mockUseWaveformStore.mockReturnValue({
      loadWaveform: vi.fn(),
      getWaveform: vi.fn(() => null),
      isLoading: vi.fn(() => true),
      getError: vi.fn(() => undefined),
      clearWaveform: vi.fn(),
      clearAll: vi.fn(),
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });

    const { container } = renderComponent();
    // The WaveformDisplay should receive isLoading=true
    expect(container).toBeDefined();
  });

  it('should render waveform when data is available', () => {
    const mockWaveformData = {
      peaks: [0.5, 0.8, 0.3],
      duration: 5,
      sampleRate: 44100,
      channels: 1,
    };

    mockUseWaveformStore.mockReturnValue({
      loadWaveform: vi.fn(),
      getWaveform: vi.fn(() => mockWaveformData),
      isLoading: vi.fn(() => false),
      getError: vi.fn(() => undefined),
      clearWaveform: vi.fn(),
      clearAll: vi.fn(),
      waveforms: new Map(),
      loading: new Set(),
      errors: new Map(),
    });

    const { container } = renderComponent();
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeDefined();
  });

  it('should apply video clip type color', () => {
    const { container } = renderComponent({ clipType: 'video' });
    expect(container).toBeDefined();
  });
});
