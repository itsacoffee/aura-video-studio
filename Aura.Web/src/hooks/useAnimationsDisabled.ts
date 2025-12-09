/**
 * useAnimationsDisabled Hook
 * Efficiently checks if animations are disabled via CSS custom properties
 */

/**
 * Hook to check if animations are disabled based on graphics settings.
 * Uses CSS custom properties which are set by the graphics settings provider.
 *
 * Note: This reads the CSS property directly to avoid needing React context.
 * The GraphicsProvider updates CSS variables when settings change, making
 * the computed value reactive through the normal rendering cycle.
 *
 * @returns true if animations are disabled, false if enabled
 */
export function useAnimationsDisabled(): boolean {
  if (typeof document === 'undefined') {
    return false;
  }
  const computedDuration = getComputedStyle(document.documentElement)
    .getPropertyValue('--duration-normal')
    .trim();
  return computedDuration === '0ms';
}
