/**
 * Hook to register Electron menu event listeners
 * Wires up menu items (File, Edit, View, Tools, Help) to React app actions
 */

import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loggingService } from '../services/loggingService';
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
    if (!window.electron?.menu) {
      return;
    }

    const menu: MenuAPI = window.electron.menu;
    const unsubscribers: Array<() => void> = [];

    try {
      // File Menu
      if (menu.onNewProject) {
        const unsub = menu.onNewProject(() => {
          loggingService.info('Menu action: New Project');
          navigate('/create');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenProject) {
        const unsub = menu.onOpenProject(() => {
          loggingService.info('Menu action: Open Project');
          navigate('/projects');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenRecentProject) {
        const unsub = menu.onOpenRecentProject((data: OpenRecentProjectData) => {
          loggingService.info('Menu action: Open Recent Project', { path: data.path });
          navigate('/projects');
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
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportAudio) {
        const unsub = menu.onImportAudio(() => {
          loggingService.info('Menu action: Import Audio');
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportImages) {
        const unsub = menu.onImportImages(() => {
          loggingService.info('Menu action: Import Images');
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onImportDocument) {
        const unsub = menu.onImportDocument(() => {
          loggingService.info('Menu action: Import Document');
          navigate('/rag');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onExportVideo) {
        const unsub = menu.onExportVideo(() => {
          loggingService.info('Menu action: Export Video');
          navigate('/render');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onExportTimeline) {
        const unsub = menu.onExportTimeline(() => {
          loggingService.info('Menu action: Export Timeline');
          navigate('/editor');
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
          navigate('/settings');
        });
        unsubscribers.push(unsub);
      }

      // Tools Menu
      if (menu.onOpenProviderSettings) {
        const unsub = menu.onOpenProviderSettings(() => {
          loggingService.info('Menu action: Open Provider Settings');
          navigate('/settings?tab=providers');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onOpenFFmpegConfig) {
        const unsub = menu.onOpenFFmpegConfig(() => {
          loggingService.info('Menu action: Open FFmpeg Config');
          navigate('/settings?tab=ffmpeg');
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
          navigate('/logs');
        });
        unsubscribers.push(unsub);
      }

      if (menu.onRunDiagnostics) {
        const unsub = menu.onRunDiagnostics(() => {
          loggingService.info('Menu action: Run Diagnostics');
          navigate('/health');
        });
        unsubscribers.push(unsub);
      }

      // Help Menu
      if (menu.onOpenGettingStarted) {
        const unsub = menu.onOpenGettingStarted(() => {
          loggingService.info('Menu action: Open Getting Started');
          navigate('/');
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
