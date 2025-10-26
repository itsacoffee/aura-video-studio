import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import {
  trackEvent,
  wizardAnalytics,
  getStoredEvents,
  clearStoredEvents,
  getWizardStats,
} from '../../services/analytics';

// Mock localStorage
const localStorageMock = (() => {
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
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
});

describe('Analytics Service', () => {
  beforeEach(() => {
    localStorageMock.clear();
    clearStoredEvents();
  });

  afterEach(() => {
    localStorageMock.clear();
    clearStoredEvents();
  });

  describe('trackEvent', () => {
    it('should store event in localStorage', () => {
      trackEvent('test_event', 'test_category', { key: 'value' });

      const events = getStoredEvents();
      expect(events).toHaveLength(1);
      expect(events[0].name).toBe('test_event');
      expect(events[0].category).toBe('test_category');
      expect(events[0].properties).toEqual({ key: 'value' });
    });

    it('should store event with timestamp', () => {
      const beforeTime = new Date();
      trackEvent('test_event', 'test_category');
      const afterTime = new Date();

      const events = getStoredEvents();
      const eventTime = new Date(events[0].timestamp);

      expect(eventTime.getTime()).toBeGreaterThanOrEqual(beforeTime.getTime());
      expect(eventTime.getTime()).toBeLessThanOrEqual(afterTime.getTime());
    });

    it('should limit stored events to 100', () => {
      // Add 150 events
      for (let i = 0; i < 150; i++) {
        trackEvent(`event_${i}`, 'test');
      }

      const events = getStoredEvents();
      expect(events).toHaveLength(100);
      // Should keep the last 100 events
      expect(events[0].name).toBe('event_50');
      expect(events[99].name).toBe('event_149');
    });
  });

  describe('wizardAnalytics', () => {
    it('should track wizard started', () => {
      wizardAnalytics.started();

      const events = getStoredEvents();
      expect(events).toHaveLength(1);
      expect(events[0].name).toBe('wizard_started');
      expect(events[0].category).toBe('onboarding');
    });

    it('should track step viewed', () => {
      wizardAnalytics.stepViewed(1, 'Choose Tier');

      const events = getStoredEvents();
      expect(events).toHaveLength(1);
      expect(events[0].name).toBe('wizard_step_viewed');
      expect(events[0].properties).toEqual({
        step_number: 1,
        step_name: 'Choose Tier',
      });
    });

    it('should track step completed with time', () => {
      wizardAnalytics.stepCompleted(1, 'Choose Tier', 30);

      const events = getStoredEvents();
      expect(events[0].name).toBe('wizard_step_completed');
      expect(events[0].properties).toEqual({
        step_number: 1,
        step_name: 'Choose Tier',
        time_spent_seconds: 30,
      });
    });

    it('should track tier selection', () => {
      wizardAnalytics.tierSelected('pro');

      const events = getStoredEvents();
      expect(events[0].name).toBe('tier_selected');
      expect(events[0].properties).toEqual({ tier: 'pro' });
    });

    it('should track template selection', () => {
      wizardAnalytics.templateSelected('youtube');

      const events = getStoredEvents();
      expect(events[0].name).toBe('template_selected');
      expect(events[0].properties).toEqual({ template_id: 'youtube' });
    });

    it('should track dependency installation', () => {
      wizardAnalytics.dependencyInstalled('ffmpeg', 'auto');

      const events = getStoredEvents();
      expect(events[0].name).toBe('dependency_installed');
      expect(events[0].properties).toEqual({
        dependency_id: 'ffmpeg',
        install_method: 'auto',
      });
    });

    it('should track wizard completion', () => {
      wizardAnalytics.completed(300, { tier: 'pro', api_keys_count: 2 });

      const events = getStoredEvents();
      expect(events[0].name).toBe('wizard_completed');
      expect(events[0].properties?.total_time_seconds).toBe(300);
      expect(events[0].properties?.tier).toBe('pro');
      expect(events[0].properties?.api_keys_count).toBe(2);
    });
  });

  describe('getWizardStats', () => {
    it('should return zero stats when no events', () => {
      const stats = getWizardStats();

      expect(stats.totalCompletions).toBe(0);
      expect(stats.totalAbandons).toBe(0);
      expect(stats.averageCompletionTime).toBe(0);
      expect(stats.mostCommonExitStep).toBeNull();
    });

    it('should calculate completion stats', () => {
      wizardAnalytics.completed(300, {});
      wizardAnalytics.completed(600, {});

      const stats = getWizardStats();

      expect(stats.totalCompletions).toBe(2);
      expect(stats.averageCompletionTime).toBe(450);
    });

    it('should calculate abandon stats', () => {
      wizardAnalytics.stepAbandoned(2, 'Dependencies');
      wizardAnalytics.stepAbandoned(2, 'Dependencies');
      wizardAnalytics.stepAbandoned(4, 'Workspace');

      const stats = getWizardStats();

      expect(stats.totalAbandons).toBe(3);
      expect(stats.mostCommonExitStep).toBe(2);
    });
  });

  describe('clearStoredEvents', () => {
    it('should clear all stored events', () => {
      trackEvent('event1', 'category');
      trackEvent('event2', 'category');

      expect(getStoredEvents()).toHaveLength(2);

      clearStoredEvents();

      expect(getStoredEvents()).toHaveLength(0);
    });
  });
});
