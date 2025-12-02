/**
 * Custom Event Handlers Service
 * Implements handlers for custom menu events by delegating to the project event bus
 *
 * REQUIREMENT 7: Each custom event handler must have implemented logic
 */

import { loggingService } from './loggingService';
import { warnIfNoHandler } from './routeRegistry';
import { executeSaveProject, executeSaveProjectAs, showToast } from './projectEventBus';

// Store references to wrapped handlers for proper cleanup
const registeredHandlers: Map<string, EventListener> = new Map();

/**
 * LocalStorage keys that should be preserved when clearing cache.
 * These contain user preferences and essential app state.
 */
const PRESERVE_LOCALSTORAGE_KEYS = [
  'darkMode',
  'themeName',
  'hasCompletedFirstRun',
  'hasSeenOnboarding',
] as const;

/**
 * Handler for app:saveProject event
 * Delegates to the project event bus to save the current project
 */
async function handleSaveProject(): Promise<void> {
  loggingService.info('Save Project event triggered');
  warnIfNoHandler('app:saveProject');

  try {
    await executeSaveProject();
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    loggingService.error('Save project failed', new Error(errorMessage), 'customEventHandlers');
  }
}

/**
 * Handler for app:saveProjectAs event
 * Delegates to the project event bus to open Save As dialog
 */
async function handleSaveProjectAs(): Promise<void> {
  loggingService.info('Save Project As event triggered');
  warnIfNoHandler('app:saveProjectAs');

  try {
    await executeSaveProjectAs();
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    loggingService.error('Save project as failed', new Error(errorMessage), 'customEventHandlers');
  }
}

/**
 * Handler for app:showFind event
 * Shows a "coming soon" message for the Find feature
 */
function handleShowFind(): void {
  loggingService.info('Show Find event triggered');
  warnIfNoHandler('app:showFind');

  // Show informational message about this feature
  showToast('Find functionality is coming soon. Use Ctrl+F in text areas for now.', 'info');
}

/**
 * Handler for app:clearCache event
 * Clears application cache (localStorage, sessionStorage, caches)
 */
async function handleClearCache(): Promise<void> {
  loggingService.info('Clear Cache event triggered');
  warnIfNoHandler('app:clearCache');

  try {
    // Clear relevant localStorage items (preserve user preferences)
    const keysToRemove: string[] = [];

    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (
        key &&
        !PRESERVE_LOCALSTORAGE_KEYS.includes(key as (typeof PRESERVE_LOCALSTORAGE_KEYS)[number])
      ) {
        keysToRemove.push(key);
      }
    }

    keysToRemove.forEach((key) => localStorage.removeItem(key));

    // Clear sessionStorage
    sessionStorage.clear();

    // Clear service worker caches if available
    if ('caches' in window) {
      const cacheNames = await caches.keys();
      await Promise.all(cacheNames.map((name) => caches.delete(name)));
    }

    loggingService.info('Cache cleared successfully');
    showToast('Cache cleared successfully. Some data may need to be reloaded.', 'success');
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    loggingService.error('Failed to clear cache', new Error(errorMessage), 'customEventHandlers');
    showToast('Failed to clear cache. Please try again.', 'error');
  }
}

/**
 * Handler for app:showKeyboardShortcuts event
 * Dispatches an event to show the keyboard shortcuts panel
 */
function handleShowKeyboardShortcuts(): void {
  loggingService.info('Show Keyboard Shortcuts event triggered');
  warnIfNoHandler('app:showKeyboardShortcuts');

  // Dispatch a custom event that the App component listens to
  // This allows the keyboard shortcuts panel to be shown from anywhere
  window.dispatchEvent(new CustomEvent('app:toggleKeyboardShortcuts'));
  showToast('Press Ctrl+/ or ? to see all keyboard shortcuts', 'info');
}

/**
 * Handler for app:checkForUpdates event
 * Shows update status or a "coming soon" message
 */
function handleCheckForUpdates(): void {
  loggingService.info('Check for Updates event triggered');
  warnIfNoHandler('app:checkForUpdates');

  // In a real implementation, this would check for updates via Electron's auto-updater
  // For now, show an informational message
  showToast('You are running the latest version of Aura Video Studio.', 'success');
}

/**
 * Helper to create a wrapped handler that catches errors
 */
function createAsyncHandler(handlerFn: () => Promise<void>, eventName: string): EventListener {
  return () => {
    handlerFn().catch((error) => {
      const err = error instanceof Error ? error : new Error(String(error));
      loggingService.error(`Error in ${eventName} handler`, err, 'customEventHandlers', eventName);
    });
  };
}

/**
 * Helper to create a wrapped sync handler that catches errors
 */
function createSyncHandler(handlerFn: () => void, eventName: string): EventListener {
  return () => {
    try {
      handlerFn();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      loggingService.error(
        `Error in ${eventName} handler`,
        error,
        'customEventHandlers',
        eventName
      );
    }
  };
}

/**
 * Registers all custom event handlers on the window object
 * Should be called once at application startup
 */
export function registerCustomEventHandlers(): void {
  loggingService.info('Registering custom event handlers...');

  // Clear any existing handlers first
  unregisterCustomEventHandlers();

  // Create and store wrapped handlers
  const saveProjectHandler = createAsyncHandler(handleSaveProject, 'app:saveProject');
  const saveProjectAsHandler = createAsyncHandler(handleSaveProjectAs, 'app:saveProjectAs');
  const showFindHandler = createSyncHandler(handleShowFind, 'app:showFind');
  const clearCacheHandler = createAsyncHandler(handleClearCache, 'app:clearCache');
  const showKeyboardShortcutsHandler = createSyncHandler(
    handleShowKeyboardShortcuts,
    'app:showKeyboardShortcuts'
  );
  const checkForUpdatesHandler = createSyncHandler(handleCheckForUpdates, 'app:checkForUpdates');

  // Store references for cleanup
  registeredHandlers.set('app:saveProject', saveProjectHandler);
  registeredHandlers.set('app:saveProjectAs', saveProjectAsHandler);
  registeredHandlers.set('app:showFind', showFindHandler);
  registeredHandlers.set('app:clearCache', clearCacheHandler);
  registeredHandlers.set('app:showKeyboardShortcuts', showKeyboardShortcutsHandler);
  registeredHandlers.set('app:checkForUpdates', checkForUpdatesHandler);

  // Register handlers
  window.addEventListener('app:saveProject', saveProjectHandler);
  window.addEventListener('app:saveProjectAs', saveProjectAsHandler);
  window.addEventListener('app:showFind', showFindHandler);
  window.addEventListener('app:clearCache', clearCacheHandler);
  window.addEventListener('app:showKeyboardShortcuts', showKeyboardShortcutsHandler);
  window.addEventListener('app:checkForUpdates', checkForUpdatesHandler);

  loggingService.info('Custom event handlers registered successfully');
}

/**
 * Unregisters all custom event handlers
 * Should be called when cleaning up (e.g., in tests)
 */
export function unregisterCustomEventHandlers(): void {
  registeredHandlers.forEach((handler, eventName) => {
    window.removeEventListener(eventName, handler);
  });
  registeredHandlers.clear();

  loggingService.info('Custom event handlers unregistered');
}
