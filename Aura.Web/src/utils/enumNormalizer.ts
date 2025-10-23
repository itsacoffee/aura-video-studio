/**
 * Enum normalization utilities for handling legacy and canonical enum values
 */

import type { Brief, PlanSpec } from '../types';

export type Aspect = 'Widescreen16x9' | 'Vertical9x16' | 'Square1x1';
export type Density = 'Sparse' | 'Balanced' | 'Dense';

/**
 * Normalizes aspect ratio values to canonical enum names
 * Accepts both canonical names and legacy aliases
 */
export function normalizeAspect(value: string): Aspect {
  const normalized = value.trim();

  // Handle aliases
  switch (normalized) {
    case '16:9':
      return 'Widescreen16x9';
    case '9:16':
      return 'Vertical9x16';
    case '1:1':
      return 'Square1x1';
    default:
      // Return as-is if already canonical
      if (
        normalized === 'Widescreen16x9' ||
        normalized === 'Vertical9x16' ||
        normalized === 'Square1x1'
      ) {
        return normalized as Aspect;
      }
      // Fallback to default
      console.warn(`Unknown aspect value: ${value}, defaulting to Widescreen16x9`);
      return 'Widescreen16x9';
  }
}

/**
 * Normalizes density values to canonical enum names
 * Accepts both canonical names and legacy aliases
 */
export function normalizeDensity(value: string): Density {
  const normalized = value.trim();

  // Handle aliases
  if (normalized.toLowerCase() === 'normal') {
    console.warn('Density "Normal" is deprecated, using "Balanced" instead');
    return 'Balanced';
  }

  // Return as-is if already canonical
  if (normalized === 'Sparse' || normalized === 'Balanced' || normalized === 'Dense') {
    return normalized as Density;
  }

  // Fallback to default
  console.warn(`Unknown density value: ${value}, defaulting to Balanced`);
  return 'Balanced';
}

/**
 * Validates and warns about legacy enum values in brief and plan spec
 */
export function validateAndWarnEnums(brief: Partial<Brief>, planSpec: Partial<PlanSpec>): void {
  // Check aspect
  if (brief.aspect) {
    const legacyAspects = ['16:9', '9:16', '1:1'];
    if (legacyAspects.includes(brief.aspect)) {
      console.warn(
        `[Compatibility] Aspect ratio "${brief.aspect}" is a legacy format. Consider using canonical name (e.g., "Widescreen16x9").`
      );
    }
  }

  // Check density
  if (planSpec.density) {
    if (planSpec.density.toLowerCase() === 'normal') {
      console.warn(`[Compatibility] Density "Normal" is deprecated. Use "Balanced" instead.`);
    }
  }
}

/**
 * Normalizes brief and plan spec enums before sending to API
 */
export function normalizeEnumsForApi(
  brief: Partial<Brief>,
  planSpec: Partial<PlanSpec>
): { brief: Partial<Brief>; planSpec: Partial<PlanSpec> } {
  const normalizedBrief: Partial<Brief> = {
    ...brief,
    aspect: brief.aspect ? normalizeAspect(brief.aspect) : undefined,
  };

  const normalizedPlanSpec: Partial<PlanSpec> = {
    ...planSpec,
    density: planSpec.density ? normalizeDensity(planSpec.density) : undefined,
  };

  return { brief: normalizedBrief, planSpec: normalizedPlanSpec };
}
