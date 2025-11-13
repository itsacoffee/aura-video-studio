/**
 * Navigation Service Tests
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { navigationService } from '../navigationService';
import type { RouteMetadata } from '../navigationService';

describe('NavigationService', () => {
  beforeEach(() => {
    localStorage.clear();
    navigationService.clearPersistedRoute();
  });

  describe('Route Registration', () => {
    it('should register a single route', () => {
      const route: RouteMetadata = {
        path: '/test',
        title: 'Test Route',
        description: 'Test description',
      };

      navigationService.registerRoute(route);
      const metadata = navigationService.getRouteMeta('/test');

      expect(metadata).toEqual(route);
    });

    it('should register multiple routes', () => {
      const routes: RouteMetadata[] = [
        { path: '/test1', title: 'Test 1' },
        { path: '/test2', title: 'Test 2' },
      ];

      navigationService.registerRoutes(routes);

      expect(navigationService.getRouteMeta('/test1')?.title).toBe('Test 1');
      expect(navigationService.getRouteMeta('/test2')?.title).toBe('Test 2');
    });
  });

  describe('Route Metadata', () => {
    beforeEach(() => {
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
        requiresFirstRun: true,
        requiresFFmpeg: true,
        requiresSettings: true,
      });
    });

    it('should check if route requires first-run', () => {
      expect(navigationService.requiresFirstRun('/test')).toBe(true);
      expect(navigationService.requiresFirstRun('/other')).toBe(false);
    });

    it('should check if route requires FFmpeg', () => {
      expect(navigationService.requiresFFmpeg('/test')).toBe(true);
      expect(navigationService.requiresFFmpeg('/other')).toBe(false);
    });

    it('should check if route requires settings', () => {
      expect(navigationService.requiresSettings('/test')).toBe(true);
      expect(navigationService.requiresSettings('/other')).toBe(false);
    });
  });

  describe('Route Persistence', () => {
    it('should persist route to storage', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      await navigationService.push('/test');

      const persisted = navigationService.getPersistedRoute();
      expect(persisted).toBeTruthy();
      expect(persisted?.path).toBe('/test');
    });

    it('should not persist route when skipPersistence is true', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      await navigationService.push('/test', { skipPersistence: true });

      const persisted = navigationService.getPersistedRoute();
      expect(persisted).toBeNull();
    });

    it('should restore persisted route', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      await navigationService.push('/test');

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).toBe('/test');
    });

    it('should clear persisted route', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      await navigationService.push('/test');
      navigationService.clearPersistedRoute();

      const persisted = navigationService.getPersistedRoute();
      expect(persisted).toBeNull();
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      navigationService.clearPersistedRoute();
    });

    it('should call navigate function on push', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.push('/test');

      expect(result).toBe(true);
      expect(mockNavigate).toHaveBeenCalledWith('/test', {});
    });

    it('should call navigate function on replace', async () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.replace('/test');

      expect(result).toBe(true);
      expect(mockNavigate).toHaveBeenCalledWith('/test', { replace: true });
    });

    it('should handle goBack', () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      navigationService.goBack();

      expect(mockNavigate).toHaveBeenCalledWith(-1);
    });

    it('should handle goForward', () => {
      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      navigationService.goForward();

      expect(mockNavigate).toHaveBeenCalledWith(1);
    });

    it('should return false when navigate is not initialized', async () => {
      // Create a new isolated instance by clearing the navigate function
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const originalNavigate = (navigationService as any).navigate;
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (navigationService as any).navigate = null;

      const result = await navigationService.push('/test');
      expect(result).toBe(false);

      // Restore
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (navigationService as any).navigate = originalNavigate;
    });
  });

  describe('Route Guards', () => {
    it('should allow navigation when no guards', async () => {
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
      });

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.push('/test');
      expect(result).toBe(true);
    });

    it('should allow navigation when guards pass', async () => {
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
        guards: [async () => true],
      });

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.push('/test');
      expect(result).toBe(true);
    });

    it('should block navigation when guard fails', async () => {
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
        guards: [async () => false],
      });

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.push('/test');
      expect(result).toBe(false);
      expect(mockNavigate).not.toHaveBeenCalled();
    });

    it('should bypass guards when bypassGuards is true', async () => {
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
        guards: [async () => false],
      });

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      const result = await navigationService.push('/test', { bypassGuards: true });
      expect(result).toBe(true);
      expect(mockNavigate).toHaveBeenCalled();
    });
  });

  describe('Navigation Listeners', () => {
    beforeEach(() => {
      navigationService.clearPersistedRoute();
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (navigationService as any).navigationListeners = [];
    });

    it('should notify listeners on navigation', async () => {
      const listener = vi.fn();
      navigationService.addNavigationListener(listener);

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      // Register route without guards
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
      });

      await navigationService.push('/test');

      expect(listener).toHaveBeenCalledWith('/test', undefined);
    });

    it('should remove listener', async () => {
      const listener = vi.fn();
      navigationService.addNavigationListener(listener);
      navigationService.removeNavigationListener(listener);

      const mockNavigate = vi.fn();
      navigationService.setNavigate(mockNavigate);

      // Register route without guards
      navigationService.registerRoute({
        path: '/test',
        title: 'Test',
      });

      await navigationService.push('/test');

      expect(listener).not.toHaveBeenCalled();
    });
  });

  describe('Current Path', () => {
    it('should update current path', () => {
      navigationService.updateCurrentPath('/test');
      expect(navigationService.getCurrentPath()).toBe('/test');
    });

    it('should get current route metadata', () => {
      const route: RouteMetadata = {
        path: '/test',
        title: 'Test',
      };
      navigationService.registerRoute(route);
      navigationService.updateCurrentPath('/test');

      const meta = navigationService.getCurrentRouteMeta();
      expect(meta).toEqual(route);
    });
  });
});
