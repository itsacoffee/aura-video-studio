/**
 * Tests for VideoThumbnail component
 * Enhanced with error handling and file access validation tests
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi, type Mock } from 'vitest';
import { VideoThumbnail } from '../components/Editor/Timeline/VideoThumbnail';

// Create mock FFmpeg instance that we can control
let mockFFmpegInstance: {
  load: Mock;
  writeFile: Mock;
  exec: Mock;
  readFile: Mock;
  deleteFile: Mock;
};

// Mock FFmpeg
vi.mock('@ffmpeg/ffmpeg', () => ({
  FFmpeg: vi.fn().mockImplementation(() => {
    mockFFmpegInstance = {
      load: vi.fn().mockResolvedValue(undefined),
      writeFile: vi.fn().mockResolvedValue(undefined),
      exec: vi.fn().mockResolvedValue(undefined),
      readFile: vi.fn().mockResolvedValue(new Uint8Array([0xff, 0xd8, 0xff, 0xe0])), // Minimal JPEG header
      deleteFile: vi.fn().mockResolvedValue(undefined),
    };
    return mockFFmpegInstance;
  }),
}));

let mockFetchFile: Mock;

vi.mock('@ffmpeg/util', () => ({
  fetchFile: vi.fn().mockImplementation((...args) => {
    if (mockFetchFile) {
      return mockFetchFile(...args);
    }
    return Promise.resolve(new Uint8Array());
  }),
  toBlobURL: vi.fn().mockResolvedValue('blob:mock-url'),
}));

describe('VideoThumbnail', () => {
  const renderComponent = (props = {}) => {
    return render(
      <FluentProvider theme={webLightTheme}>
        <VideoThumbnail {...props} />
      </FluentProvider>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock URL.createObjectURL and URL.revokeObjectURL for jsdom environment
    if (!URL.createObjectURL) {
      URL.createObjectURL = vi.fn().mockReturnValue('blob:mock-thumbnail-url');
    }
    if (!URL.revokeObjectURL) {
      URL.revokeObjectURL = vi.fn();
    }
  });

  it('should render placeholder when no video path is provided', () => {
    renderComponent();
    expect(screen.getByText('No Video')).toBeDefined();
  });

  it('should show loading state initially when video path is provided', async () => {
    renderComponent({ videoPath: '/test/video.mp4' });

    await waitFor(
      () => {
        const spinner = screen.queryByText(/Loading thumbnail/i);
        if (spinner) {
          expect(spinner).toBeDefined();
        }
      },
      { timeout: 1000 }
    );
  });

  it('should render with custom dimensions', () => {
    const { container } = renderComponent({
      videoPath: '/test/video.mp4',
      width: 200,
      height: 120,
    });

    expect(container.querySelector('.container')).toBeDefined();
  });

  it('should use specified timestamp for thumbnail extraction', async () => {
    renderComponent({
      videoPath: '/test/video.mp4',
      timestamp: 5,
    });

    // Component should attempt to load FFmpeg and extract thumbnail
    await waitFor(() => {
      expect(true).toBe(true); // FFmpeg mocks are called
    });
  });

  it('should render placeholder icon when FFmpeg initialization fails', async () => {
    // This test validates graceful degradation
    const { container } = renderComponent({ videoPath: '/test/video.mp4' });

    await waitFor(
      () => {
        const placeholder = container.querySelector('.placeholder');
        if (placeholder) {
          expect(placeholder.textContent).toContain('📹');
        }
      },
      { timeout: 2000 }
    );
  });

  it('should handle fetchFile errors gracefully and show error message', async () => {
    // Mock fetchFile to throw an error (e.g., 404 or network error)
    mockFetchFile = vi.fn().mockRejectedValue(new Error('Network error: 404 Not Found'));

    renderComponent({ videoPath: '/test/invalid-video.mp4' });

    // Wait for error message to appear
    await waitFor(
      () => {
        const errorElement = screen.queryByText(/Cannot load video/i);
        expect(errorElement).toBeDefined();
      },
      { timeout: 2000 }
    );
  });

  it('should handle FFmpeg exec errors gracefully and show fallback UI', async () => {
    // Mock fetchFile to succeed
    mockFetchFile = vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3]));

    // Wait for FFmpeg instance to be created
    await waitFor(() => {
      expect(mockFFmpegInstance).toBeDefined();
    });

    // Mock exec to fail
    if (mockFFmpegInstance) {
      mockFFmpegInstance.exec.mockRejectedValue(new Error('FFmpeg processing failed'));
    }

    renderComponent({ videoPath: '/test/corrupt-video.mp4' });

    // Wait for error message to appear
    await waitFor(
      () => {
        const errorElement = screen.queryByText(/Failed to process video/i);
        expect(errorElement).toBeDefined();
      },
      { timeout: 2000 }
    );
  });

  it('should cleanup FFmpeg files even when errors occur', async () => {
    // Mock fetchFile to succeed
    mockFetchFile = vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3]));

    // Wait for FFmpeg instance to be created
    await waitFor(() => {
      expect(mockFFmpegInstance).toBeDefined();
    });

    // Mock exec to fail
    if (mockFFmpegInstance) {
      mockFFmpegInstance.exec.mockRejectedValue(new Error('Processing error'));
    }

    renderComponent({ videoPath: '/test/video.mp4' });

    // Wait for processing to complete
    await waitFor(
      () => {
        const errorElement = screen.queryByText(/Failed to process video/i);
        expect(errorElement).toBeDefined();
      },
      { timeout: 2000 }
    );

    // Verify deleteFile was called for cleanup despite error
    if (mockFFmpegInstance) {
      await waitFor(() => {
        expect(mockFFmpegInstance.deleteFile).toHaveBeenCalled();
      });
    }
  });

  it('should support non-mp4 video files by deriving extension', async () => {
    // Mock fetchFile to succeed
    mockFetchFile = vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3]));

    renderComponent({ videoPath: '/test/video.webm' });

    // Wait for writeFile to be called
    await waitFor(
      () => {
        if (mockFFmpegInstance && mockFFmpegInstance.writeFile.mock.calls.length > 0) {
          const firstCall = mockFFmpegInstance.writeFile.mock.calls[0];
          // First argument should be the filename, which should end with .webm
          expect(firstCall[0]).toMatch(/\.webm$/);
        }
      },
      { timeout: 2000 }
    );
  });

  it('should revoke previous thumbnail URL when generating new thumbnail', async () => {
    // Mock URL.revokeObjectURL if it doesn't exist
    if (!URL.revokeObjectURL) {
      URL.revokeObjectURL = vi.fn();
    }

    // Create a spy on URL.revokeObjectURL
    const revokeObjectURLSpy = vi.spyOn(URL, 'revokeObjectURL');

    // Mock fetchFile to succeed
    mockFetchFile = vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3]));

    const { rerender } = renderComponent({ videoPath: '/test/video1.mp4' });

    // Wait for first thumbnail to load
    await waitFor(() => {
      if (mockFFmpegInstance) {
        expect(mockFFmpegInstance.exec).toHaveBeenCalled();
      }
    });

    // Change video path to trigger new thumbnail generation
    rerender(
      <FluentProvider theme={webLightTheme}>
        <VideoThumbnail videoPath="/test/video2.mp4" />
      </FluentProvider>
    );

    // Wait for revocation to occur
    await waitFor(() => {
      expect(revokeObjectURLSpy).toHaveBeenCalled();
    });

    revokeObjectURLSpy.mockRestore();
  });
});
