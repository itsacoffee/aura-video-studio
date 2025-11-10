import { render, RenderOptions, RenderResult } from '@testing-library/react';
import { ReactElement, ReactNode } from 'react';
import { FluentProvider, webLightTheme, Theme } from '@fluentui/react-components';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';

/**
 * Custom render options for tests
 */
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  theme?: Theme;
  withRouter?: boolean;
  withQueryClient?: boolean;
  queryClient?: QueryClient;
}

/**
 * Create a test query client with sensible defaults
 */
export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
    logger: {
      log: console.log,
      warn: console.warn,
      error: () => {}, // Silence errors in tests
    },
  });
}

/**
 * Custom render method that includes common providers
 * 
 * @example
 * ```tsx
 * const { getByText } = renderWithProviders(<MyComponent />);
 * ```
 * 
 * @example with custom query client
 * ```tsx
 * const queryClient = createTestQueryClient();
 * const { getByText } = renderWithProviders(<MyComponent />, { queryClient });
 * ```
 */
export function renderWithProviders(
  ui: ReactElement,
  {
    theme = webLightTheme,
    withRouter = false,
    withQueryClient = true,
    queryClient,
    ...renderOptions
  }: CustomRenderOptions = {}
): RenderResult {
  const testQueryClient = queryClient ?? createTestQueryClient();

  function Wrapper({ children }: { children: ReactNode }): ReactElement {
    let content = children;

    // Wrap with QueryClientProvider if requested
    if (withQueryClient) {
      content = (
        <QueryClientProvider client={testQueryClient}>
          {content}
        </QueryClientProvider>
      );
    }

    // Wrap with Router if requested
    if (withRouter) {
      content = <BrowserRouter>{content}</BrowserRouter>;
    }

    // Always wrap with FluentProvider for UI components
    return <FluentProvider theme={theme}>{content}</FluentProvider>;
  }

  return render(ui, { wrapper: Wrapper, ...renderOptions });
}

/**
 * Wait for a condition to be true
 * 
 * @example
 * ```tsx
 * await waitFor(() => expect(getByText('Loaded')).toBeInTheDocument());
 * ```
 */
export { waitFor, screen, within, fireEvent } from '@testing-library/react';

/**
 * User event utilities for simulating user interactions
 */
export { default as userEvent } from '@testing-library/user-event';

/**
 * Mock functions and spies
 */
export const createMockFn = <T extends (...args: any[]) => any>(): jest.Mock<
  ReturnType<T>,
  Parameters<T>
> => {
  return jest.fn();
};

/**
 * Async utility to wait for next tick
 */
export const waitForNextTick = (): Promise<void> => {
  return new Promise((resolve) => setTimeout(resolve, 0));
};

/**
 * Flush all pending promises
 */
export const flushPromises = (): Promise<void> => {
  return new Promise((resolve) => setImmediate(resolve));
};

/**
 * Mock localStorage for tests
 */
export class MockLocalStorage {
  private store: Map<string, string> = new Map();

  getItem(key: string): string | null {
    return this.store.get(key) ?? null;
  }

  setItem(key: string, value: string): void {
    this.store.set(key, value);
  }

  removeItem(key: string): void {
    this.store.delete(key);
  }

  clear(): void {
    this.store.clear();
  }

  get length(): number {
    return this.store.size;
  }

  key(index: number): string | null {
    return Array.from(this.store.keys())[index] ?? null;
  }
}

/**
 * Mock sessionStorage for tests
 */
export class MockSessionStorage extends MockLocalStorage {}

/**
 * Setup mock storage
 */
export function setupMockStorage(): void {
  const mockLocalStorage = new MockLocalStorage();
  const mockSessionStorage = new MockSessionStorage();

  Object.defineProperty(window, 'localStorage', {
    value: mockLocalStorage,
    writable: true,
  });

  Object.defineProperty(window, 'sessionStorage', {
    value: mockSessionStorage,
    writable: true,
  });
}

/**
 * Mock window.matchMedia
 */
export function setupMockMatchMedia(): void {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {}, // deprecated
      removeListener: () => {}, // deprecated
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => true,
    }),
  });
}

/**
 * Mock IntersectionObserver
 */
export function setupMockIntersectionObserver(): void {
  global.IntersectionObserver = class IntersectionObserver {
    constructor() {}
    disconnect() {}
    observe() {}
    takeRecords(): IntersectionObserverEntry[] {
      return [];
    }
    unobserve() {}
  } as any;
}

/**
 * Setup all common mocks
 */
export function setupCommonMocks(): void {
  setupMockStorage();
  setupMockMatchMedia();
  setupMockIntersectionObserver();
}

/**
 * Create a mock fetch response
 */
export function createMockResponse<T>(
  data: T,
  options: {
    status?: number;
    statusText?: string;
    headers?: Record<string, string>;
  } = {}
): Response {
  return {
    ok: (options.status ?? 200) >= 200 && (options.status ?? 200) < 300,
    status: options.status ?? 200,
    statusText: options.statusText ?? 'OK',
    headers: new Headers(options.headers ?? {}),
    json: async () => data,
    text: async () => JSON.stringify(data),
    blob: async () => new Blob([JSON.stringify(data)]),
    arrayBuffer: async () => new ArrayBuffer(0),
    formData: async () => new FormData(),
    clone: function() { return this; },
    body: null,
    bodyUsed: false,
    redirected: false,
    type: 'basic',
    url: '',
  } as Response;
}

/**
 * Re-export everything from @testing-library/react
 */
export * from '@testing-library/react';
