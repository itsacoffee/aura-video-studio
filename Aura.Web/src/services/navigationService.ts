/**
 * Navigation Service
 * Centralized navigation abstraction for Electron routing
 *
 * Provides:
 * - Typed route definitions with metadata
 * - Route guards support
 * - Navigation state persistence
 * - Safe mode routing override
 */

import type { NavigateFunction, NavigateOptions } from 'react-router-dom';
import { crashRecoveryService } from './crashRecoveryService';
import { loggingService } from './loggingService';

/**
 * Route guard function that returns true if navigation is allowed
 * Can be async to check prerequisites
 */
export type RouteGuard = () => boolean | Promise<boolean>;

/**
 * Route metadata including guards and requirements
 */
export interface RouteMetadata {
  path: string;
  title: string;
  description?: string;
  requiresFirstRun?: boolean;
  requiresFFmpeg?: boolean;
  requiresSettings?: boolean;
  guards?: RouteGuard[];
  icon?: string;
}

/**
 * Route state that can be serialized and persisted
 */
export interface RouteState {
  path: string;
  state?: Record<string, unknown>;
  timestamp: number;
}

/**
 * Navigation options extending React Router's options
 */
export interface NavigationOptions extends NavigateOptions {
  skipPersistence?: boolean;
  bypassGuards?: boolean;
}

const STORAGE_KEY = 'aura_last_route';
const SAFE_MODE_ROUTE = '/diagnostics';
const DEFAULT_ROUTE = '/';
const GUARD_TIMEOUT_MS = 250;

/**
 * Navigation Service class
 * Wraps React Router's navigate function with additional functionality
 */
class NavigationService {
  private navigate: NavigateFunction | null = null;
  private currentPath: string = DEFAULT_ROUTE;
  private routeMetadata: Map<string, RouteMetadata> = new Map();
  private navigationListeners: Array<(path: string, state?: RouteState) => void> = [];
  private guardCheckInProgress = false;

  /**
   * Initialize the navigation service with React Router's navigate function
   * Must be called inside Router context
   */
  setNavigate(navigateFn: NavigateFunction): void {
    this.navigate = navigateFn;
    loggingService.info('Navigation service initialized', 'NavigationService', 'setNavigate');
  }

  /**
   * Register route metadata
   */
  registerRoute(metadata: RouteMetadata): void {
    this.routeMetadata.set(metadata.path, metadata);
  }

  /**
   * Register multiple routes at once
   */
  registerRoutes(routes: RouteMetadata[]): void {
    routes.forEach((route) => this.registerRoute(route));
    loggingService.info(
      `Registered ${routes.length} routes`,
      'NavigationService',
      'registerRoutes'
    );
  }

  /**
   * Navigate to a path
   */
  async push(to: string | number, options: NavigationOptions = {}): Promise<boolean> {
    if (typeof to === 'number') {
      if (this.navigate) {
        this.navigate(to);
        return true;
      }
      return false;
    }

    try {
      const canNavigate = options.bypassGuards || (await this.checkGuards(to));

      if (!canNavigate) {
        loggingService.warn(`Navigation to ${to} blocked by guards`, 'NavigationService', 'push');
        return false;
      }

      if (!this.navigate) {
        loggingService.error(
          'Navigate function not initialized',
          undefined,
          'NavigationService',
          'push'
        );
        return false;
      }

      this.navigate(to, options);
      this.currentPath = to;

      if (!options.skipPersistence) {
        this.persistRoute(to, options.state as Record<string, unknown> | undefined);
      }

      this.notifyListeners(
        to,
        options.state
          ? {
              path: to,
              state: options.state as Record<string, unknown>,
              timestamp: Date.now(),
            }
          : undefined
      );

      loggingService.info(`Navigated to ${to}`, 'NavigationService', 'push');
      return true;
    } catch (error) {
      loggingService.error('Navigation failed', error as Error, 'NavigationService', 'push', {
        to,
      });
      return false;
    }
  }

  /**
   * Replace current route (doesn't add to history)
   */
  async replace(to: string, options: NavigationOptions = {}): Promise<boolean> {
    return this.push(to, { ...options, replace: true });
  }

  /**
   * Go back in history
   */
  goBack(): void {
    if (this.navigate) {
      this.navigate(-1);
    }
  }

  /**
   * Go forward in history
   */
  goForward(): void {
    if (this.navigate) {
      this.navigate(1);
    }
  }

  /**
   * Get current route metadata
   */
  getCurrentRouteMeta(): RouteMetadata | null {
    return this.routeMetadata.get(this.currentPath) || null;
  }

  /**
   * Get route metadata by path
   */
  getRouteMeta(path: string): RouteMetadata | null {
    return this.routeMetadata.get(path) || null;
  }

  /**
   * Get current path
   */
  getCurrentPath(): string {
    return this.currentPath;
  }

  /**
   * Update current path (called by router)
   */
  updateCurrentPath(path: string): void {
    this.currentPath = path;
  }

