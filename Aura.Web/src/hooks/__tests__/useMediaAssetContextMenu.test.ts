import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useMediaAssetContextMenu } from '../useMediaAssetContextMenu';

// Mock window.electron.contextMenu
const mockContextMenu = {
  show: vi.fn(),
  onAction: vi.fn(),
};

describe('useMediaAssetContextMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Setup mock electron API with proper return value for onAction
    const unsubscribe = vi.fn();
    mockContextMenu.onAction.mockReturnValue(unsubscribe);
    mockContextMenu.show.mockResolvedValue({ success: true });

    (window as unknown as { electron: { contextMenu: typeof mockContextMenu } }).electron = {
      contextMenu: mockContextMenu,
    };
  });

  afterEach(() => {
    delete (window as unknown as { electron?: unknown }).electron;
  });

  describe('handleContextMenu', () => {
    it('should return a function to show the context menu', () => {
      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      const { result } = renderHook(() => useMediaAssetContextMenu(callbacks));
      expect(typeof result.current).toBe('function');
    });

    it('should call window.electron.contextMenu.show with media-asset type when invoked', async () => {
      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      const { result } = renderHook(() => useMediaAssetContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockAsset = {
        id: 'asset-123',
        type: 'video' as const,
        filePath: '/path/to/video.mp4',
        isFavorite: true,
        tags: ['tag1', 'tag2'],
      };

      await act(async () => {
        await result.current(mockEvent, mockAsset);
      });

      expect(mockEvent.preventDefault).toHaveBeenCalled();
      expect(mockEvent.stopPropagation).toHaveBeenCalled();
      expect(mockContextMenu.show).toHaveBeenCalledWith('media-asset', {
        assetId: 'asset-123',
        assetType: 'video',
        filePath: '/path/to/video.mp4',
        isFavorite: true,
        tags: ['tag1', 'tag2'],
        position: { x: 100, y: 200 },
      });
    });

    it('should handle assets with missing optional properties', async () => {
      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      const { result } = renderHook(() => useMediaAssetContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 50,
        clientY: 100,
      } as unknown as React.MouseEvent;

      const mockAsset = {
        id: 'asset-456',
        type: 'image' as const,
        // filePath, isFavorite, and tags are undefined
      };

      await act(async () => {
        await result.current(mockEvent, mockAsset);
      });

      expect(mockContextMenu.show).toHaveBeenCalledWith('media-asset', {
        assetId: 'asset-456',
        assetType: 'image',
        filePath: '',
        isFavorite: false,
        tags: [],
        position: { x: 50, y: 100 },
      });
    });
  });

  describe('action listeners', () => {
    it('should register action listeners for all context menu actions', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      renderHook(() => useMediaAssetContextMenu(callbacks));

      // Should register listeners for all actions
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onAddToTimeline',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onPreview',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onRename',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onToggleFavorite',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onDelete',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'media-asset',
        'onProperties',
        expect.any(Function)
      );
    });

    it('should unsubscribe all listeners on unmount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      const { unmount } = renderHook(() => useMediaAssetContextMenu(callbacks));

      expect(unsubscribe).not.toHaveBeenCalled();

      unmount();

      // 6 action listeners should be unsubscribed
      expect(unsubscribe).toHaveBeenCalledTimes(6);
    });
  });

  describe('when electron API is not available', () => {
    it('should log warning when showing context menu', async () => {
      delete (window as unknown as { electron?: unknown }).electron;
      const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      const { result } = renderHook(() => useMediaAssetContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockAsset = {
        id: 'asset-123',
        type: 'video' as const,
      };

      await act(async () => {
        await result.current(mockEvent, mockAsset);
      });

      expect(consoleWarn).toHaveBeenCalledWith('Context menu API not available');
      consoleWarn.mockRestore();
    });

    it('should not register action listeners', () => {
      delete (window as unknown as { electron?: unknown }).electron;

      const callbacks = {
        onAddToTimeline: vi.fn(),
        onPreview: vi.fn(),
        onRename: vi.fn(),
        onToggleFavorite: vi.fn(),
        onDelete: vi.fn(),
        onShowProperties: vi.fn(),
      };

      renderHook(() => useMediaAssetContextMenu(callbacks));

      expect(mockContextMenu.onAction).not.toHaveBeenCalled();
    });
  });
});
