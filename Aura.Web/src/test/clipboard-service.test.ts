/**
 * Tests for ClipboardService
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ClipboardService } from '../services/timeline/ClipboardService';
import type { TimelineScene } from '../types/timeline';

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

describe('ClipboardService', () => {
  let service: ClipboardService;
  let mockScenes: TimelineScene[];

  beforeEach(() => {
    service = new ClipboardService();
    mockScenes = [
      {
        index: 0,
        heading: 'Scene 1',
        script: 'Test script',
        start: 0,
        duration: 10,
        visualAssets: [],
        transitionType: 'fade',
      },
      {
        index: 1,
        heading: 'Scene 2',
        script: 'Test script 2',
        start: 10,
        duration: 15,
        visualAssets: [],
        transitionType: 'fade',
      },
    ];
  });

  afterEach(() => {
    service.clear();
    localStorage.clear();
  });

  describe('copy', () => {
    it('should copy scenes to clipboard', () => {
      service.copy(mockScenes);
      expect(service.hasData()).toBe(true);
    });

    it('should deep clone scenes', () => {
      service.copy(mockScenes);
      mockScenes[0].heading = 'Modified';
      
      const pasted = service.paste(0);
      expect(pasted?.[0].heading).toBe('Scene 1');
    });

    it('should save to localStorage', () => {
      service.copy(mockScenes);
      
      const stored = localStorage.getItem('aura-timeline-clipboard');
      expect(stored).toBeTruthy();
      
      const data = JSON.parse(stored!);
      expect(data.scenes).toHaveLength(2);
    });
  });

  describe('paste', () => {
    beforeEach(() => {
      service.copy(mockScenes);
    });

    it('should paste scenes at specified time', () => {
      const pasted = service.paste(20);
      
      expect(pasted).toHaveLength(2);
      expect(pasted![0].start).toBe(20);
      expect(pasted![1].start).toBe(30); // 20 + 10 duration
    });

    it('should return null if no clipboard data', () => {
      service.clear();
      expect(service.paste(0)).toBeNull();
    });

    it('should preserve scene properties except timing', () => {
      const pasted = service.paste(100);
      
      expect(pasted![0].heading).toBe('Scene 1');
      expect(pasted![0].script).toBe('Test script');
      expect(pasted![0].duration).toBe(10);
    });
  });

  describe('duplicate', () => {
    it('should copy and paste scenes', () => {
      const duplicated = service.duplicate(mockScenes, 25);
      
      expect(duplicated).toHaveLength(2);
      expect(duplicated[0].start).toBe(25);
      expect(duplicated[0].heading).toBe('Scene 1');
    });

    it('should update clipboard', () => {
      service.duplicate(mockScenes, 25);
      expect(service.hasData()).toBe(true);
    });
  });

  describe('hasData', () => {
    it('should return false initially', () => {
      expect(service.hasData()).toBe(false);
    });

    it('should return true after copy', () => {
      service.copy(mockScenes);
      expect(service.hasData()).toBe(true);
    });

    it('should return false after clear', () => {
      service.copy(mockScenes);
      service.clear();
      expect(service.hasData()).toBe(false);
    });
  });

  describe('clear', () => {
    it('should clear clipboard data', () => {
      service.copy(mockScenes);
      service.clear();
      
      expect(service.hasData()).toBe(false);
      expect(service.paste(0)).toBeNull();
    });

    it('should clear localStorage', () => {
      service.copy(mockScenes);
      service.clear();
      
      const stored = localStorage.getItem('aura-timeline-clipboard');
      expect(stored).toBeNull();
    });
  });

  describe('localStorage persistence', () => {
    it('should restore from localStorage', () => {
      const service1 = new ClipboardService();
      service1.copy(mockScenes);
      
      // Create new instance
      const service2 = new ClipboardService();
      expect(service2.hasData()).toBe(true);
      
      const pasted = service2.paste(0);
      expect(pasted).toHaveLength(2);
    });
  });
});
