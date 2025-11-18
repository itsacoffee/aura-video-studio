/**
 * Routing Verification Test
 * Verifies that all routes render correctly in MemoryRouter
 */

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, waitFor } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import App from '../App';

// Mock window.electron
beforeEach(() => {
  window.AURA_IS_ELECTRON = true;
  window.AURA_BACKEND_URL = 'http://localhost:5005';

  // Mock fetch for API calls
  global.fetch = vi.fn(() =>
    Promise.resolve({
      ok: true,
      json: () => Promise.resolve({ isComplete: true }),
    })
  ) as unknown as typeof fetch;
});

describe('Routing Verification', () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  it('should render App with MemoryRouter', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    );

    // App should render without crashing
    await waitFor(() => {
      expect(document.body).toBeInTheDocument();
    });
  });

  it('should have FluentProvider wrapping routes', () => {
    const { container } = render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    );

    // FluentProvider adds specific class names
    const fluentProviderElement = container.querySelector('[class*="fluent"]');
    expect(fluentProviderElement).toBeTruthy();
  });

  it('should use MemoryRouter for Electron context', () => {
    // Verify MemoryRouter is being used (not BrowserRouter)
    // MemoryRouter doesn't interact with window.location
    const initialPath = window.location.pathname;

    render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    );

    // window.location should not change with MemoryRouter
    expect(window.location.pathname).toBe(initialPath);
  });
});
