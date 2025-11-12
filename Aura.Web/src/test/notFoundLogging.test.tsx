/**
 * Tests for 404 NotFound route logging
 * Requirement 5: 404 route must log which path was attempted before redirecting
 * Requirement 6: Forbidden - Catch-all route that renders generic NotFound without logging
 */

import { render, screen } from '@testing-library/react';
import { HashRouter, Route, Routes } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NotFoundPage } from '../pages/NotFoundPage';

// Mock loggingService
const mockWarn = vi.fn();
vi.mock('../services/loggingService', () => ({
  loggingService: {
    warn: mockWarn,
    error: vi.fn(),
    info: vi.fn(),
  },
}));

describe('NotFoundPage Logging', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should log the attempted path when NotFoundPage mounts', () => {
    render(
      <HashRouter>
        <Routes>
          <Route path="/non-existent-path" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(mockWarn).toHaveBeenCalledWith(
      '404 Page Not Found: User attempted to access non-existent route',
      'NotFoundPage',
      'mount',
      expect.objectContaining({
        attemptedPath: expect.any(String),
      })
    );
  });

  it('should log path with query parameters', () => {
    window.location.hash = '#/invalid-path?param=value';

    render(
      <HashRouter>
        <Routes>
          <Route path="/invalid-path" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(mockWarn).toHaveBeenCalledWith(
      '404 Page Not Found: User attempted to access non-existent route',
      'NotFoundPage',
      'mount',
      expect.objectContaining({
        attemptedPath: expect.any(String),
        search: expect.any(String),
      })
    );
  });

  it('should display 404 error code', () => {
    render(
      <HashRouter>
        <Routes>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(screen.getByText('404')).toBeInTheDocument();
  });

  it('should display helpful message to user', () => {
    render(
      <HashRouter>
        <Routes>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(screen.getByText('Page Not Found')).toBeInTheDocument();
    expect(
      screen.getByText(/The page you're looking for doesn't exist or has been moved/)
    ).toBeInTheDocument();
  });

  it('should provide navigation options', () => {
    render(
      <HashRouter>
        <Routes>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(screen.getByRole('button', { name: /Go to Home/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Go Back/i })).toBeInTheDocument();
  });

  it('should log location state if provided', () => {
    render(
      <HashRouter>
        <Routes>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </HashRouter>
    );

    expect(mockWarn).toHaveBeenCalledWith(
      '404 Page Not Found: User attempted to access non-existent route',
      'NotFoundPage',
      'mount',
      expect.objectContaining({
        attemptedPath: expect.any(String),
        search: expect.any(String),
        hash: expect.any(String),
        state: expect.anything(),
      })
    );
  });
});
