import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { useAIProviderContextMenu } from '../useAIProviderContextMenu';

// Mock window.electron.contextMenu
const mockContextMenu = {
  show: vi.fn(),
  onAction: vi.fn(),
};

describe('useAIProviderContextMenu', () => {
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
      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      const { result } = renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );
      expect(typeof result.current).toBe('function');
    });

    it('should call window.electron.contextMenu.show with ai-provider type when invoked', async () => {
      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      const { result } = renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockProvider = {
        id: 'openai',
        type: 'llm' as const,
        isDefault: true,
        hasFallback: false,
      };

      await act(async () => {
        await result.current(mockEvent, mockProvider);
      });

      expect(mockEvent.preventDefault).toHaveBeenCalled();
      expect(mockEvent.stopPropagation).toHaveBeenCalled();
      expect(mockContextMenu.show).toHaveBeenCalledWith('ai-provider', {
        providerId: 'openai',
        providerType: 'llm',
        isDefault: true,
        hasFallback: false,
        position: { x: 100, y: 200 },
      });
    });

    it('should handle providers with missing optional properties', async () => {
      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      const { result } = renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 50,
        clientY: 100,
      } as unknown as React.MouseEvent;

      const mockProvider = {
        id: 'elevenlabs',
        type: 'tts' as const,
        // isDefault and hasFallback are undefined
      };

      await act(async () => {
        await result.current(mockEvent, mockProvider);
      });

      expect(mockContextMenu.show).toHaveBeenCalledWith('ai-provider', {
        providerId: 'elevenlabs',
        providerType: 'tts',
        isDefault: false,
        hasFallback: false,
        position: { x: 50, y: 100 },
      });
    });
  });

  describe('action listeners', () => {
    it('should register action listeners for all context menu actions', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      // Should register listeners for all actions
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-provider',
        'onTestConnection',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-provider',
        'onViewStats',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-provider',
        'onSetDefault',
        expect.any(Function)
      );
      expect(mockContextMenu.onAction).toHaveBeenCalledWith(
        'ai-provider',
        'onConfigure',
        expect.any(Function)
      );
    });

    it('should unsubscribe all listeners on unmount', () => {
      const unsubscribe = vi.fn();
      mockContextMenu.onAction.mockReturnValue(unsubscribe);

      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      const { unmount } = renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      expect(unsubscribe).not.toHaveBeenCalled();

      unmount();

      // 4 action listeners should be unsubscribed
      expect(unsubscribe).toHaveBeenCalledTimes(4);
    });
  });

  describe('when electron API is not available', () => {
    it('should log warning when showing context menu', async () => {
      delete (window as unknown as { electron?: unknown }).electron;
      const consoleWarn = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      const { result } = renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      const mockEvent = {
        preventDefault: vi.fn(),
        stopPropagation: vi.fn(),
        clientX: 100,
        clientY: 200,
      } as unknown as React.MouseEvent;

      const mockProvider = {
        id: 'openai',
        type: 'llm' as const,
      };

      await act(async () => {
        await result.current(mockEvent, mockProvider);
      });

      expect(consoleWarn).toHaveBeenCalledWith('Context menu API not available');
      consoleWarn.mockRestore();
    });

    it('should not register action listeners', () => {
      delete (window as unknown as { electron?: unknown }).electron;

      const onTestConnection = vi.fn();
      const onViewStats = vi.fn();
      const onSetDefault = vi.fn();
      const onConfigure = vi.fn();

      renderHook(() =>
        useAIProviderContextMenu(onTestConnection, onViewStats, onSetDefault, onConfigure)
      );

      expect(mockContextMenu.onAction).not.toHaveBeenCalled();
    });
  });
});
