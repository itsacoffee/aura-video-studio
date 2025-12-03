/**
 * OpenCut Utility Hooks
 *
 * Reusable hooks for the OpenCut video editor.
 * Provides common functionality like debounce, throttle, keyboard shortcuts, etc.
 */

import { useState, useEffect, useCallback, useRef, useMemo } from 'react';

/**
 * Debounce hook - delays value updates until after the specified delay.
 *
 * @param value - The value to debounce
 * @param delay - Delay in milliseconds
 * @returns The debounced value
 *
 * @example
 * const [searchTerm, setSearchTerm] = useState('');
 * const debouncedSearch = useDebounce(searchTerm, 300);
 *
 * useEffect(() => {
 *   // This runs 300ms after the user stops typing
 *   searchApi(debouncedSearch);
 * }, [debouncedSearch]);
 */
export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(timer);
    };
  }, [value, delay]);

  return debouncedValue;
}

/**
 * Throttle hook - limits callback frequency to once per delay period.
 *
 * @param callback - The callback to throttle
 * @param delay - Minimum time between calls in milliseconds
 * @returns The throttled callback
 *
 * @example
 * const handleScroll = useThrottle((e) => {
 *   console.log('Scroll position:', e.target.scrollTop);
 * }, 100);
 */
export function useThrottle<T extends (...args: Parameters<T>) => ReturnType<T>>(
  callback: T,
  delay: number
): T {
  const lastCallRef = useRef<number>(0);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const callbackRef = useRef(callback);

  // Keep callback ref updated
  useEffect(() => {
    callbackRef.current = callback;
  }, [callback]);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  const throttledCallback = useCallback(
    (...args: Parameters<T>) => {
      const now = Date.now();
      const timeSinceLastCall = now - lastCallRef.current;

      if (timeSinceLastCall >= delay) {
        lastCallRef.current = now;
        return callbackRef.current(...args);
      } else {
        // Schedule a call for the end of the delay period
        if (timeoutRef.current) {
          clearTimeout(timeoutRef.current);
        }

        timeoutRef.current = setTimeout(() => {
          lastCallRef.current = Date.now();
          callbackRef.current(...args);
        }, delay - timeSinceLastCall);
      }
    },
    [delay]
  ) as T;

  return throttledCallback;
}

/**
 * Previous value hook - tracks the previous render's value.
 *
 * @param value - The current value
 * @returns The previous value (undefined on first render)
 *
 * @example
 * const [count, setCount] = useState(0);
 * const prevCount = usePrevious(count);
 *
 * console.log(`Count changed from ${prevCount} to ${count}`);
 */
export function usePrevious<T>(value: T): T | undefined {
  const ref = useRef<T>();

  useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}

/**
 * Click outside hook - detects clicks outside the referenced element.
 *
 * @param callback - Function to call when click occurs outside
 * @returns Ref to attach to the element to monitor
 *
 * @example
 * const handleClose = useCallback(() => setIsOpen(false), []);
 * const containerRef = useClickOutside<HTMLDivElement>(handleClose);
 *
 * return <div ref={containerRef}>...</div>;
 */
