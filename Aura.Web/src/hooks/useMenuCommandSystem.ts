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
import type { MenuCommandPayload } from '../services/menuCommandDispatcher';
import { AppContext, menuCommandDispatcher } from '../services/menuCommandDispatcher';
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
  // CRITICAL FIX: useNavigate will throw if router context is not available
  // This is expected behavior - React Router will handle it and ErrorBoundary will catch it
  // We wrap the effect logic in try-catch instead to prevent crashes during initialization
  const navigate = useNavigate();
  const { addNotification } = useNotificationStore();

  useEffect(() => {
    // CRITICAL FIX: Track all cleanup functions to ensure proper cleanup even on partial failure
    const handlerCleanupFunctions: Array<() => void> = [];
    const menuListenerUnsubscribers: Array<() => void> = [];
    let toastHandlerSet = false;

    // CRITICAL FIX: Wrap initialization in try-catch to prevent crashes during wizard-to-app transition
    try {
      // Only run in Electron environment
      const menuApi = window.aura?.menu ?? window.electron?.menu;
      if (!menuApi) {
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
      toastHandlerSet = true;

      const menu = menuApi;

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
      handlerCleanupFunctions.push(unsubNewProject);

      const unsubOpenProject = menuCommandDispatcher.registerHandler({
        commandId: 'menu:openProject',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Open Project', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onOpenProject);
        },
        context: AppContext.GLOBAL,
        feature: 'project-management',
      });
      handlerCleanupFunctions.push(unsubOpenProject);

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
      handlerCleanupFunctions.push(unsubOpenRecentProject);

      const unsubSaveProject = menuCommandDispatcher.registerHandler({
        commandId: 'menu:saveProject',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Save Project', { correlationId: payload._correlationId });
          window.dispatchEvent(new CustomEvent('app:saveProject'));
        },
        context: AppContext.PROJECT_LOADED,
        feature: 'project-management',
      });
      handlerCleanupFunctions.push(unsubSaveProject);

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
      handlerCleanupFunctions.push(unsubSaveProjectAs);

      const unsubImportVideo = menuCommandDispatcher.registerHandler({
        commandId: 'menu:importVideo',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Import Video', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onImportVideo);
        },
        context: AppContext.PROJECT_LOADED,
        feature: 'media-library',
      });
      handlerCleanupFunctions.push(unsubImportVideo);

      const unsubImportAudio = menuCommandDispatcher.registerHandler({
        commandId: 'menu:importAudio',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Import Audio', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onImportAudio);
        },
        context: AppContext.PROJECT_LOADED,
        feature: 'media-library',
      });
      handlerCleanupFunctions.push(unsubImportAudio);

      const unsubImportImages = menuCommandDispatcher.registerHandler({
        commandId: 'menu:importImages',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Import Images', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onImportImages);
        },
        context: AppContext.PROJECT_LOADED,
        feature: 'media-library',
      });
      handlerCleanupFunctions.push(unsubImportImages);

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
      handlerCleanupFunctions.push(unsubImportDocument);

      const unsubExportVideo = menuCommandDispatcher.registerHandler({
        commandId: 'menu:exportVideo',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Export Video', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onExportVideo);
        },
        context: AppContext.PROJECT_LOADED,
        feature: 'export',
      });
      handlerCleanupFunctions.push(unsubExportVideo);

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
      handlerCleanupFunctions.push(unsubExportTimeline);

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
      handlerCleanupFunctions.push(unsubFind);

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
      handlerCleanupFunctions.push(unsubOpenPreferences);

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
      handlerCleanupFunctions.push(unsubOpenProviderSettings);

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
      handlerCleanupFunctions.push(unsubOpenFFmpegConfig);

      const unsubClearCache = menuCommandDispatcher.registerHandler({
        commandId: 'menu:clearCache',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: Clear Cache', { correlationId: payload._correlationId });
          window.dispatchEvent(new CustomEvent('app:clearCache'));
        },
        context: AppContext.GLOBAL,
        feature: 'system',
      });
      handlerCleanupFunctions.push(unsubClearCache);

      const unsubViewLogs = menuCommandDispatcher.registerHandler({
        commandId: 'menu:viewLogs',
        handler: (payload: MenuCommandPayload) => {
          loggingService.info('Executing: View Logs', { correlationId: payload._correlationId });
          navigate(MENU_EVENT_ROUTES.onViewLogs);
        },
        context: AppContext.GLOBAL,
        feature: 'diagnostics',
      });
      handlerCleanupFunctions.push(unsubViewLogs);

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
      handlerCleanupFunctions.push(unsubRunDiagnostics);

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
      handlerCleanupFunctions.push(unsubOpenGettingStarted);

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
      handlerCleanupFunctions.push(unsubShowKeyboardShortcuts);

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
      handlerCleanupFunctions.push(unsubCheckForUpdates);

      // Wire up Electron menu listeners to dispatcher
      menuListenerUnsubscribers.push(
        menu.onNewProject(() => void menuCommandDispatcher.dispatch('menu:newProject', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onOpenProject(() => void menuCommandDispatcher.dispatch('menu:openProject', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onOpenRecentProject(
          (payload) => void menuCommandDispatcher.dispatch('menu:openRecentProject', payload)
        )
      );
      menuListenerUnsubscribers.push(
        menu.onSaveProject(() => void menuCommandDispatcher.dispatch('menu:saveProject', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onSaveProjectAs(() => void menuCommandDispatcher.dispatch('menu:saveProjectAs', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onImportVideo(() => void menuCommandDispatcher.dispatch('menu:importVideo', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onImportAudio(() => void menuCommandDispatcher.dispatch('menu:importAudio', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onImportImages(() => void menuCommandDispatcher.dispatch('menu:importImages', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onImportDocument(() => void menuCommandDispatcher.dispatch('menu:importDocument', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onExportVideo(() => void menuCommandDispatcher.dispatch('menu:exportVideo', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onExportTimeline(() => void menuCommandDispatcher.dispatch('menu:exportTimeline', {}))
      );
      menuListenerUnsubscribers.push(menu.onFind(() => void menuCommandDispatcher.dispatch('menu:find', {})));
      menuListenerUnsubscribers.push(
        menu.onOpenPreferences(() => void menuCommandDispatcher.dispatch('menu:openPreferences', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onOpenProviderSettings(
          () => void menuCommandDispatcher.dispatch('menu:openProviderSettings', {})
        )
      );
      menuListenerUnsubscribers.push(
        menu.onOpenFFmpegConfig(
          () => void menuCommandDispatcher.dispatch('menu:openFFmpegConfig', {})
        )
      );
      menuListenerUnsubscribers.push(
        menu.onClearCache(() => void menuCommandDispatcher.dispatch('menu:clearCache', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onViewLogs(() => void menuCommandDispatcher.dispatch('menu:viewLogs', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onRunDiagnostics(() => void menuCommandDispatcher.dispatch('menu:runDiagnostics', {}))
      );
      menuListenerUnsubscribers.push(
        menu.onOpenGettingStarted(
          () => void menuCommandDispatcher.dispatch('menu:openGettingStarted', {})
        )
      );
      menuListenerUnsubscribers.push(
        menu.onShowKeyboardShortcuts(
          () => void menuCommandDispatcher.dispatch('menu:showKeyboardShortcuts', {})
        )
      );
      menuListenerUnsubscribers.push(
        menu.onCheckForUpdates(() => void menuCommandDispatcher.dispatch('menu:checkForUpdates', {}))
      );

      loggingService.info('Menu command system initialized', {
        handlersRegistered: menuCommandDispatcher.getRegisteredCommands().length,
      });

      // Cleanup function
      return () => {
        loggingService.info('Cleaning up menu command system');

        // Unsubscribe from Electron menu events
        menuListenerUnsubscribers.forEach((unsub) => {
          try {
            unsub();
          } catch (error) {
            loggingService.error('Error unsubscribing menu listener', { error });
          }
        });

        // Unregister command handlers
        handlerCleanupFunctions.forEach((cleanup) => {
          try {
            cleanup();
          } catch (error) {
            loggingService.error('Error unregistering command handler', { error });
          }
        });

        // Clear toast handler if it was set
        if (toastHandlerSet) {
          try {
            menuCommandDispatcher.setToastHandler(null);
          } catch (error) {
            loggingService.error('Error clearing toast handler', { error });
          }
        }
      };
    } catch (error) {
      // CRITICAL FIX: Clean up any handlers that were registered before the error
      // This prevents handler leaks when initialization fails partway through
      console.error('[useMenuCommandSystem] Failed to initialize menu command system:', error);
      loggingService.error('Failed to initialize menu command system', { error });

      // Clean up any handlers that were successfully registered before the error
      handlerCleanupFunctions.forEach((cleanup) => {
        try {
          cleanup();
        } catch (cleanupError) {
          loggingService.error('Error cleaning up handler after initialization failure', {
            error: cleanupError,
          });
        }
      });

      // Clean up any menu listeners that were registered before the error
      menuListenerUnsubscribers.forEach((unsub) => {
        try {
          unsub();
        } catch (cleanupError) {
          loggingService.error('Error cleaning up menu listener after initialization failure', {
            error: cleanupError,
          });
        }
      });

      // Clear toast handler if it was set
      if (toastHandlerSet) {
        try {
          menuCommandDispatcher.setToastHandler(null);
        } catch (cleanupError) {
          loggingService.error('Error clearing toast handler after initialization failure', {
            error: cleanupError,
          });
        }
      }

      // Return empty cleanup function - all cleanup already done above
      return () => {
        // No-op cleanup - handlers already cleaned up in catch block
      };
    }
  }, [navigate, addNotification]);
}

// Export the original hook for backward compatibility
export { useElectronMenuEvents } from './useElectronMenuEvents';
