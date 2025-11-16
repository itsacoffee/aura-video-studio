import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NetworkConnectivityTest } from '../NetworkConnectivityTest';

// Mock the fetch function
global.fetch = vi.fn();

describe('NetworkConnectivityTest', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders initial state with test button', () => {
    render(<NetworkConnectivityTest />);

    expect(screen.getByText('Run Network Tests')).toBeDefined();
    expect(
      screen.getByText(/Test network connectivity to external APIs and services/)
    ).toBeDefined();
  });

  it('shows loading state when tests are running', async () => {
    // Mock fetch to never resolve to keep loading state
    (global.fetch as ReturnType<typeof vi.fn>).mockImplementation(() => new Promise(() => {}));

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('Testing network connectivity...')).toBeDefined();
      expect(screen.getByText('Running Tests...')).toBeDefined();
    });
  });

  it('displays successful test results', async () => {
    const mockResults = {
      success: true,
      overallStatus: 'AllTestsPassed',
      timestamp: '2024-01-01T12:00:00Z',
      totalElapsedMs: 500,
      tests: {
        google: {
          name: 'Google',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 100,
          reachable: true,
        },
        openai: {
          name: 'OpenAI',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 150,
          reachable: true,
        },
        pexels: {
          name: 'Pexels',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 120,
          reachable: true,
        },
        dns: {
          name: 'DNS',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 130,
          reachable: true,
        },
      },
    };

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => mockResults,
    });

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('Internet Connectivity')).toBeDefined();
      expect(screen.getByText('OpenAI API')).toBeDefined();
      expect(screen.getByText('Pexels API')).toBeDefined();
      expect(screen.getByText('DNS Resolution')).toBeDefined();
    });

    // Check that all tests show as connected
    const connectedStatuses = screen.getAllByText(/Status: Connected/);
    expect(connectedStatuses.length).toBe(4);

    // Check response times are displayed
    expect(screen.getByText(/Response time: 100ms/)).toBeDefined();
    expect(screen.getByText(/Response time: 150ms/)).toBeDefined();
  });

  it('displays failed test results with error messages', async () => {
    const mockResults = {
      success: false,
      overallStatus: 'SomeTestsFailed',
      timestamp: '2024-01-01T12:00:00Z',
      totalElapsedMs: 500,
      tests: {
        google: {
          name: 'Google',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 100,
          reachable: true,
        },
        openai: {
          name: 'OpenAI',
          success: false,
          elapsedMs: 10000,
          reachable: false,
          errorType: 'Timeout',
          errorMessage: 'Request timed out after 10000ms',
        },
        pexels: {
          name: 'Pexels',
          success: false,
          statusCode: 401,
          statusText: 'Unauthorized',
          elapsedMs: 200,
          reachable: true,
          errorMessage: 'Invalid API key',
        },
        dns: {
          name: 'DNS',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 130,
          reachable: true,
        },
      },
    };

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => mockResults,
    });

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('⚠️ Some Tests Failed')).toBeDefined();
      expect(screen.getByText(/Request timed out after 10000ms/)).toBeDefined();
      expect(screen.getByText(/Invalid API key/)).toBeDefined();
    });

    // Check that failed tests show appropriate status
    expect(screen.getByText(/Status: Timeout/)).toBeDefined();
    expect(screen.getByText(/Status: Failed/)).toBeDefined();
  });

  it('displays error message when network test request fails', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText('Test Failed')).toBeDefined();
      expect(screen.getByText(/Failed to run network tests: Network error/)).toBeDefined();
    });
  });

  it('disables button while tests are running', async () => {
    (global.fetch as ReturnType<typeof vi.fn>).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 1000))
    );

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests') as HTMLButtonElement;
    expect(button.disabled).toBe(false);

    fireEvent.click(button);

    await waitFor(() => {
      const runningButton = screen.getByText('Running Tests...') as HTMLButtonElement;
      expect(runningButton.disabled).toBe(true);
    });
  });

  it('displays timestamp of last test', async () => {
    const mockResults = {
      success: true,
      overallStatus: 'AllTestsPassed',
      timestamp: '2024-01-01T12:00:00Z',
      totalElapsedMs: 500,
      tests: {
        google: {
          name: 'Google',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 100,
          reachable: true,
        },
        openai: {
          name: 'OpenAI',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 150,
          reachable: true,
        },
        pexels: {
          name: 'Pexels',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 120,
          reachable: true,
        },
        dns: {
          name: 'DNS',
          success: true,
          statusCode: 200,
          statusText: 'OK',
          elapsedMs: 130,
          reachable: true,
        },
      },
    };

    (global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
      ok: true,
      json: async () => mockResults,
    });

    render(<NetworkConnectivityTest />);

    const button = screen.getByText('Run Network Tests');
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByText(/Last tested:/)).toBeDefined();
      expect(screen.getByText(/Total time: 500ms/)).toBeDefined();
    });
  });
});
