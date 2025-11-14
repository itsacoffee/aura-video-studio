/**
 * Enhanced Menu Command Hook
 *
 * Sets up centralized menu command handling using MenuCommandDispatcher.
 * Registers default handlers for all menu commands and integrates with
 * the notification system for user feedback.
 */

import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { loggingService } from '../services/loggingService';
import { menuCommandDispatcher, AppContext } from '../services/menuCommandDispatcher';
import type { MenuCommandPayload } from '../services/menuCommandDispatcher';
import { MENU_EVENT_ROUTES } from '../services/routeRegistry';
import { useNotificationStore } from '../state/notifications';

/**
 * Hook to set up centralized menu command handling
 *
 * Usage:
 * ```tsx
 * function App() {
 *   useMenuCommandSystem();
 *   return <div>...</div>;
 * }
 * ```
 */
export function useMenuCommandSystem() {
  const navigate = useNavigate();
  const { addNotification } = useNotificationStore();

  useEffect(() => {
    // Only run in Electron environment
    if (!window.electron?.menu) {
      loggingService.info('Not in Electron environment, skipping menu command setup');
      return;
    }

    loggingService.info('Initializing menu command system');

    // Set up toast handler for dispatcher
    menuCommandDispatcher.setToastHandler((message, type) => {
      addNotification({
        type: type === 'info' ? 'info' : type === 'warning' ? 'warning' : 'error',
        title: 'Menu Command',
        message,
      });
    });

    const menu = window.electron.menu;
    const unsubscribers: Array<() => void> = [];

    // Register handlers for all menu commands

    // File Menu Commands
    const unsubNewProject = menuCommandDispatcher.registerHandler({
      commandId: 'menu:newProject',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: New Project', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onNewProject);
      },
      context: AppContext.GLOBAL,
      feature: 'project-management',
    });

    const unsubOpenProject = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openProject',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open Project', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onOpenProject);
      },
      context: AppContext.GLOBAL,
      feature: 'project-management',
    });

    const unsubOpenRecentProject = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openRecentProject',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open Recent Project', {
          correlationId: payload._correlationId,
          path: payload.path,
        });
        navigate(MENU_EVENT_ROUTES.onOpenRecentProject);
      },
      context: AppContext.GLOBAL,
      feature: 'project-management',
    });

    const unsubSaveProject = menuCommandDispatcher.registerHandler({
      commandId: 'menu:saveProject',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Save Project', { correlationId: payload._correlationId });
        window.dispatchEvent(new CustomEvent('app:saveProject'));
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'project-management',
    });

    const unsubSaveProjectAs = menuCommandDispatcher.registerHandler({
      commandId: 'menu:saveProjectAs',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Save Project As', {
          correlationId: payload._correlationId,
        });
        window.dispatchEvent(new CustomEvent('app:saveProjectAs'));
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'project-management',
    });

    const unsubImportVideo = menuCommandDispatcher.registerHandler({
      commandId: 'menu:importVideo',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Import Video', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onImportVideo);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'media-library',
    });

    const unsubImportAudio = menuCommandDispatcher.registerHandler({
      commandId: 'menu:importAudio',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Import Audio', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onImportAudio);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'media-library',
    });

    const unsubImportImages = menuCommandDispatcher.registerHandler({
      commandId: 'menu:importImages',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Import Images', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onImportImages);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'media-library',
    });

    const unsubImportDocument = menuCommandDispatcher.registerHandler({
      commandId: 'menu:importDocument',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Import Document', {
          correlationId: payload._correlationId,
        });
        navigate(MENU_EVENT_ROUTES.onImportDocument);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'script-generation',
    });

    const unsubExportVideo = menuCommandDispatcher.registerHandler({
      commandId: 'menu:exportVideo',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Export Video', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onExportVideo);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'export',
    });

    const unsubExportTimeline = menuCommandDispatcher.registerHandler({
      commandId: 'menu:exportTimeline',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Export Timeline', {
          correlationId: payload._correlationId,
        });
        navigate(MENU_EVENT_ROUTES.onExportTimeline);
      },
      context: AppContext.PROJECT_LOADED,
      feature: 'export',
    });

    // Edit Menu Commands
    const unsubFind = menuCommandDispatcher.registerHandler({
      commandId: 'menu:find',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Find', { correlationId: payload._correlationId });
        window.dispatchEvent(new CustomEvent('app:showFind'));
      },
      context: AppContext.GLOBAL,
      feature: 'search',
    });

    const unsubOpenPreferences = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openPreferences',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open Preferences', {
          correlationId: payload._correlationId,
        });
        navigate(MENU_EVENT_ROUTES.onOpenPreferences);
      },
      context: AppContext.GLOBAL,
      feature: 'settings',
    });

    // Tools Menu Commands
    const unsubOpenProviderSettings = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openProviderSettings',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open Provider Settings', {
          correlationId: payload._correlationId,
        });
        navigate(`${MENU_EVENT_ROUTES.onOpenProviderSettings}?tab=providers`);
      },
      context: AppContext.GLOBAL,
      feature: 'settings',
    });

    const unsubOpenFFmpegConfig = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openFFmpegConfig',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open FFmpeg Config', {
          correlationId: payload._correlationId,
        });
        navigate(`${MENU_EVENT_ROUTES.onOpenFFmpegConfig}?tab=ffmpeg`);
      },
      context: AppContext.GLOBAL,
      feature: 'settings',
    });

    const unsubClearCache = menuCommandDispatcher.registerHandler({
      commandId: 'menu:clearCache',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Clear Cache', { correlationId: payload._correlationId });
        window.dispatchEvent(new CustomEvent('app:clearCache'));
      },
      context: AppContext.GLOBAL,
      feature: 'system',
    });

    const unsubViewLogs = menuCommandDispatcher.registerHandler({
      commandId: 'menu:viewLogs',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: View Logs', { correlationId: payload._correlationId });
        navigate(MENU_EVENT_ROUTES.onViewLogs);
      },
      context: AppContext.GLOBAL,
      feature: 'diagnostics',
    });

    const unsubRunDiagnostics = menuCommandDispatcher.registerHandler({
      commandId: 'menu:runDiagnostics',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Run Diagnostics', {
          correlationId: payload._correlationId,
        });
        navigate(MENU_EVENT_ROUTES.onRunDiagnostics);
      },
      context: AppContext.GLOBAL,
      feature: 'diagnostics',
    });

    // Help Menu Commands
    const unsubOpenGettingStarted = menuCommandDispatcher.registerHandler({
      commandId: 'menu:openGettingStarted',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Open Getting Started', {
          correlationId: payload._correlationId,
        });
        navigate(MENU_EVENT_ROUTES.onOpenGettingStarted);
      },
      context: AppContext.GLOBAL,
      feature: 'help',
    });

    const unsubShowKeyboardShortcuts = menuCommandDispatcher.registerHandler({
      commandId: 'menu:showKeyboardShortcuts',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Show Keyboard Shortcuts', {
          correlationId: payload._correlationId,
        });
        window.dispatchEvent(new CustomEvent('app:showKeyboardShortcuts'));
      },
      context: AppContext.GLOBAL,
      feature: 'help',
    });

    const unsubCheckForUpdates = menuCommandDispatcher.registerHandler({
      commandId: 'menu:checkForUpdates',
      handler: (payload: MenuCommandPayload) => {
        loggingService.info('Executing: Check for Updates', {
          correlationId: payload._correlationId,
        });
        window.dispatchEvent(new CustomEvent('app:checkForUpdates'));
      },
      context: AppContext.GLOBAL,
      feature: 'updates',
    });

    // Wire up Electron menu listeners to dispatcher
    unsubscribers.push(
      menu.onNewProject((payload) => menuCommandDispatcher.dispatch('menu:newProject', payload))
    );
    unsubscribers.push(
      menu.onOpenProject((payload) => menuCommandDispatcher.dispatch('menu:openProject', payload))
    );
    unsubscribers.push(
      menu.onOpenRecentProject((payload) =>
        menuCommandDispatcher.dispatch('menu:openRecentProject', payload)
      )
    );
    unsubscribers.push(
      menu.onSaveProject((payload) => menuCommandDispatcher.dispatch('menu:saveProject', payload))
    );
    unsubscribers.push(
      menu.onSaveProjectAs((payload) =>
        menuCommandDispatcher.dispatch('menu:saveProjectAs', payload)
      )
    );
    unsubscribers.push(
      menu.onImportVideo((payload) => menuCommandDispatcher.dispatch('menu:importVideo', payload))
    );
    unsubscribers.push(
      menu.onImportAudio((payload) => menuCommandDispatcher.dispatch('menu:importAudio', payload))
    );
    unsubscribers.push(
      menu.onImportImages((payload) => menuCommandDispatcher.dispatch('menu:importImages', payload))
    );
    unsubscribers.push(
      menu.onImportDocument((payload) =>
        menuCommandDispatcher.dispatch('menu:importDocument', payload)
      )
    );
    unsubscribers.push(
      menu.onExportVideo((payload) => menuCommandDispatcher.dispatch('menu:exportVideo', payload))
    );
    unsubscribers.push(
      menu.onExportTimeline((payload) =>
        menuCommandDispatcher.dispatch('menu:exportTimeline', payload)
      )
    );
    unsubscribers.push(
      menu.onFind((payload) => menuCommandDispatcher.dispatch('menu:find', payload))
    );
    unsubscribers.push(
      menu.onOpenPreferences((payload) =>
        menuCommandDispatcher.dispatch('menu:openPreferences', payload)
      )
    );
    unsubscribers.push(
      menu.onOpenProviderSettings((payload) =>
        menuCommandDispatcher.dispatch('menu:openProviderSettings', payload)
      )
    );
    unsubscribers.push(
      menu.onOpenFFmpegConfig((payload) =>
        menuCommandDispatcher.dispatch('menu:openFFmpegConfig', payload)
      )
    );
    unsubscribers.push(
      menu.onClearCache((payload) => menuCommandDispatcher.dispatch('menu:clearCache', payload))
    );
    unsubscribers.push(
      menu.onViewLogs((payload) => menuCommandDispatcher.dispatch('menu:viewLogs', payload))
    );
    unsubscribers.push(
      menu.onRunDiagnostics((payload) =>
        menuCommandDispatcher.dispatch('menu:runDiagnostics', payload)
      )
    );
    unsubscribers.push(
      menu.onOpenGettingStarted((payload) =>
        menuCommandDispatcher.dispatch('menu:openGettingStarted', payload)
      )
    );
    unsubscribers.push(
      menu.onShowKeyboardShortcuts((payload) =>
        menuCommandDispatcher.dispatch('menu:showKeyboardShortcuts', payload)
      )
    );
    unsubscribers.push(
      menu.onCheckForUpdates((payload) =>
        menuCommandDispatcher.dispatch('menu:checkForUpdates', payload)
      )
    );

    loggingService.info('Menu command system initialized', {
      handlersRegistered: menuCommandDispatcher.getRegisteredCommands().length,
    });

    // Cleanup function
    return () => {
      loggingService.info('Cleaning up menu command system');

      // Unsubscribe from Electron menu events
      unsubscribers.forEach((unsub) => {
        try {
          unsub();
        } catch (error) {
          loggingService.error('Error unsubscribing menu listener', { error });
        }
      });

      // Unregister command handlers
      unsubNewProject();
      unsubOpenProject();
      unsubOpenRecentProject();
      unsubSaveProject();
      unsubSaveProjectAs();
      unsubImportVideo();
      unsubImportAudio();
      unsubImportImages();
      unsubImportDocument();
      unsubExportVideo();
      unsubExportTimeline();
      unsubFind();
      unsubOpenPreferences();
      unsubOpenProviderSettings();
      unsubOpenFFmpegConfig();
      unsubClearCache();
      unsubViewLogs();
      unsubRunDiagnostics();
      unsubOpenGettingStarted();
      unsubShowKeyboardShortcuts();
      unsubCheckForUpdates();
    };
  }, [navigate, addNotification]);
}

// Export the original hook for backward compatibility
export { useElectronMenuEvents } from './useElectronMenuEvents';
