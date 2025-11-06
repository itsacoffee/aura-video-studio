/**
 * Tests for path utilities
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import {
  getDefaultSaveLocation,
  getDefaultCacheLocation,
  isValidPath,
  migrateLegacyPath,
} from '../../utils/pathUtils';

describe('pathUtils', () => {
  describe('getDefaultSaveLocation', () => {
    beforeEach(() => {
      // Mock navigator
      Object.defineProperty(window, 'navigator', {
        writable: true,
        value: {
          platform: '',
          userAgent: '',
        },
      });
    });

    it('returns Windows path for Windows platform', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'Win32',
      });

      const path = getDefaultSaveLocation();
      expect(path).toBe('%USERPROFILE%\\Videos\\Aura');
    });

    it('returns macOS path for Mac platform', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'MacIntel',
      });

      const path = getDefaultSaveLocation();
      expect(path).toBe('~/Movies/Aura');
    });

    it('returns Linux path for other platforms', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'Linux x86_64',
      });

      const path = getDefaultSaveLocation();
      expect(path).toBe('~/Videos/Aura');
    });
  });

  describe('getDefaultCacheLocation', () => {
    it('returns Windows cache path for Windows platform', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'Win32',
      });

      const path = getDefaultCacheLocation();
      expect(path).toBe('%LOCALAPPDATA%\\Aura\\Cache');
    });

    it('returns macOS cache path for Mac platform', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'MacIntel',
      });

      const path = getDefaultCacheLocation();
      expect(path).toBe('~/Library/Caches/Aura');
    });

    it('returns Linux cache path for other platforms', () => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'Linux x86_64',
      });

      const path = getDefaultCacheLocation();
      expect(path).toBe('~/.cache/aura');
    });
  });

  describe('isValidPath', () => {
    it('returns true for valid paths', () => {
      expect(isValidPath('C:\\Users\\John\\Videos')).toBe(true);
      expect(isValidPath('/home/john/videos')).toBe(true);
      expect(isValidPath('~/Videos/Aura')).toBe(true);
      expect(isValidPath('%USERPROFILE%\\Videos\\Aura')).toBe(true);
    });

    it('returns false for empty paths', () => {
      expect(isValidPath('')).toBe(false);
      expect(isValidPath('   ')).toBe(false);
    });

    it('returns false for paths with invalid characters', () => {
      expect(isValidPath('C:\\Users\\<invalid>\\Videos')).toBe(false);
      expect(isValidPath('/home/john|invalid')).toBe(false);
      expect(isValidPath('~/Videos?*')).toBe(false);
    });

    it('returns false for paths with placeholder text', () => {
      expect(isValidPath('C:\\Users\\YourName\\Videos')).toBe(false);
      expect(isValidPath('/home/username/videos')).toBe(false);
      expect(isValidPath('~/User Name/Aura')).toBe(false);
      expect(isValidPath('/path/<user>/videos')).toBe(false);
      expect(isValidPath('/path/{user}/videos')).toBe(false);
    });
  });

  describe('migrateLegacyPath', () => {
    beforeEach(() => {
      Object.defineProperty(navigator, 'platform', {
        writable: true,
        value: 'Win32',
      });
    });

    it('returns default path for invalid paths', () => {
      const result = migrateLegacyPath('');
      expect(result).toBe('%USERPROFILE%\\Videos\\Aura');
    });

    it('migrates Windows paths with YourName placeholder', () => {
      const result = migrateLegacyPath('C:\\Users\\YourName\\Videos\\Aura');
      expect(result).toBe('%USERPROFILE%\\Videos\\Aura');
    });

    it('migrates Unix paths with YourName placeholder', () => {
      const result = migrateLegacyPath('/home/YourName/Videos/Aura');
      expect(result).toBe('%USERPROFILE%\\Videos\\Aura');
    });

    it('migrates paths with username placeholder', () => {
      const result = migrateLegacyPath('C:\\Users\\username\\Videos');
      expect(result).toBe('%USERPROFILE%\\Videos\\Aura');
    });

    it('returns unchanged path if no placeholders found', () => {
      const validPath = 'C:\\Users\\John\\Videos\\Aura';
      const result = migrateLegacyPath(validPath);
      expect(result).toBe(validPath);
    });
  });
});