export function useClickOutside<T extends HTMLElement>(
  callback: () => void
): React.RefObject<T | null> {
  const ref = useRef<T | null>(null);
  const callbackRef = useRef(callback);

  // Keep callback ref updated
  useEffect(() => {
    callbackRef.current = callback;
  }, [callback]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        callbackRef.current();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  return ref;
}

/**
 * Keyboard shortcut options.
 */
export interface KeyboardShortcutOptions {
  /** Require Ctrl key (or Cmd on Mac) */
  ctrl?: boolean;
  /** Require Shift key */
  shift?: boolean;
  /** Require Alt key */
  alt?: boolean;
  /** Require Meta key (Cmd on Mac) */
  meta?: boolean;
  /** Whether the shortcut is enabled */
  enabled?: boolean;
  /** Prevent default browser behavior */
  preventDefault?: boolean;
}

/**
 * Keyboard shortcut hook - registers global keyboard shortcuts.
 *
 * @param key - The key to listen for (e.g., 's', 'Enter', 'Escape')
 * @param callback - Function to call when shortcut is triggered
 * @param options - Modifier key requirements and enabled state
 *
 * @example
 * // Save on Cmd+S (Mac) or Ctrl+S (Windows)
 * useKeyboardShortcut('s', handleSave, { ctrl: true, preventDefault: true });
 *
 * // Split clip on 'S' key
 * useKeyboardShortcut('s', handleSplit);
 */
export function useKeyboardShortcut(
  key: string,
  callback: (e: KeyboardEvent) => void,
  options: KeyboardShortcutOptions = {}
): void {
  const {
    ctrl = false,
    shift = false,
    alt = false,
    meta = false,
    enabled = true,
    preventDefault = false,
  } = options;

  const callbackRef = useRef(callback);

  // Keep callback ref updated
  useEffect(() => {
    callbackRef.current = callback;
  }, [callback]);

  useEffect(() => {
    if (!enabled) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Skip if typing in an input
      const target = e.target as HTMLElement;
      const isInputFocused =
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.tagName === 'SELECT' ||
        target.isContentEditable;

      // Allow shortcuts with modifiers even in inputs
      if (isInputFocused && !ctrl && !meta && !alt) {
        return;
      }

      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;

      // Check if the key matches (case-insensitive)
      if (e.key.toLowerCase() !== key.toLowerCase()) {
        return;
      }

      // Check modifier keys
      // For ctrl, we check both Ctrl and Cmd on Mac
      const ctrlMatches = ctrl ? (isMac ? e.metaKey : e.ctrlKey) : !e.ctrlKey && !e.metaKey;
      const shiftMatches = shift ? e.shiftKey : !e.shiftKey;
      const altMatches = alt ? e.altKey : !e.altKey;
      const metaMatches = meta ? e.metaKey : true; // Meta is special, only required if specified

      // If ctrl is required, ignore individual meta check
      if (ctrl && ctrlMatches && shiftMatches && altMatches) {
        if (preventDefault) {
          e.preventDefault();
        }
        callbackRef.current(e);
      } else if (!ctrl && ctrlMatches && shiftMatches && altMatches && metaMatches) {
        if (preventDefault) {
          e.preventDefault();
        }
        callbackRef.current(e);
      }
    };

    window.addEventListener('keydown', handleKeyDown);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [key, ctrl, shift, alt, meta, enabled, preventDefault]);
}

/**
 * Local storage hook - persisted state with JSON serialization.
 *
 * @param key - Storage key
 * @param initialValue - Default value if no stored value exists
 * @returns Tuple of [value, setValue] similar to useState
 *
 * @example
 * const [theme, setTheme] = useLocalStorage('theme', 'dark');
 * const [settings, setSettings] = useLocalStorage('settings', { zoom: 1, snap: true });
 */
export function useLocalStorage<T>(
  key: string,
  initialValue: T
): [T, (value: T | ((prev: T) => T)) => void] {
  // Get stored value or use initial value
  const storedValue = useMemo(() => {
    try {
      const item = window.localStorage.getItem(key);

      if (item) {
        return JSON.parse(item) as T;
      }
    } catch {
      console.warn(`Error reading localStorage key "${key}"`);
    }
    return initialValue;
  }, [key, initialValue]);

  const [value, setValue] = useState<T>(storedValue);

  // Update localStorage when value changes
  useEffect(() => {
    try {
      window.localStorage.setItem(key, JSON.stringify(value));
    } catch {
      console.warn(`Error writing localStorage key "${key}"`);
    }
  }, [key, value]);

  // Handle storage events from other tabs
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === key && e.newValue !== null) {
        try {
          setValue(JSON.parse(e.newValue) as T);
        } catch {
          console.warn(`Error parsing localStorage value for "${key}"`);
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, [key]);

  return [value, setValue];
}

/**
 * Resize observer dimensions.
 */
export interface ResizeObserverDimensions {
  width: number;
  height: number;
}

/**
 * Resize observer hook - tracks element dimensions.
 *
 * @returns Tuple of [ref, dimensions]
 *
 * @example
 * const [containerRef, { width, height }] = useResizeObserver<HTMLDivElement>();
 *
 * return (
 *   <div ref={containerRef}>
 *     Container size: {width} x {height}
 *   </div>
 * );
 */
export function useResizeObserver<T extends HTMLElement>(): [
  React.RefObject<T | null>,
  ResizeObserverDimensions,
] {
  const ref = useRef<T | null>(null);
  const [dimensions, setDimensions] = useState<ResizeObserverDimensions>({ width: 0, height: 0 });

  useEffect(() => {
    const element = ref.current;

    if (!element) return;

    const resizeObserver = new ResizeObserver((entries) => {
      if (entries[0]) {
        const { width, height } = entries[0].contentRect;
        setDimensions({ width, height });
      }
    });

    resizeObserver.observe(element);

    return () => {
      resizeObserver.disconnect();
    };
  }, []);

  return [ref, dimensions];
}

/**
 * Intersection observer hook - detects when element is visible in viewport.
 *
 * @param options - IntersectionObserver options
 * @returns Tuple of [ref, isIntersecting]
 *
 * @example
 * const [elementRef, isVisible] = useIntersectionObserver<HTMLDivElement>({ threshold: 0.5 });
 *
 * useEffect(() => {
 *   if (isVisible) {
 *     loadContent();
 *   }
 * }, [isVisible]);
 */
export function useIntersectionObserver<T extends HTMLElement>(
  options?: IntersectionObserverInit
): [React.RefObject<T | null>, boolean] {
  const ref = useRef<T | null>(null);
  const [isIntersecting, setIsIntersecting] = useState(false);

  useEffect(() => {
    const element = ref.current;

    if (!element) return;

    const observer = new IntersectionObserver(([entry]) => {
      setIsIntersecting(entry.isIntersecting);
    }, options);

    observer.observe(element);

    return () => {
      observer.disconnect();
    };
  }, [options]);

  return [ref, isIntersecting];
}

/**
 * Media query hook - tracks media query matches.
 *
 * @param query - CSS media query string
 * @returns Whether the media query matches
 *
 * @example
 * const isMobile = useMediaQuery('(max-width: 768px)');
 * const prefersReducedMotion = useMediaQuery('(prefers-reduced-motion: reduce)');
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() => {
    if (typeof window === 'undefined') return false;
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    const mediaQuery = window.matchMedia(query);
    const handleChange = (e: MediaQueryListEvent) => {
      setMatches(e.matches);
    };

    // Set initial value
    setMatches(mediaQuery.matches);

    // Listen for changes
    mediaQuery.addEventListener('change', handleChange);

    return () => {
      mediaQuery.removeEventListener('change', handleChange);
    };
  }, [query]);

  return matches;
}