  /**
   * Check if navigation to a route is allowed based on guards
   */
  private async checkGuards(path: string): Promise<boolean> {
    if (this.guardCheckInProgress) {
      loggingService.warn('Guard check already in progress', 'NavigationService', 'checkGuards');
      return false;
    }

    const metadata = this.routeMetadata.get(path);
    if (!metadata || !metadata.guards || metadata.guards.length === 0) {
      return true;
    }

    this.guardCheckInProgress = true;

    try {
      const timeoutPromise = new Promise<boolean>((resolve) => {
        setTimeout(() => {
          loggingService.warn(
            `Guard check timeout for ${path}`,
            'NavigationService',
            'checkGuards'
          );
          resolve(false);
        }, GUARD_TIMEOUT_MS);
      });

      const guardsPromise = this.executeGuards(metadata.guards);
      return await Promise.race([guardsPromise, timeoutPromise]);
    } finally {
      this.guardCheckInProgress = false;
    }
  }

  /**
   * Execute all guards for a route
   */
  private async executeGuards(guards: RouteGuard[]): Promise<boolean> {
    for (const guard of guards) {
      try {
        const result = await guard();
        if (!result) {
          return false;
        }
      } catch (error) {
        loggingService.error(
          'Guard execution failed',
          error as Error,
          'NavigationService',
          'executeGuards'
        );
        return false;
      }
    }
    return true;
  }

  /**
   * Persist current route to storage
   */
  private persistRoute(path: string, state?: Record<string, unknown>): void {
    try {
      const routeState: RouteState = {
        path,
        state,
        timestamp: Date.now(),
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(routeState));
    } catch (error) {
      loggingService.error(
        'Failed to persist route',
        error as Error,
        'NavigationService',
        'persistRoute'
      );
    }
  }

  /**
   * Get persisted route from storage
   */
  getPersistedRoute(): RouteState | null {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (!saved) {
        return null;
      }
      return JSON.parse(saved) as RouteState;
    } catch (error) {
      loggingService.error(
        'Failed to get persisted route',
        error as Error,
        'NavigationService',
        'getPersistedRoute'
      );
      return null;
    }
  }

  /**
   * Restore last route from storage
   * Returns route to navigate to, considering safe mode overrides
   */
  getInitialRoute(): string {
    try {
      const recoveryState = crashRecoveryService.getRecoveryState();

      if (recoveryState && crashRecoveryService.shouldShowRecoveryScreen()) {
        loggingService.warn(
          'Safe mode triggered, forcing diagnostics route',
          'NavigationService',
          'getInitialRoute'
        );
        return SAFE_MODE_ROUTE;
      }

      const persisted = this.getPersistedRoute();
      if (
        persisted &&
        persisted.path &&
        persisted.path !== '/setup' &&
        persisted.path !== '/onboarding'
      ) {
        loggingService.info(
          `Restoring route: ${persisted.path}`,
          'NavigationService',
          'getInitialRoute'
        );
        return persisted.path;
      }

      return DEFAULT_ROUTE;
    } catch (error) {
      loggingService.error(
        'Failed to get initial route',
        error as Error,
        'NavigationService',
        'getInitialRoute'
      );
      return DEFAULT_ROUTE;
    }
  }

  /**
   * Clear persisted route (useful for testing or reset)
   */
  clearPersistedRoute(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
      loggingService.info('Persisted route cleared', 'NavigationService', 'clearPersistedRoute');
    } catch (error) {
      loggingService.error(
        'Failed to clear persisted route',
        error as Error,
        'NavigationService',
        'clearPersistedRoute'
      );
    }
  }

  /**
   * Add navigation listener
   */
  addNavigationListener(listener: (path: string, state?: RouteState) => void): void {
    this.navigationListeners.push(listener);
  }

  /**
   * Remove navigation listener
   */
  removeNavigationListener(listener: (path: string, state?: RouteState) => void): void {
    this.navigationListeners = this.navigationListeners.filter((l) => l !== listener);
  }

  /**
   * Notify all listeners of navigation
   */
  private notifyListeners(path: string, state?: RouteState): void {
    this.navigationListeners.forEach((listener) => {
      try {
        listener(path, state);
      } catch (error) {
        loggingService.error(
          'Navigation listener error',
          error as Error,
          'NavigationService',
          'notifyListeners'
        );
      }
    });
  }

  /**
   * Check if a route requires first-run completion
   */
  requiresFirstRun(path: string): boolean {
    const metadata = this.routeMetadata.get(path);
    return metadata?.requiresFirstRun ?? false;
  }

  /**
   * Check if a route requires FFmpeg
   */
  requiresFFmpeg(path: string): boolean {
    const metadata = this.routeMetadata.get(path);
    return metadata?.requiresFFmpeg ?? false;
  }

  /**
   * Check if a route requires settings
   */
  requiresSettings(path: string): boolean {
    const metadata = this.routeMetadata.get(path);
    return metadata?.requiresSettings ?? false;
  }
}

export const navigationService = new NavigationService();
