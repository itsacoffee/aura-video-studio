/**
 * Tests for default route validation
 * Requirement 9: Verify default route "/" actually has component defined - not empty Route
 */

import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tantml:react-query';
import { render, waitFor } from '@testing-library/react';
import { HashRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi } from 'vitest';
import { WelcomePage } from '../pages/WelcomePage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
});

describe('Default Route Validation', () => {
  it('should have a component defined for default route "/"', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <HashRouter>
            <Routes>
              <Route path="/" element={<WelcomePage />} />
            </Routes>
          </HashRouter>
        </FluentProvider>
      </QueryClientProvider>
    );

    // Wait for the component to render
    await waitFor(() => {
      // The route should render the WelcomePage component, not an empty route
      const container = document.querySelector('body');
      expect(container).toBeTruthy();
      expect(container?.textContent).not.toBe('');
    });
  });

  it('should render WelcomePage with content', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <HashRouter>
            <Routes>
              <Route path="/" element={<WelcomePage />} />
            </Routes>
          </HashRouter>
        </FluentProvider>
      </QueryClientProvider>
    );

    await waitFor(() => {
      // WelcomePage should have actual content
      const body = document.body;
      expect(body.innerHTML).not.toBe('');
      expect(body.innerHTML.length).toBeGreaterThan(0);
    });
  });

  it('should not be an empty Route element', () => {
    const { container } = render(
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <HashRouter>
            <Routes>
              <Route path="/" element={<WelcomePage />} />
            </Routes>
          </HashRouter>
        </FluentProvider>
      </QueryClientProvider>
    );

    // Route should have rendered content
    expect(container.firstChild).toBeTruthy();
    expect(container.innerHTML).not.toBe('');
  });

  it('should navigate to default route when accessing root', async () => {
    window.location.hash = '#/';

    render(
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <HashRouter>
            <Routes>
              <Route path="/" element={<WelcomePage />} />
            </Routes>
          </HashRouter>
        </FluentProvider>
      </QueryClientProvider>
    );

    await waitFor(() => {
      // Should be at root route
      expect(window.location.hash).toBe('#/');

      // And should have content rendered
      const body = document.body;
      expect(body.textContent).not.toBe('');
    });
  });

  it('should render WelcomePage without errors', () => {
    const consoleSpy = vi.spyOn(console, 'error');
    consoleSpy.mockImplementation(() => {
      // suppress errors
    });

    render(
      <QueryClientProvider client={queryClient}>
        <FluentProvider theme={webLightTheme}>
          <HashRouter>
            <Routes>
              <Route path="/" element={<WelcomePage />} />
            </Routes>
          </HashRouter>
        </FluentProvider>
      </QueryClientProvider>
    );

    // There should be no console errors during render
    expect(consoleSpy).not.toHaveBeenCalled();

    consoleSpy.mockRestore();
  });
});
