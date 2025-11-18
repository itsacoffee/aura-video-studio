/**
 * Tests for protected routes and route guards
 * Requirement 4: Add route guard test - if auth required, verify redirect to login happens
 */

import { render, screen, waitFor } from '@testing-library/react';
import { HashRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ProtectedRoute } from '../components/ProtectedRoute';
// Mock the useAuth hook
vi.mock('../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

// Mock loggingService
vi.mock('../services/loggingService', () => ({
  loggingService: {
    warn: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

import { useAuth } from '../hooks/useAuth';

const mockedUseAuth = vi.mocked(useAuth);

describe('Protected Routes and Route Guards', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should redirect to login when user is not authenticated', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      isLoading: false,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
      expect(window.location.hash).toContain('/login');
    });
  });

  it('should render protected content when user is authenticated', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: '1', name: 'Test User', email: 'test@example.com', role: 'user' },
      isLoading: false,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div>Login Page</div>} />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('should show loading state while checking authentication', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      isLoading: true,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Loading...')).toBeInTheDocument();
    });
  });

  it('should show custom fallback during loading when provided', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      isLoading: true,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute fallback={<div>Custom Loading</div>}>
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Custom Loading')).toBeInTheDocument();
    });
  });

  it('should deny access when user lacks required role', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: '1', name: 'Test User', email: 'test@example.com', role: 'user' },
      isLoading: false,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute requiredRole="admin">
                <div>Admin Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
      expect(screen.getByText('Access Denied')).toBeInTheDocument();
      expect(
        screen.getByText("You don't have permission to access this page.")
      ).toBeInTheDocument();
    });
  });

  it('should allow access when user has required role', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: { id: '1', name: 'Admin User', email: 'admin@example.com', role: 'admin' },
      isLoading: false,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute requiredRole="admin">
                <div>Admin Content</div>
              </ProtectedRoute>
            }
          />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Admin Content')).toBeInTheDocument();
    });
  });

  it('should use custom redirect path when provided', async () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: false,
      user: null,
      isLoading: false,
      refreshUser: vi.fn(),
      login: vi.fn(),
      logout: vi.fn(),
    });

    render(
      <HashRouter>
        <Routes>
          <Route
            path="/"
            element={
              <ProtectedRoute redirectTo="/custom-login">
                <div>Protected Content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/custom-login" element={<div>Custom Login Page</div>} />
        </Routes>
      </HashRouter>
    );

    await waitFor(() => {
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument();
      expect(window.location.hash).toContain('/custom-login');
    });
  });
});
