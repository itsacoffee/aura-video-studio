/**
 * RouteGuard Component Tests
 */

import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { RouteGuard } from '../../components/RouteGuard';
import { navigationService } from '../../services/navigationService';

describe('RouteGuard Component', () => {
  beforeEach(() => {
    navigationService.clearPersistedRoute();
  });

  it('should render children when no guards', async () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
    });

    render(
      <MemoryRouter>
        <RouteGuard path="/test">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('should show loading while checking guards', () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      guards: [async () => new Promise((resolve) => setTimeout(() => resolve(true), 100))],
    });

    render(
      <MemoryRouter>
        <RouteGuard path="/test">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    expect(screen.getByText('Checking prerequisites...')).toBeInTheDocument();
  });

  it('should render children when guards pass', async () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      guards: [async () => true],
    });

    render(
      <MemoryRouter>
        <RouteGuard path="/test">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('should redirect when guards fail', async () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      guards: [async () => false],
    });

    render(
      <MemoryRouter initialEntries={['/test']}>
        <RouteGuard path="/test" fallbackRoute="/login">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });
  });

  it('should redirect to /setup when requiresFirstRun fails', async () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      requiresFirstRun: true,
      guards: [async () => false],
    });

    render(
      <MemoryRouter initialEntries={['/test']}>
        <RouteGuard path="/test">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });
  });

  it('should not show loading when showLoading is false', async () => {
    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      guards: [async () => new Promise((resolve) => setTimeout(() => resolve(true), 100))],
    });

    render(
      <MemoryRouter>
        <RouteGuard path="/test" showLoading={false}>
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    expect(screen.queryByText('Checking prerequisites...')).not.toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('should handle guard errors gracefully', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    navigationService.registerRoute({
      path: '/test',
      title: 'Test',
      guards: [
        async () => {
          throw new Error('Guard failed');
        },
      ],
    });

    render(
      <MemoryRouter initialEntries={['/test']}>
        <RouteGuard path="/test" fallbackRoute="/error">
          <div>Protected Content</div>
        </RouteGuard>
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
    });

    consoleError.mockRestore();
  });
});
