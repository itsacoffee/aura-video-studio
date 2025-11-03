import { describe, it, expect } from 'vitest';
import { navItems } from '../navigation';

describe('Advanced Mode Navigation', () => {
  describe('Navigation Items Configuration', () => {
    it('should have advancedOnly property on some items', () => {
      const advancedItems = navItems.filter((item) => item.advancedOnly === true);
      expect(advancedItems.length).toBeGreaterThan(0);
    });

    it('should have non-advanced items available by default', () => {
      const normalItems = navItems.filter((item) => !item.advancedOnly);
      expect(normalItems.length).toBeGreaterThan(0);
    });

    it('should mark Pacing Analyzer as advanced only', () => {
      const pacingItem = navItems.find((item) => item.key === 'pacing');
      expect(pacingItem?.advancedOnly).toBe(true);
    });

    it('should mark AI Editing as advanced only', () => {
      const aiEditingItem = navItems.find((item) => item.key === 'ai-editing');
      expect(aiEditingItem?.advancedOnly).toBe(true);
    });

    it('should mark Visual Aesthetics as advanced only', () => {
      const aestheticsItem = navItems.find((item) => item.key === 'aesthetics');
      expect(aestheticsItem?.advancedOnly).toBe(true);
    });

    it('should mark Prompt Management as advanced only', () => {
      const promptItem = navItems.find((item) => item.key === 'prompt-management');
      expect(promptItem?.advancedOnly).toBe(true);
    });

    it('should mark Performance Analytics as advanced only', () => {
      const perfItem = navItems.find((item) => item.key === 'performance-analytics');
      expect(perfItem?.advancedOnly).toBe(true);
    });

    it('should NOT mark Dashboard as advanced only', () => {
      const dashboardItem = navItems.find((item) => item.key === 'dashboard');
      expect(dashboardItem?.advancedOnly).toBeUndefined();
    });

    it('should NOT mark Create as advanced only', () => {
      const createItem = navItems.find((item) => item.key === 'create');
      expect(createItem?.advancedOnly).toBeUndefined();
    });

    it('should NOT mark Settings as advanced only', () => {
      const settingsItem = navItems.find((item) => item.key === 'settings');
      expect(settingsItem?.advancedOnly).toBeUndefined();
    });
  });

  describe('Navigation Filtering Logic', () => {
    it('should filter out advanced items when advanced mode is false', () => {
      const advancedMode = false;
      const visibleItems = navItems.filter((item) => !item.advancedOnly || advancedMode);
      const hasAdvancedItems = visibleItems.some((item) => item.advancedOnly === true);
      expect(hasAdvancedItems).toBe(false);
    });

    it('should include advanced items when advanced mode is true', () => {
      const advancedMode = true;
      const visibleItems = navItems.filter((item) => !item.advancedOnly || advancedMode);
      const hasAdvancedItems = visibleItems.some((item) => item.advancedOnly === true);
      expect(hasAdvancedItems).toBe(true);
    });

    it('should always show non-advanced items regardless of mode', () => {
      const advancedModeOff = false;
      const advancedModeOn = true;

      const visibleWhenOff = navItems.filter((item) => !item.advancedOnly || advancedModeOff);
      const visibleWhenOn = navItems.filter((item) => !item.advancedOnly || advancedModeOn);

      const normalItems = navItems.filter((item) => !item.advancedOnly);

      normalItems.forEach((normalItem) => {
        expect(visibleWhenOff.some((item) => item.key === normalItem.key)).toBe(true);
        expect(visibleWhenOn.some((item) => item.key === normalItem.key)).toBe(true);
      });
    });

    it('should show fewer items when advanced mode is off', () => {
      const advancedModeOff = false;
      const advancedModeOn = true;

      const visibleWhenOff = navItems.filter((item) => !item.advancedOnly || advancedModeOff);
      const visibleWhenOn = navItems.filter((item) => !item.advancedOnly || advancedModeOn);

      expect(visibleWhenOff.length).toBeLessThan(visibleWhenOn.length);
    });

    it('should show exactly 5 fewer items when advanced mode is off', () => {
      const advancedModeOff = false;
      const advancedModeOn = true;

      const visibleWhenOff = navItems.filter((item) => !item.advancedOnly || advancedModeOff);
      const visibleWhenOn = navItems.filter((item) => !item.advancedOnly || advancedModeOn);

      const difference = visibleWhenOn.length - visibleWhenOff.length;
      expect(difference).toBe(5);
    });
  });
});
