/**
 * Comprehensive test helpers and utilities
 */

import { vi } from 'vitest';

/**
 * Waits for a condition to be true
 */
export async function waitFor(
  condition: () => boolean | Promise<boolean>,
  options: {
    timeout?: number;
    interval?: number;
    timeoutMessage?: string;
  } = {}
): Promise<void> {
  const {
    timeout = 5000,
    interval = 50,
    timeoutMessage = 'Timeout waiting for condition',
  } = options;

  const startTime = Date.now();

  while (Date.now() - startTime < timeout) {
    const result = await condition();
    if (result) {
      return;
    }
    await sleep(interval);
  }

  throw new Error(timeoutMessage);
}

/**
 * Sleep for specified milliseconds
 */
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Creates a deferred promise
 */
export function createDeferred<T>(): {
  promise: Promise<T>;
  resolve: (value: T) => void;
  reject: (error: Error) => void;
} {
  let resolve!: (value: T) => void;
  let reject!: (error: Error) => void;

  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });

  return { promise, resolve, reject };
}

/**
 * Mocks console methods to prevent noise in tests
 */
export function mockConsole() {
  const originalConsole = { ...console };

  beforeEach(() => {
    console.log = vi.fn();
    console.warn = vi.fn();
    console.error = vi.fn();
    console.info = vi.fn();
    console.debug = vi.fn();
  });

  afterEach(() => {
    Object.assign(console, originalConsole);
  });
}

/**
 * Mocks localStorage for tests
 */
export function createMockStorage(): Storage {
  let store: Record<string, string> = {};

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      store = {};
    },
    key: (index: number) => Object.keys(store)[index] || null,
    get length() {
      return Object.keys(store).length;
    },
  };
}

/**
 * Mocks window.matchMedia
 */
export function mockMatchMedia() {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
}

/**
 * Mocks IntersectionObserver
 */
export function mockIntersectionObserver() {
  global.IntersectionObserver = class IntersectionObserver {
    observe = vi.fn();
    disconnect = vi.fn();
    unobserve = vi.fn();
    takeRecords = vi.fn();
    root = null;
    rootMargin = '';
    thresholds = [];
  } as unknown as IntersectionObserver;
}

/**
 * Mocks ResizeObserver
 */
export function mockResizeObserver() {
  global.ResizeObserver = class ResizeObserver {
    observe = vi.fn();
    disconnect = vi.fn();
    unobserve = vi.fn();
  } as unknown as typeof ResizeObserver;
}

/**
 * Creates mock file for upload testing
 */
export function createMockFile(
  content: string,
  fileName: string,
  mimeType: string = 'text/plain'
): File {
  const blob = new Blob([content], { type: mimeType });
  return new File([blob], fileName, { type: mimeType });
}

/**
 * Creates mock video file
 */
export function createMockVideoFile(
  fileName: string = 'test-video.mp4',
  size: number = 1024 * 1024
): File {
  const blob = new Blob([new ArrayBuffer(size)], { type: 'video/mp4' });
  return new File([blob], fileName, { type: 'video/mp4' });
}

/**
 * Creates mock image file
 */
export function createMockImageFile(
  fileName: string = 'test-image.jpg',
  width: number = 1920,
  height: number = 1080
): File {
  // Create a simple 1x1 pixel image
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');
  if (ctx) {
    ctx.fillStyle = '#ff0000';
    ctx.fillRect(0, 0, width, height);
  }

  return new Promise<File>((resolve) => {
    canvas.toBlob((blob) => {
      if (blob) {
        resolve(new File([blob], fileName, { type: 'image/jpeg' }));
      }
    }, 'image/jpeg');
  });
}

/**
 * Simulates user typing
 */
export async function typeText(
  element: HTMLInputElement | HTMLTextAreaElement,
  text: string,
  options: { delay?: number } = {}
): Promise<void> {
  const { delay = 10 } = options;

  for (const char of text) {
    element.value += char;
    element.dispatchEvent(new Event('input', { bubbles: true }));
    await sleep(delay);
  }
}

/**
 * Simulates user click
 */
export function click(element: HTMLElement): void {
  element.click();
  element.dispatchEvent(new MouseEvent('click', { bubbles: true }));
}

/**
 * Generates random test data
 */
