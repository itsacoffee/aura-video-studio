/**
 * Type definitions for Playwright E2E tests
 * Includes Chrome-specific Performance APIs
 */

interface MemoryInfo {
  usedJSHeapSize: number;
  totalJSHeapSize: number;
  jsHeapSizeLimit: number;
}

export interface PerformanceWithMemory extends Performance {
  memory?: MemoryInfo;
}

interface ChromeDevToolsAPI {
  getEventListeners?: (element: EventTarget) => Record<string, EventListener[]>;
}

export interface WindowWithGC extends Window, ChromeDevToolsAPI {
  gc?: () => void;
}

declare global {
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface Window extends WindowWithGC {}
  const performance: PerformanceWithMemory;
}

export {};
