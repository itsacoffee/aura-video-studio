import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { useJobQueueContextMenu } from '../useJobQueueContextMenu';

// Mock window.electron.contextMenu
const mockContextMenu = {
  show: vi.fn(),
  onAction: vi.fn(),
};

// Mock window.confirm
const originalConfirm = window.confirm;

describe('useJobQueueContextMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Setup mock electron API with proper return value for onAction
    const unsubscribe = vi.fn();
    mockContextMenu.onAction.mockReturnValue(unsubscribe);
    mockContextMenu.show.mockResolvedValue({ success: true });

    (window as unknown as { electron: { contextMenu: typeof mockContextMenu } }).electron = {
      contextMenu: mockContextMenu,
    };

    // Mock confirm to always return true
    window.confirm = vi.fn(() => true);
  });

  afterEach(() => {
    delete (window as unknown as { electron?: unknown }).electron;
    window.confirm = originalConfirm;
  });

  describe('handleContextMenu', () => {
    it('should return a function to show the context menu', () => {
      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      const { result } = renderHook(() => useJobQueueContextMenu(callbacks));
      expect(typeof result.current).toBe('function');
    });

    it('should call window.electron.contextMenu.show with job-queue type when invoked', async () => {
      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      const { result } = renderHook(() => useJobQueueContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockJob = {
        id: 'job-123',
        status: 'running' as const,
        outputPath: '/output/video.mp4',
      };

      await act(async () => {
        await result.current(mockEvent, mockJob);
      });

      expect(mockEvent.preventDefault).toHaveBeenCalled();
      expect(mockEvent.stopPropagation).toHaveBeenCalled();
      expect(mockContextMenu.show).toHaveBeenCalledWith('job-queue', {
        jobId: 'job-123',
        status: 'running',
        outputPath: '/output/video.mp4',
        position: { x: 100, y: 200 },
      });
    });

    it('should handle jobs with missing optional properties', async () => {
      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      const { result } = renderHook(() => useJobQueueContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 50,
        clientY: 100,
      } as unknown as React.MouseEvent;

      const mockJob = {
        id: 'job-456',
        status: 'queued' as const,
        // outputPath is undefined
      };

      await act(async () => {
        await result.current(mockEvent, mockJob);
      });

      expect(mockContextMenu.show).toHaveBeenCalledWith('job-queue', {
        jobId: 'job-456',
        status: 'queued',
        outputPath: undefined,
        position: { x: 50, y: 100 },
      });
    });
  });

  describe('action listeners', () => {
    it('should register action listeners for all context menu actions', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      renderHook(() => useJobQueueContextMenu(callbacks));

      // Should register listeners for all actions
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'job-queue',
        'onPause',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'job-queue',
        'onResume',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'job-queue',
        'onCancel',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'job-queue',
        'onViewLogs',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'job-queue',
        'onRetry',
        expect.any(Function)
      );
    });

    it('should unsubscribe all listeners on unmount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      const { unmount } = renderHook(() => useJobQueueContextMenu(callbacks));

      expect(unsubscribe).not.toHaveBeenCalled();

      unmount();

      // 5 action listeners should be unsubscribed
      expect(unsubscribe).toHaveBeenCalledTimes(5);
    });
  });

  describe('when electron API is not available', () => {
    it('should log warning when showing context menu', async () => {
      delete (window as unknown as { electron?: unknown }).electron;
      const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      const { result } = renderHook(() => useJobQueueContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockJob = {
        id: 'job-123',
        status: 'running' as const,
      };

      await act(async () => {
        await result.current(mockEvent, mockJob);
      });

      expect(consoleWarn).toHaveBeenCalledWith('Context menu API not available');
      consoleWarn.mockRestore();
    });

    it('should not register action listeners', () => {
      delete (window as unknown as { electron?: unknown }).electron;

      const callbacks = {
        onPause: vi.fn(),
        onResume: vi.fn(),
        onCancel: vi.fn(),
        onViewLogs: vi.fn(),
        onRetry: vi.fn(),
      };

      renderHook(() => useJobQueueContextMenu(callbacks));

      expect(mockContextMenu.onAction).not.toHaveBeenCalled();
    });
  });
});
