import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useContextMenu, useContextMenuAction } from '../useContextMenu';

// Mock window.electron.contextMenu
const mockContextMenu = {
  show: vi.fn(),
  onAction: vi.fn(),
};

describe('useContextMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Setup mock electron API
    (window as unknown as { electron: { contextMenu: typeof mockContextMenu } }).electron = {
      contextMenu: mockContextMenu,
    };
  });

  afterEach(() => {
    delete (window as unknown as { electron?: unknown }).electron;
  });

  describe('useContextMenu hook', () => {
    it('should return a function to show the context menu', () => {
      const { result } = renderHook(() => useContextMenu('timeline-clip'));
      expect(typeof result.current).toBe('function');
    });

    it('should call window.electron.contextMenu.show when invoked', async () => {
      mockContextMenu.show.mockResolvedValue({ success: true });

      const { result } = renderHook(() => useContextMenu('timeline-clip'));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, { clipId: 'test-clip' });
      });

      expect(mockEvent.preventDefault).toHaveBeenCalled();
      expect(mockEvent.stopPropagation).toHaveBeenCalled();
      expect(mockContextMenu.show).toHaveBeenCalledWith('timeline-clip', {
        clipId: 'test-clip',
        position: { x: 100, y: 200 },
      });
    });

    it('should log warning when electron API is not available', async () => {
      delete (window as unknown as { electron?: unknown }).electron;
      const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const { result } = renderHook(() => useContextMenu('timeline-clip'));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, { clipId: 'test-clip' });
      });

      expect(consoleWarn).toHaveBeenCalledWith('Context menu API not available');
      consoleWarn.mockRestore();
    });

    it('should handle errors from context menu show', async () => {
      mockContextMenu.show.mockResolvedValue({ success: false, error: 'Test error' });
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

      const { result } = renderHook(() => useContextMenu('timeline-clip'));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, { clipId: 'test-clip' });
      });

      expect(consoleError).toHaveBeenCalledWith('Failed to show context menu:', 'Test error');
      consoleError.mockRestore();
    });

    it('should handle exceptions from context menu show', async () => {
      mockContextMenu.show.mockRejectedValue(new Error('Network error'));
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

      const { result } = renderHook(() => useContextMenu('timeline-clip'));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, { clipId: 'test-clip' });
      });

      expect(consoleError).toHaveBeenCalledWith('Error showing context menu:', 'Network error');
      consoleError.mockRestore();
    });
  });

  describe('useContextMenuAction hook', () => {
    it('should register action listener on mount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callback = vi.fn();
      renderHook(() => useContextMenuAction('timeline-clip', 'onCut', callback));

      expect(mockContextMenu.onAction).toHaveBeenCalledWith('timeline-clip', 'onCut', callback);
    });

    it('should unsubscribe on unmount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callback = vi.fn();
      const { unmount } = renderHook(() =>
        useContextMenuAction('timeline-clip', 'onCut', callback)
      );

      expect(unsubscribe).not.toHaveBeenCalled();

      unmount();

      expect(unsubscribe).toHaveBeenCalled();
    });

    it('should not register listener when electron API is not available', () => {
      delete (window as unknown as { electron?: unknown }).electron;

      const callback = vi.fn();
      renderHook(() => useContextMenuAction('timeline-clip', 'onCut', callback));

      expect(mockContextMenu.onAction).not.toHaveBeenCalled();
    });

    it('should re-register when callback changes', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callback1 = vi.fn();
      const callback2 = vi.fn();

      const { rerender } = renderHook(
        ({ callback }) => useContextMenuAction('timeline-clip', 'onCut', callback),
        { initialProps: { callback: callback1 } }
      );

      expect(mockContextMenu.onAction).toHaveBeenCalledTimes(1);
      expect(mockContextMenu.onAction).toHaveBeenLastCalledWith(
        'timeline-clip',
        'onCut',
        callback1
      );

      rerender({ callback: callback2 });

      expect(mockContextMenu.onAction).toHaveBeenCalledTimes(2);
      expect(mockContextMenu.onAction).toHaveBeenLastCalledWith(
        'timeline-clip',
        'onCut',
        callback2
      );
      expect(unsubscribe).toHaveBeenCalledTimes(1);
    });
  });
});
