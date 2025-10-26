import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';
import '@testing-library/jest-dom';

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock localStorage
const localStorageMock = {
  getItem: (_key: string) => {
    return null;
  },
  setItem: (_key: string, _value: string) => {},
  removeItem: (_key: string) => {},
  clear: () => {},
  length: 0,
  key: (_index: number) => null,
};

(globalThis as any).localStorage = localStorageMock;

// Mock ResizeObserver
class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

(globalThis as any).ResizeObserver = ResizeObserverMock;
