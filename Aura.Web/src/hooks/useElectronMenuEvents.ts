/**
 * Hook to register Electron menu event listeners
 * Wires up menu items (File, Edit, View, Tools, Help) to React app actions
 */

import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loggingService } from '../services/loggingService';

interface ElectronAPI {
  menu?: {
    onNewProject?: (callback: () => void) => () => void;
    onOpenProject?: (callback: () => void) => () => void;
    onOpenRecentProject?: (callback: (data: { path: string }) => void) => () => void;
    onSaveProject?: (callback: () => void) => () => void;
    onSaveProjectAs?: (callback: () => void) => () => void;
    onImportVideo?: (callback: () => void) => () => void;
    onImportAudio?: (callback: () => void) => () => void;
    onImportImages?: (callback: () => void) => () => void;
    onImportDocument?: (callback: () => void) => () => void;
    onExportVideo?: (callback: () => void) => () => void;
    onExportTimeline?: (callback: () => void) => () => void;
    onFind?: (callback: () => void) => () => void;
    onOpenPreferences?: (callback: () => void) => () => void;
    onOpenProviderSettings?: (callback: () => void) => () => void;
    onOpenFFmpegConfig?: (callback: () => void) => () => void;
    onClearCache?: (callback: () => void) => () => void;
    onViewLogs?: (callback: () => void) => () => void;
    onRunDiagnostics?: (callback: () => void) => () => void;
    onOpenGettingStarted?: (callback: () => void) => () => void;
    onShowKeyboardShortcuts?: (callback: () => void) => () => void;
    onCheckForUpdates?: (callback: () => void) => () => void;
  };
}

declare global {
  interface Window {
    electron?: ElectronAPI;
  }
}

export function useElectronMenuEvents() {
  const navigate = useNavigate();

  useEffect(() => {
    // Only run in Electron environment
    if (!window.electron?.menu) {
      return;
    }

    const unsubscribers: Array<() => void> = [];

    try {
      // File Menu
      if (window.electron.menu.onNewProject) {
        const unsub = window.electron.menu.onNewProject(() => {
          loggingService.info('Menu action: New Project');
          navigate('/create');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onOpenProject) {
        const unsub = window.electron.menu.onOpenProject(() => {
          loggingService.info('Menu action: Open Project');
          navigate('/projects');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onOpenRecentProject) {
        const unsub = window.electron.menu.onOpenRecentProject((data: { path: string }) => {
          loggingService.info('Menu action: Open Recent Project', { path: data.path });
          navigate('/projects');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onSaveProject) {
        const unsub = window.electron.menu.onSaveProject(() => {
          loggingService.info('Menu action: Save Project');
          window.dispatchEvent(new CustomEvent('app:saveProject'));
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onSaveProjectAs) {
        const unsub = window.electron.menu.onSaveProjectAs(() => {
          loggingService.info('Menu action: Save Project As');
          window.dispatchEvent(new CustomEvent('app:saveProjectAs'));
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onImportVideo) {
        const unsub = window.electron.menu.onImportVideo(() => {
          loggingService.info('Menu action: Import Video');
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onImportAudio) {
        const unsub = window.electron.menu.onImportAudio(() => {
          loggingService.info('Menu action: Import Audio');
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onImportImages) {
        const unsub = window.electron.menu.onImportImages(() => {
          loggingService.info('Menu action: Import Images');
          navigate('/assets');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onImportDocument) {
        const unsub = window.electron.menu.onImportDocument(() => {
          loggingService.info('Menu action: Import Document');
          navigate('/rag');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onExportVideo) {
        const unsub = window.electron.menu.onExportVideo(() => {
          loggingService.info('Menu action: Export Video');
          navigate('/render');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onExportTimeline) {
        const unsub = window.electron.menu.onExportTimeline(() => {
          loggingService.info('Menu action: Export Timeline');
          navigate('/editor');
        });
        unsubscribers.push(unsub);
      }

      // Edit Menu
      if (window.electron.menu.onFind) {
        const unsub = window.electron.menu.onFind(() => {
          loggingService.info('Menu action: Find');
          window.dispatchEvent(new CustomEvent('app:showFind'));
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onOpenPreferences) {
        const unsub = window.electron.menu.onOpenPreferences(() => {
          loggingService.info('Menu action: Open Preferences');
          navigate('/settings');
        });
        unsubscribers.push(unsub);
      }

      // Tools Menu
      if (window.electron.menu.onOpenProviderSettings) {
        const unsub = window.electron.menu.onOpenProviderSettings(() => {
          loggingService.info('Menu action: Open Provider Settings');
          navigate('/settings?tab=providers');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onOpenFFmpegConfig) {
        const unsub = window.electron.menu.onOpenFFmpegConfig(() => {
          loggingService.info('Menu action: Open FFmpeg Config');
          navigate('/settings?tab=ffmpeg');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onClearCache) {
        const unsub = window.electron.menu.onClearCache(() => {
          loggingService.info('Menu action: Clear Cache');
          window.dispatchEvent(new CustomEvent('app:clearCache'));
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onViewLogs) {
        const unsub = window.electron.menu.onViewLogs(() => {
          loggingService.info('Menu action: View Logs');
          navigate('/logs');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onRunDiagnostics) {
        const unsub = window.electron.menu.onRunDiagnostics(() => {
          loggingService.info('Menu action: Run Diagnostics');
          navigate('/health');
        });
        unsubscribers.push(unsub);
      }

      // Help Menu
      if (window.electron.menu.onOpenGettingStarted) {
        const unsub = window.electron.menu.onOpenGettingStarted(() => {
          loggingService.info('Menu action: Open Getting Started');
          navigate('/');
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onShowKeyboardShortcuts) {
        const unsub = window.electron.menu.onShowKeyboardShortcuts(() => {
          loggingService.info('Menu action: Show Keyboard Shortcuts');
          window.dispatchEvent(new CustomEvent('app:showKeyboardShortcuts'));
        });
        unsubscribers.push(unsub);
      }

      if (window.electron.menu.onCheckForUpdates) {
        const unsub = window.electron.menu.onCheckForUpdates(() => {
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
