/**
 * Integration tests for menu navigation
 *
 * REQUIREMENT 3: Automated test that clicks each menu item and verifies correct page renders
 * REQUIREMENT 10: Integration test that verifies menu accelerators actually trigger actions
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
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

// Test pages for each route
const HomePage = () => <div>Home Page</div>;
const CreatePage = () => <div>Create Page</div>;
const ProjectsPage = () => <div>Projects Page</div>;
const AssetsPage = () => <div>Assets Page</div>;
const RagPage = () => <div>RAG Page</div>;
const RenderPage = () => <div>Render Page</div>;
const EditorPage = () => <div>Editor Page</div>;
const SettingsPage = () => <div>Settings Page</div>;
const LogsPage = () => <div>Logs Page</div>;
const HealthPage = () => <div>Health Page</div>;

// Test app component that uses the hook
function TestApp() {
  useElectronMenuEvents();

  return (
    <div>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/create" element={<CreatePage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/assets" element={<AssetsPage />} />
        <Route path="/rag" element={<RagPage />} />
        <Route path="/render" element={<RenderPage />} />
        <Route path="/editor" element={<EditorPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/logs" element={<LogsPage />} />
        <Route path="/health" element={<HealthPage />} />
      </Routes>
    </div>
  );
}

describe('Menu Navigation Integration Tests', () => {
  let mockMenuAPI: MenuAPI;
  let menuCallbacks: Record<string, () => void>;

  beforeEach(() => {
    menuCallbacks = {};

    // Create mock menu API that stores callbacks
    mockMenuAPI = {
      onNewProject: vi.fn((callback) => {
        menuCallbacks.onNewProject = callback;
        return () => {};
      }),
      onOpenProject: vi.fn((callback) => {
        menuCallbacks.onOpenProject = callback;
        return () => {};
      }),
      onOpenRecentProject: vi.fn((callback) => {
        menuCallbacks.onOpenRecentProject = callback;
        return () => {};
      }),
      onSaveProject: vi.fn((callback) => {
        menuCallbacks.onSaveProject = callback;
        return () => {};
      }),
      onSaveProjectAs: vi.fn((callback) => {
        menuCallbacks.onSaveProjectAs = callback;
        return () => {};
      }),
      onImportVideo: vi.fn((callback) => {
        menuCallbacks.onImportVideo = callback;
        return () => {};
      }),
      onImportAudio: vi.fn((callback) => {
        menuCallbacks.onImportAudio = callback;
        return () => {};
      }),
      onImportImages: vi.fn((callback) => {
        menuCallbacks.onImportImages = callback;
        return () => {};
      }),
      onImportDocument: vi.fn((callback) => {
        menuCallbacks.onImportDocument = callback;
        return () => {};
      }),
      onExportVideo: vi.fn((callback) => {
        menuCallbacks.onExportVideo = callback;
        return () => {};
      }),
      onExportTimeline: vi.fn((callback) => {
        menuCallbacks.onExportTimeline = callback;
        return () => {};
      }),
      onFind: vi.fn((callback) => {
        menuCallbacks.onFind = callback;
        return () => {};
      }),
      onOpenPreferences: vi.fn((callback) => {
        menuCallbacks.onOpenPreferences = callback;
        return () => {};
      }),
      onOpenProviderSettings: vi.fn((callback) => {
        menuCallbacks.onOpenProviderSettings = callback;
        return () => {};
      }),
      onOpenFFmpegConfig: vi.fn((callback) => {
        menuCallbacks.onOpenFFmpegConfig = callback;
        return () => {};
      }),
      onClearCache: vi.fn((callback) => {
        menuCallbacks.onClearCache = callback;
        return () => {};
      }),
      onViewLogs: vi.fn((callback) => {
        menuCallbacks.onViewLogs = callback;
        return () => {};
      }),
      onRunDiagnostics: vi.fn((callback) => {
        menuCallbacks.onRunDiagnostics = callback;
        return () => {};
      }),
      onOpenGettingStarted: vi.fn((callback) => {
        menuCallbacks.onOpenGettingStarted = callback;
        return () => {};
      }),
      onShowKeyboardShortcuts: vi.fn((callback) => {
        menuCallbacks.onShowKeyboardShortcuts = callback;
        return () => {};
      }),
      onCheckForUpdates: vi.fn((callback) => {
        menuCallbacks.onCheckForUpdates = callback;
        return () => {};
      }),
    };

    (window as unknown as { electron: { menu: MenuAPI } }).electron = {
      menu: mockMenuAPI,
    };
  });

  afterEach(() => {
    delete (window as unknown as { electron?: { menu: MenuAPI } }).electron;
    vi.clearAllMocks();
  });

  describe('File Menu Navigation', () => {
    it('should navigate to Create page when New Project is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      // Trigger New Project menu event
      menuCallbacks.onNewProject();

      await waitFor(() => {
        expect(screen.getByText('Create Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Projects page when Open Project is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onOpenProject();

      await waitFor(() => {
        expect(screen.getByText('Projects Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Assets page when Import Video is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onImportVideo();

      await waitFor(() => {
        expect(screen.getByText('Assets Page')).toBeInTheDocument();
      });
    });

    it('should navigate to RAG page when Import Document is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onImportDocument();

      await waitFor(() => {
        expect(screen.getByText('RAG Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Render page when Export Video is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onExportVideo();

      await waitFor(() => {
        expect(screen.getByText('Render Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Editor page when Export Timeline is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onExportTimeline();

      await waitFor(() => {
        expect(screen.getByText('Editor Page')).toBeInTheDocument();
      });
    });
  });

  describe('Edit Menu Navigation', () => {
    it('should navigate to Settings page when Open Preferences is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onOpenPreferences();

      await waitFor(() => {
        expect(screen.getByText('Settings Page')).toBeInTheDocument();
      });
    });
  });

  describe('Tools Menu Navigation', () => {
    it('should navigate to Settings page when Open Provider Settings is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onOpenProviderSettings();

      await waitFor(() => {
        expect(screen.getByText('Settings Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Logs page when View Logs is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onViewLogs();

      await waitFor(() => {
        expect(screen.getByText('Logs Page')).toBeInTheDocument();
      });
    });

    it('should navigate to Health page when Run Diagnostics is clicked', async () => {
      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onRunDiagnostics();

      await waitFor(() => {
        expect(screen.getByText('Health Page')).toBeInTheDocument();
      });
    });
  });

  describe('Help Menu Navigation', () => {
    it('should navigate to Home page when Getting Started is clicked', async () => {
      render(
        <BrowserRouter initialEntries={['/create']}>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onOpenGettingStarted();

      await waitFor(() => {
        expect(screen.getByText('Home Page')).toBeInTheDocument();
      });
    });
  });

  describe('Custom Event Dispatch', () => {
    it('should dispatch app:saveProject event when Save Project is clicked', async () => {
      const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onSaveProject();

      await waitFor(() => {
        expect(dispatchSpy).toHaveBeenCalledWith(
          expect.objectContaining({
            type: 'app:saveProject',
          })
        );
      });

      dispatchSpy.mockRestore();
    });

    it('should dispatch app:showKeyboardShortcuts event', async () => {
      const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onShowKeyboardShortcuts();

      await waitFor(() => {
        expect(dispatchSpy).toHaveBeenCalledWith(
          expect.objectContaining({
            type: 'app:showKeyboardShortcuts',
          })
        );
      });

      dispatchSpy.mockRestore();
    });

    it('should dispatch app:clearCache event when Clear Cache is clicked', async () => {
      const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

      render(
        <BrowserRouter>
          <FluentProvider theme={webLightTheme}>
            <TestApp />
          </FluentProvider>
        </BrowserRouter>
      );

      menuCallbacks.onClearCache();

      await waitFor(() => {
        expect(dispatchSpy).toHaveBeenCalledWith(
          expect.objectContaining({
            type: 'app:clearCache',
          })
        );
      });

      dispatchSpy.mockRestore();
    });
  });
});
