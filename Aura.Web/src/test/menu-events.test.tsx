/**
 * Unit tests for Electron menu event handlers
 *
 * REQUIREMENT 9: Unit test that mocks window.electron.menu.on* and verifies each handler is called
 */

import { renderHook } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { beforeEach, afterEach, describe, it, expect, vi } from 'vitest';
import { useElectronMenuEvents } from '../hooks/useElectronMenuEvents';
import type { MenuAPI } from '../types/electron-menu';

// Mock loggingService
vi.mock('../services/loggingService', () => ({
  loggingService: {
    info: vi.fn(),
    error: vi.fn(),
    warn: vi.fn(),
  },
}));

// Mock routeRegistry
vi.mock('../services/routeRegistry', () => ({
  MENU_EVENT_ROUTES: {
    onNewProject: '/create',
    onOpenProject: '/projects',
    onOpenRecentProject: '/projects',
    onImportVideo: '/assets',
    onImportAudio: '/assets',
    onImportImages: '/assets',
    onImportDocument: '/rag',
    onExportVideo: '/render',
    onExportTimeline: '/editor',
    onOpenPreferences: '/settings',
    onOpenProviderSettings: '/settings',
    onOpenFFmpegConfig: '/settings',
    onViewLogs: '/logs',
    onRunDiagnostics: '/health',
    onOpenGettingStarted: '/',
  },
}));

describe('useElectronMenuEvents', () => {
  let mockMenuAPI: MenuAPI;
  let mockNavigate: ReturnType<typeof vi.fn>;
  let unsubscribers: Array<() => void>;

  beforeEach(() => {
    // Reset unsubscribers
    unsubscribers = [];

    // Create mock navigate function
    mockNavigate = vi.fn();

    // Mock useNavigate
    vi.mock('react-router-dom', async () => {
      const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
      return {
        ...actual,
        useNavigate: () => mockNavigate,
      };
    });

    // Create mock menu API
    mockMenuAPI = {
      onNewProject: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenProject: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenRecentProject: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onSaveProject: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onSaveProjectAs: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onImportVideo: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onImportAudio: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onImportImages: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onImportDocument: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onExportVideo: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onExportTimeline: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onFind: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenPreferences: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenProviderSettings: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenFFmpegConfig: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onClearCache: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onViewLogs: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onRunDiagnostics: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onOpenGettingStarted: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onShowKeyboardShortcuts: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
      onCheckForUpdates: vi.fn((callback) => {
        const unsub = () => {};
        unsubscribers.push(unsub);
        return unsub;
      }),
    };

    // Set up window.electron
    (window as unknown as { electron: { menu: MenuAPI } }).electron = {
      menu: mockMenuAPI,
    };
  });

  afterEach(() => {
    // Clean up window.electron
    delete (window as unknown as { electron?: { menu: MenuAPI } }).electron;
    vi.clearAllMocks();
  });

  describe('Menu Event Registration', () => {
    it('should register onNewProject handler', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onNewProject).toHaveBeenCalled();
    });

    it('should register onOpenProject handler', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onOpenProject).toHaveBeenCalled();
    });

    it('should register onOpenRecentProject handler', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onOpenRecentProject).toHaveBeenCalled();
    });

    it('should register onSaveProject handler', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onSaveProject).toHaveBeenCalled();
    });

    it('should register onSaveProjectAs handler', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onSaveProjectAs).toHaveBeenCalled();
    });

    it('should register all import handlers', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onImportVideo).toHaveBeenCalled();
      expect(mockMenuAPI.onImportAudio).toHaveBeenCalled();
      expect(mockMenuAPI.onImportImages).toHaveBeenCalled();
      expect(mockMenuAPI.onImportDocument).toHaveBeenCalled();
    });

    it('should register all export handlers', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onExportVideo).toHaveBeenCalled();
      expect(mockMenuAPI.onExportTimeline).toHaveBeenCalled();
    });

    it('should register edit menu handlers', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onFind).toHaveBeenCalled();
      expect(mockMenuAPI.onOpenPreferences).toHaveBeenCalled();
    });

    it('should register tools menu handlers', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onOpenProviderSettings).toHaveBeenCalled();
      expect(mockMenuAPI.onOpenFFmpegConfig).toHaveBeenCalled();
      expect(mockMenuAPI.onClearCache).toHaveBeenCalled();
      expect(mockMenuAPI.onViewLogs).toHaveBeenCalled();
      expect(mockMenuAPI.onRunDiagnostics).toHaveBeenCalled();
    });

    it('should register help menu handlers', () => {
      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onOpenGettingStarted).toHaveBeenCalled();
      expect(mockMenuAPI.onShowKeyboardShortcuts).toHaveBeenCalled();
      expect(mockMenuAPI.onCheckForUpdates).toHaveBeenCalled();
    });
  });

  describe('Handler Behavior', () => {
    it('should call navigate with correct path for New Project', () => {
      // Re-mock onNewProject to capture and call the callback
      mockMenuAPI.onNewProject = vi.fn((callback) => {
        callback();
        return () => {};
      });

      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockNavigate).toHaveBeenCalledWith('/create');
    });

    it('should call navigate with correct path for Open Project', () => {
      mockMenuAPI.onOpenProject = vi.fn((callback) => {
        callback();
        return () => {};
      });

      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockNavigate).toHaveBeenCalledWith('/projects');
    });

    it('should dispatch custom event for Save Project', () => {
      const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

      mockMenuAPI.onSaveProject = vi.fn((callback) => {
        callback();
        return () => {};
      });

      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(dispatchSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'app:saveProject',
        })
      );

      dispatchSpy.mockRestore();
    });

    it('should dispatch custom event for Show Keyboard Shortcuts', () => {
      const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

      mockMenuAPI.onShowKeyboardShortcuts = vi.fn((callback) => {
        callback();
        return () => {};
      });

      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(dispatchSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          type: 'app:showKeyboardShortcuts',
        })
      );

      dispatchSpy.mockRestore();
    });
  });

  describe('Non-Electron Environment', () => {
    it('should not register handlers when window.electron is not available', () => {
      delete (window as unknown as { electron?: { menu: MenuAPI } }).electron;

      renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      expect(mockMenuAPI.onNewProject).not.toHaveBeenCalled();
      expect(mockMenuAPI.onOpenProject).not.toHaveBeenCalled();
    });
  });

  describe('Cleanup', () => {
    it('should call unsubscribe functions on unmount', () => {
      const unsubSpy1 = vi.fn();
      const unsubSpy2 = vi.fn();

      mockMenuAPI.onNewProject = vi.fn(() => unsubSpy1);
      mockMenuAPI.onOpenProject = vi.fn(() => unsubSpy2);

      const { unmount } = renderHook(() => useElectronMenuEvents(), {
        wrapper: BrowserRouter,
      });

      unmount();

      expect(unsubSpy1).toHaveBeenCalled();
      expect(unsubSpy2).toHaveBeenCalled();
    });
  });
});
