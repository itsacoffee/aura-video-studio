import { loggingService as logger } from './loggingService';
/**
 * Analytics service for tracking wizard and user events
 * This is a simple implementation that can be extended with real analytics providers
 */

export interface AnalyticsEvent {
  name: string;
  category: string;
  properties?: Record<string, string | number | boolean>;
  timestamp: Date;
}

/**
 * Track an analytics event
 */
export function trackEvent(
  name: string,
  category: string,
  properties?: Record<string, string | number | boolean>
): void {
  const event: AnalyticsEvent = {
    name,
    category,
    properties,
    timestamp: new Date(),
  };

  // Log to console in development
  if (import.meta.env.DEV) {
    logger.debug('Analytics Event', 'analytics', 'trackEvent', { event });
  }

  // Store in localStorage for now (can be extended to send to backend)
  storeEventLocally(event);

  // Future: Send to analytics backend
  // sendToAnalyticsBackend(event);
}

/**
 * Track wizard-specific events
 */
export const wizardAnalytics = {
  started: () => {
    trackEvent('wizard_started', 'onboarding');
  },

  stepViewed: (stepNumber: number, stepName: string) => {
    trackEvent('wizard_step_viewed', 'onboarding', {
      step_number: stepNumber,
      step_name: stepName,
    });
  },

  stepCompleted: (stepNumber: number, stepName: string, timeSpent: number) => {
    trackEvent('wizard_step_completed', 'onboarding', {
      step_number: stepNumber,
      step_name: stepName,
      time_spent_seconds: timeSpent,
    });
  },

  stepAbandoned: (stepNumber: number, stepName: string) => {
    trackEvent('wizard_step_abandoned', 'onboarding', {
      step_number: stepNumber,
      step_name: stepName,
    });
  },

  completed: (totalTime: number, configuration: Record<string, unknown>) => {
    trackEvent('wizard_completed', 'onboarding', {
      total_time_seconds: totalTime,
      ...configuration,
    });
  },

  skipped: (stepNumber: number, reason?: string) => {
    trackEvent('wizard_skipped', 'onboarding', {
      step_number: stepNumber,
      reason: reason || 'user_choice',
    });
  },

  tierSelected: (tier: 'free' | 'pro') => {
    trackEvent('tier_selected', 'onboarding', { tier });
  },

  templateSelected: (templateId: string) => {
    trackEvent('template_selected', 'onboarding', { template_id: templateId });
  },

  dependencyInstalled: (dependencyId: string, method: 'auto' | 'manual') => {
    trackEvent('dependency_installed', 'onboarding', {
      dependency_id: dependencyId,
      install_method: method,
    });
  },

  tutorialStarted: () => {
    trackEvent('tutorial_started', 'onboarding');
  },

  tutorialCompleted: () => {
    trackEvent('tutorial_completed', 'onboarding');
  },

  tutorialSkipped: (atStep: number) => {
    trackEvent('tutorial_skipped', 'onboarding', { at_step: atStep });
  },
};

/**
 * Store event locally for analysis
 */
function storeEventLocally(event: AnalyticsEvent): void {
  const key = 'aura_analytics_events';
  const existingEvents = localStorage.getItem(key);
  const events: AnalyticsEvent[] = existingEvents ? JSON.parse(existingEvents) : [];

  events.push(event);

  // Keep only last 100 events to avoid storage bloat
  const trimmedEvents = events.slice(-100);

  localStorage.setItem(key, JSON.stringify(trimmedEvents));
}

/**
 * Get all stored analytics events
 */
export function getStoredEvents(): AnalyticsEvent[] {
  const key = 'aura_analytics_events';
  const stored = localStorage.getItem(key);
  return stored ? JSON.parse(stored) : [];
}

/**
 * Clear all stored analytics events
 */
export function clearStoredEvents(): void {
  localStorage.removeItem('aura_analytics_events');
}

/**
 * Get wizard completion statistics
 */
export function getWizardStats(): {
  totalCompletions: number;
  totalAbandons: number;
  averageCompletionTime: number;
  mostCommonExitStep: number | null;
} {
  const events = getStoredEvents();

  const completions = events.filter((e) => e.name === 'wizard_completed');
  const abandons = events.filter((e) => e.name === 'wizard_step_abandoned');

  const averageTime =
    completions.length > 0
      ? completions.reduce(
          (sum, e) => sum + ((e.properties?.total_time_seconds as number) || 0),
          0
        ) / completions.length
      : 0;

  // Find most common abandon step
  const abandonSteps = abandons.map((e) => e.properties?.step_number as number);
  const stepCounts = abandonSteps.reduce(
    (acc, step) => {
      acc[step] = (acc[step] || 0) + 1;
      return acc;
    },
    {} as Record<number, number>
  );

  const mostCommonExitStep =
    Object.entries(stepCounts).length > 0
      ? parseInt(Object.entries(stepCounts).sort((a, b) => b[1] - a[1])[0][0])
      : null;

  return {
    totalCompletions: completions.length,
    totalAbandons: abandons.length,
    averageCompletionTime: averageTime,
    mostCommonExitStep,
  };
}
