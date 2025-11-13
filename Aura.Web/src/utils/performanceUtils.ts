/**
 * Performance optimization utility functions
 */

/**
 * Default comparison function that does shallow comparison
 * but ignores function props (useful for callbacks)
 */
export function shallowCompareIgnoringFunctions<P extends object>(
  prevProps: Readonly<P>,
  nextProps: Readonly<P>
): boolean {
  const prevKeys = Object.keys(prevProps) as Array<keyof P>;
  const nextKeys = Object.keys(nextProps) as Array<keyof P>;

  if (prevKeys.length !== nextKeys.length) {
    return false;
  }

  for (const key of prevKeys) {
    const prevValue = prevProps[key];
    const nextValue = nextProps[key];

    if (typeof prevValue === 'function' && typeof nextValue === 'function') {
      continue;
    }

    if (prevValue !== nextValue) {
      return false;
    }
  }

  return true;
}
