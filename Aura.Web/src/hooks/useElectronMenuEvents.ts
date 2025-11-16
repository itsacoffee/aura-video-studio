/**
 * Hook to register Electron menu event listeners
 * Wires up menu items (File, Edit, View, Tools, Help) to React app actions
 *
 * REQUIREMENT 2: Compile-time type safety between menu routes and Router paths
 */

import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loggingService } from '../services/loggingService';
import { MENU_EVENT_ROUTES } from '../services/routeRegistry';
import type { MenuAPI, OpenRecentProjectData } from '../types/electron-menu';

/**
 * React hook that sets up listeners for all Electron menu events
 * Automatically cleans up all listeners when component unmounts
 *
 * Usage:
 * ```tsx
 * function App() {
 *   useElectronMenuEvents();
 *   return <div>...</div>;
 * }
 * ```
 */
export function useElectronMenuEvents() {
  const navigate = useNavigate();

  useEffect(() => {
    // Only run in Electron environment
    const menuApi = window.aura?.menu ?? window.electron?.menu;
    if (!menuApi) {
      return;
    }

    const menu: MenuAPI = menuApi;
    const unsubscribers: Array<() => void> = [];

    try {
      // File Menu
      if (menu.onNewProject) {
        const unsub = menu.onNewProject(() => {
          loggingService.info('Menu action: New Project');
          navigate(MENU_EVENT_ROUTES.onNewProject);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenProject) {
        const unsub = menu.onOpenProject(() => {
          loggingService.info('Menu action: Open Project');
          navigate(MENU_EVENT_ROUTES.onOpenProject);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenRecentProject) {
        const unsub = menu.onOpenRecentProject((data: OpenRecentProjectData) => {
          loggingService.info('Menu action: Open Recent Project', { path: data.path });
          navigate(MENU_EVENT_ROUTES.onOpenRecentProject);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onSaveProject) {
        const unsub = menu.onSaveProject(() => {
          loggingService.info('Menu action: Save Project');
          window.dispatchEvent(new CustomEvent('app:saveProject'));
        });
        unsubscribers.push(unsub);
      }

      if (menu.onSaveProjectAs) {
        const unsub = menu.onSaveProjectAs(() => {
          loggingService.info('Menu action: Save Project As');
          window.dispatchEvent(new CustomEvent('app:saveProjectAs'));
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportVideo) {
        const unsub = menu.onImportVideo(() => {
          loggingService.info('Menu action: Import Video');
          navigate(MENU_EVENT_ROUTES.onImportVideo);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportAudio) {
        const unsub = menu.onImportAudio(() => {
          loggingService.info('Menu action: Import Audio');
          navigate(MENU_EVENT_ROUTES.onImportAudio);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportImages) {
        const unsub = menu.onImportImages(() => {
          loggingService.info('Menu action: Import Images');
          navigate(MENU_EVENT_ROUTES.onImportImages);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportDocument) {
        const unsub = menu.onImportDocument(() => {
          loggingService.info('Menu action: Import Document');
          navigate(MENU_EVENT_ROUTES.onImportDocument);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onExportVideo) {
        const unsub = menu.onExportVideo(() => {
          loggingService.info('Menu action: Export Video');
          navigate(MENU_EVENT_ROUTES.onExportVideo);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onExportTimeline) {
        const unsub = menu.onExportTimeline(() => {
          loggingService.info('Menu action: Export Timeline');
          navigate(MENU_EVENT_ROUTES.onExportTimeline);
        });
        unsubscribers.push(unsub);
      }

      // Edit Menu
      if (menu.onFind) {
        const unsub = menu.onFind(() => {
          loggingService.info('Menu action: Find');
          window.dispatchEvent(new CustomEvent('app:showFind'));
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenPreferences) {
        const unsub = menu.onOpenPreferences(() => {
          loggingService.info('Menu action: Open Preferences');
          navigate(MENU_EVENT_ROUTES.onOpenPreferences);
        });
        unsubscribers.push(unsub);
      }

      // Tools Menu
      if (menu.onOpenProviderSettings) {
        const unsub = menu.onOpenProviderSettings(() => {
          loggingService.info('Menu action: Open Provider Settings');
          navigate(`${MENU_EVENT_ROUTES.onOpenProviderSettings}?tab=providers`);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenFFmpegConfig) {
        const unsub = menu.onOpenFFmpegConfig(() => {
          loggingService.info('Menu action: Open FFmpeg Config');
          navigate(`${MENU_EVENT_ROUTES.onOpenFFmpegConfig}?tab=ffmpeg`);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onClearCache) {
        const unsub = menu.onClearCache(() => {
          loggingService.info('Menu action: Clear Cache');
          window.dispatchEvent(new CustomEvent('app:clearCache'));
        });
        unsubscribers.push(unsub);
      }

      if (menu.onViewLogs) {
        const unsub = menu.onViewLogs(() => {
          loggingService.info('Menu action: View Logs');
          navigate(MENU_EVENT_ROUTES.onViewLogs);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onRunDiagnostics) {
        const unsub = menu.onRunDiagnostics(() => {
          loggingService.info('Menu action: Run Diagnostics');
          navigate(MENU_EVENT_ROUTES.onRunDiagnostics);
        });
        unsubscribers.push(unsub);
      }

      // Help Menu
      if (menu.onOpenGettingStarted) {
        const unsub = menu.onOpenGettingStarted(() => {
          loggingService.info('Menu action: Open Getting Started');
          navigate(MENU_EVENT_ROUTES.onOpenGettingStarted);
        });
        unsubscribers.push(unsub);
      }

      if (menu.onShowKeyboardShortcuts) {
        const unsub = menu.onShowKeyboardShortcuts(() => {
          loggingService.info('Menu action: Show Keyboard Shortcuts');
          window.dispatchEvent(new CustomEvent('app:showKeyboardShortcuts'));
        });
        unsubscribers.push(unsub);
      }

      if (menu.onCheckForUpdates) {
        const unsub = menu.onCheckForUpdates(() => {
          loggingService.info('Menu action: Check for Updates');
          window.dispatchEvent(new CustomEvent('app:checkForUpdates'));
        });
        unsubscribers.push(unsub);
      }

      loggingService.info('Electron menu event listeners registered successfully');
    } catch (error) {
      loggingService.error('Failed to register menu event listeners', { error });
    }

    // Cleanup function to unsubscribe all listeners
    return () => {
      unsubscribers.forEach((unsub) => {
        try {
          unsub();
        } catch (error) {
          loggingService.error('Error unsubscribing menu listener', { error });
        }
      });
    };
  }, [navigate]);
}
