import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { useAIScriptContextMenu } from '../useAIScriptContextMenu';

// Mock window.electron.contextMenu
const mockContextMenu = {
  show: vi.fn(),
  onAction: vi.fn(),
};

// Mock navigator.clipboard
const mockClipboard = {
  writeText: vi.fn(),
};

describe('useAIScriptContextMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Setup mock electron API with proper return value for onAction
    const unsubscribe = vi.fn();
    mockContextMenu.onAction.mockReturnValue(unsubscribe);
    mockContextMenu.show.mockResolvedValue({ success: true });
    mockClipboard.writeText.mockResolvedValue(undefined);

    (window as unknown as { electron: { contextMenu: typeof mockContextMenu } }).electron = {
      contextMenu: mockContextMenu,
    };

    Object.defineProperty(navigator, 'clipboard', {
      value: mockClipboard,
      writable: true,
      configurable: true,
    });
  });

  afterEach(() => {
    delete (window as unknown as { electron?: unknown }).electron;
  });

  describe('handleContextMenu', () => {
    it('should return a function to show the context menu', () => {
      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      const { result } = renderHook(() => useAIScriptContextMenu(callbacks));
      expect(typeof result.current).toBe('function');
    });

    it('should call window.electron.contextMenu.show with ai-script type when invoked', async () => {
      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      const { result } = renderHook(() => useAIScriptContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, 1, 'Scene narration text', 'job-123');
      });

      expect(mockEvent.preventDefault).toHaveBeenCalled();
      expect(mockEvent.stopPropagation).toHaveBeenCalled();
      expect(mockContextMenu.show).toHaveBeenCalledWith('ai-script', {
        sceneIndex: 1,
        sceneText: 'Scene narration text',
        jobId: 'job-123',
        position: { x: 100, y: 200 },
      });
    });

    it('should handle scene index 0 correctly', async () => {
      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      const { result } = renderHook(() => useAIScriptContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 50,
        clientY: 100,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, 0, 'First scene text', 'job-456');
      });

      expect(mockContextMenu.show).toHaveBeenCalledWith('ai-script', {
        sceneIndex: 0,
        sceneText: 'First scene text',
        jobId: 'job-456',
        position: { x: 50, y: 100 },
      });
    });
  });

  describe('action listeners', () => {
    it('should register action listeners for all context menu actions', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      renderHook(() => useAIScriptContextMenu(callbacks));

      // Should register listeners for all actions
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-script',
        'onRegenerate',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-script',
        'onExpand',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-script',
        'onShorten',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-script',
        'onGenerateBRoll',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-script',
        'onCopyText',
        expect.any(Function)
      );
    });

    it('should unsubscribe all listeners on unmount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      const { unmount } = renderHook(() => useAIScriptContextMenu(callbacks));

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
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      const { result } = renderHook(() => useAIScriptContextMenu(callbacks));

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      await act(async () => {
        await result.current(mockEvent, 1, 'Test text', 'job-123');
      });

      expect(consoleWarn).toHaveBeenCalledWith('Context menu API not available');
      consoleWarn.mockRestore();
    });

    it('should not register action listeners', () => {
      delete (window as unknown as { electron?: unknown }).electron;

      const callbacks = {
        onRegenerate: vi.fn(),
        onExpand: vi.fn(),
        onShorten: vi.fn(),
        onGenerateBRoll: vi.fn(),
        onCopyText: vi.fn(),
      };

      renderHook(() => useAIScriptContextMenu(callbacks));

      expect(mockContextMenu.onAction).not.toHaveBeenCalled();
    });
  });
});
