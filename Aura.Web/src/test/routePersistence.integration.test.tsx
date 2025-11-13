/**
 * Route Persistence Integration Tests
 */

import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { NavigationProvider } from '../../contexts/NavigationContext';
import { crashRecoveryService } from '../../services/crashRecoveryService';
import { navigationService } from '../../services/navigationService';

describe('Route Persistence Integration', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    navigationService.clearPersistedRoute();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Route State Persistence', () => {
    it('should persist route on navigation', async () => {
      const TestComponent = () => {
        return (
          <MemoryRouter initialEntries={['/']}>
            <NavigationProvider>
              <div>Test Component</div>
            </NavigationProvider>
          </MemoryRouter>
        );
      };

      render(<TestComponent />);

      await navigationService.push('/dashboard');

      const persisted = navigationService.getPersistedRoute();
      expect(persisted).toBeTruthy();
      expect(persisted?.path).toBe('/dashboard');
      expect(persisted?.timestamp).toBeDefined();
    });

    it('should restore persisted route on app restart', () => {
      localStorage.setItem(
        'aura_last_route',
        JSON.stringify({
          path: '/projects',
          timestamp: Date.now(),
        })
      );

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).toBe('/projects');
    });

    it('should not restore setup route', () => {
      localStorage.setItem(
        'aura_last_route',
        JSON.stringify({
          path: '/setup',
          timestamp: Date.now(),
        })
      );

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).not.toBe('/setup');
      expect(initialRoute).toBe('/');
    });

    it('should not restore onboarding route', () => {
      localStorage.setItem(
        'aura_last_route',
        JSON.stringify({
          path: '/onboarding',
          timestamp: Date.now(),
        })
      );

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).not.toBe('/onboarding');
      expect(initialRoute).toBe('/');
    });
  });

  describe('Safe Mode Override', () => {
    it('should force diagnostics route in safe mode', () => {
      sessionStorage.setItem('aura_session_active', 'true');

      localStorage.setItem(
        'aura_recovery_state',
        JSON.stringify({
          wasCleanShutdown: false,
          consecutiveCrashes: 3,
          crashCount: 3,
          lastCrash: {
            timestamp: Date.now(),
            url: '/projects',
          },
        })
      );

      localStorage.setItem(
        'aura_last_route',
        JSON.stringify({
          path: '/projects',
          timestamp: Date.now(),
        })
      );

      crashRecoveryService.initialize();

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).toBe('/diagnostics');
    });

    it('should restore normal route when not in safe mode', () => {
      sessionStorage.removeItem('aura_session_active');

      localStorage.setItem(
        'aura_recovery_state',
        JSON.stringify({
          wasCleanShutdown: true,
          consecutiveCrashes: 0,
          crashCount: 0,
          lastCrash: null,
        })
      );

      localStorage.setItem(
        'aura_last_route',
        JSON.stringify({
          path: '/projects',
          timestamp: Date.now(),
        })
      );

      crashRecoveryService.initialize();

      const initialRoute = navigationService.getInitialRoute();
      expect(initialRoute).toBe('/projects');
    });
  });

  describe('Route State Serialization', () => {
    it('should persist route with state', async () => {
      await navigationService.push('/editor/123', {
        state: {
          jobId: '123',
          fromWizard: true,
        },
      });

      const persisted = navigationService.getPersistedRoute();
      expect(persisted?.state).toEqual({
        jobId: '123',
        fromWizard: true,
      });
    });

    it('should handle complex state objects', async () => {
      const complexState = {
        project: {
          id: 'proj-123',
          name: 'My Project',
          clips: [{ id: 'clip-1' }, { id: 'clip-2' }],
        },
        metadata: {
          lastModified: new Date().toISOString(),
        },
      };

      await navigationService.push('/editor', {
        state: complexState,
      });

      const persisted = navigationService.getPersistedRoute();
      expect(persisted?.state).toEqual(complexState);
    });
  });

  describe('Wizard Resume', () => {
    it('should resume wizard at last step', async () => {
      await navigationService.push('/create', {
        state: {
          currentStep: 2,
          brief: { topic: 'AI Tutorial' },
          voiceSettings: { provider: 'elevenlabs' },
        },
      });

      const persisted = navigationService.getPersistedRoute();
      expect(persisted?.path).toBe('/create');
      expect(persisted?.state).toMatchObject({
        currentStep: 2,
        brief: { topic: 'AI Tutorial' },
      });
    });
  });

  describe('Project Reopen', () => {
    it('should reopen project editor after restart', async () => {
      await navigationService.push('/editor/proj-456', {
        state: {
          projectId: 'proj-456',
          timeline: {
            clips: [],
            duration: 120,
          },
        },
      });

      const persisted = navigationService.getPersistedRoute();
      expect(persisted?.path).toBe('/editor/proj-456');
      expect(persisted?.state?.projectId).toBe('proj-456');
    });
  });
});