export const generateTestData = {
  string: (length: number = 10): string => {
    return Math.random()
      .toString(36)
      .substring(2, 2 + length);
  },

  number: (min: number = 0, max: number = 100): number => {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  },

  boolean: (): boolean => {
    return Math.random() > 0.5;
  },

  email: (): string => {
    return `test${generateTestData.string(5)}@example.com`;
  },

  url: (): string => {
    return `https://example.com/${generateTestData.string(8)}`;
  },

  uuid: (): string => {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  },

  date: (): Date => {
    const now = new Date();
    const daysOffset = generateTestData.number(-365, 365);
    return new Date(now.getTime() + daysOffset * 24 * 60 * 60 * 1000);
  },

  array: <T>(generator: () => T, count: number): T[] => {
    return Array.from({ length: count }, generator);
  },
};

/**
 * Test data fixtures
 */
export const fixtures = {
  user: () => ({
    id: generateTestData.uuid(),
    name: `Test User ${generateTestData.string(5)}`,
    email: generateTestData.email(),
    createdAt: generateTestData.date(),
  }),

  project: () => ({
    id: generateTestData.uuid(),
    name: `Test Project ${generateTestData.string(5)}`,
    description: `Test description ${generateTestData.string(20)}`,
    createdAt: generateTestData.date(),
  }),

  videoJob: () => ({
    id: generateTestData.uuid(),
    title: `Test Video ${generateTestData.string(5)}`,
    status: 'pending' as const,
    progress: 0,
    createdAt: generateTestData.date(),
  }),
};

/**
 * Performance measurement utility
 */
export class PerformanceMonitor {
  private marks: Map<string, number> = new Map();

  mark(name: string): void {
    this.marks.set(name, performance.now());
  }

  measure(name: string, startMark: string, endMark?: string): number {
    const start = this.marks.get(startMark);
    if (!start) {
      throw new Error(`Start mark "${startMark}" not found`);
    }

    const end = endMark ? this.marks.get(endMark) : performance.now();
    if (endMark && !end) {
      throw new Error(`End mark "${endMark}" not found`);
    }

    const duration = (end || performance.now()) - start;
    console.info(`${name}: ${duration.toFixed(2)}ms`);
    return duration;
  }

  clear(): void {
    this.marks.clear();
  }
}

/**
 * Retry utility for flaky operations
 */
export async function retry<T>(
  fn: () => T | Promise<T>,
  options: {
    attempts?: number;
    delay?: number;
    onRetry?: (error: Error, attempt: number) => void;
  } = {}
): Promise<T> {
  const { attempts = 3, delay = 100, onRetry } = options;

  let lastError: Error;

  for (let attempt = 1; attempt <= attempts; attempt++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error as Error;

      if (attempt < attempts) {
        onRetry?.(lastError, attempt);
        await sleep(delay * attempt); // Exponential backoff
      }
    }
  }

  throw lastError!;
}

/**
 * Debounce utility for testing
 */
export function createDebounce() {
  let timeoutId: NodeJS.Timeout | null = null;

  return (fn: () => void, delay: number): void => {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    timeoutId = setTimeout(fn, delay);
  };
}

/**
 * Throttle utility for testing
 */
export function createThrottle() {
  let lastRun = 0;

  return (fn: () => void, limit: number): void => {
    const now = Date.now();
    if (now - lastRun >= limit) {
      fn();
      lastRun = now;
    }
  };
}

/**
 * Mock API response helper
 */
export function createMockResponse<T>(
  data: T,
  options: {
    status?: number;
    delay?: number;
    error?: boolean;
  } = {}
): Promise<T> {
  const { delay = 0, error = false } = options;

  return new Promise((resolve, reject) => {
    setTimeout(() => {
      if (error) {
        reject(new Error('Mock API error'));
      } else {
        resolve(data);
      }
    }, delay);
  });
}

/**
 * Setup and teardown helpers
 */
export function setupTest(setup: () => void | Promise<void>): void {
  beforeEach(async () => {
    await setup();
  });
}

export function teardownTest(teardown: () => void | Promise<void>): void {
  afterEach(async () => {
    await teardown();
  });
}

/**
 * Snapshot testing helper
 */
export function expectSnapshot(value: unknown, name?: string): void {
  expect(value).toMatchSnapshot(name);
}
