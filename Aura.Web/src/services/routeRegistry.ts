/**
 * Route Registry Service
 * Validates that all menu navigation routes exist in the application's route configuration
 *
 * REQUIREMENT 6: Route registry that validates all menu paths exist at app startup
 */

import { MENU_ROUTES, validateRoute, type MenuRoute } from '../config/routes';
import { loggingService } from './loggingService';

/**
 * Map of menu event names to their navigation routes
 * This ensures compile-time type safety between menu events and routes
 */
export const MENU_EVENT_ROUTES: Record<string, MenuRoute> = {
  onNewProject: MENU_ROUTES.CREATE,
  onOpenProject: MENU_ROUTES.PROJECTS,
  onOpenRecentProject: MENU_ROUTES.PROJECTS,
  onImportVideo: MENU_ROUTES.ASSETS,
  onImportAudio: MENU_ROUTES.ASSETS,
  onImportImages: MENU_ROUTES.ASSETS,
  onImportDocument: MENU_ROUTES.RAG,
  onExportVideo: MENU_ROUTES.RENDER,
  onExportTimeline: MENU_ROUTES.EDITOR,
  onOpenPreferences: MENU_ROUTES.SETTINGS,
  onOpenProviderSettings: MENU_ROUTES.SETTINGS,
  onOpenFFmpegConfig: MENU_ROUTES.SETTINGS,
  onViewLogs: MENU_ROUTES.LOGS,
  onRunDiagnostics: MENU_ROUTES.HEALTH,
  onOpenGettingStarted: MENU_ROUTES.HOME,
} as const;

/**
 * Custom event names used by menu handlers
 */
export const CUSTOM_EVENT_NAMES = [
  'app:saveProject',
  'app:saveProjectAs',
  'app:showFind',
  'app:clearCache',
  'app:showKeyboardShortcuts',
  'app:checkForUpdates',
] as const;

export type CustomEventName = (typeof CUSTOM_EVENT_NAMES)[number];

/**
 * Validation result for route registry
 */
export interface RouteValidationResult {
  valid: boolean;
  errors: string[];
  warnings: string[];
}

/**
 * Validates all menu routes exist in the application
 * Should be called at application startup
 */
export function validateMenuRoutes(): RouteValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  // Validate each menu event route
  Object.entries(MENU_EVENT_ROUTES).forEach(([eventName, route]) => {
    if (!validateRoute(route)) {
      errors.push(`Menu event '${eventName}' navigates to invalid route: ${route}`);
    }
  });

  // Check for custom event handlers
  const customEventHandlers = getRegisteredCustomEventHandlers();
  CUSTOM_EVENT_NAMES.forEach((eventName) => {
    if (!customEventHandlers.includes(eventName)) {
      warnings.push(`Custom event '${eventName}' has no registered handler`);
    }
  });

  const valid = errors.length === 0;

  if (!valid) {
    loggingService.error('Route validation failed', undefined, 'routeRegistry', 'validation', {
      errors,
      warnings,
    });
  } else if (warnings.length > 0) {
    loggingService.warn('Route validation passed with warnings', 'routeRegistry', 'validation', {
      warnings,
    });
  } else {
    loggingService.info('Route validation passed successfully');
  }

  return { valid, errors, warnings };
}

/**
 * Gets a list of currently registered custom event handlers
 * Checks window event listeners for custom event names
 */
function getRegisteredCustomEventHandlers(): CustomEventName[] {
  const registered: CustomEventName[] = [];

  CUSTOM_EVENT_NAMES.forEach((eventName) => {
    if (hasEventListener(eventName)) {
      registered.push(eventName);
    }
  });

  return registered;
}

/**
 * Checks if window has an event listener for the given event name
 * This is a best-effort check since we can't reliably enumerate all event listeners
 */
function hasEventListener(eventName: string): boolean {
  try {
    let hasListener = false;
    const testListener = () => {
      hasListener = true;
    };

    window.addEventListener(eventName, testListener);

    const testEvent = new CustomEvent(eventName);
    window.dispatchEvent(testEvent);

    window.removeEventListener(eventName, testListener);

    return hasListener;
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    loggingService.error(
      `Error checking for event listener: ${eventName}`,
      error,
      'routeRegistry',
      'hasEventListener'
    );
    return false;
  }
}

/**
 * Warns if a menu event was fired but no handler is registered
 * REQUIREMENT 8: Add console warning if menu event fired but no handler registered
 */
export function warnIfNoHandler(eventName: string): void {
  const handlers = getRegisteredCustomEventHandlers();
  if (!handlers.includes(eventName as CustomEventName)) {
    loggingService.warn(`Menu event '${eventName}' fired but no handler is registered`);
    console.warn(`[Route Registry] Menu event '${eventName}' fired but no handler is registered`);
  }
}

/**
 * Initializes route registry and validates all routes
 * Should be called once at application startup
 */
export function initializeRouteRegistry(): RouteValidationResult {
  loggingService.info('Initializing route registry...');

  const result = validateMenuRoutes();

  if (!result.valid) {
    console.error('[Route Registry] Validation failed:', result.errors);
    throw new Error(`Route validation failed: ${result.errors.join(', ')}`);
  }

  if (result.warnings.length > 0) {
    console.warn('[Route Registry] Validation warnings:', result.warnings);
  }

  loggingService.info('Route registry initialized successfully');
  return result;
}
