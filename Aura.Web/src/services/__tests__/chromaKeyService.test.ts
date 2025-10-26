/**
 * Tests for chromaKeyService
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { hexToRgb, colorDistance } from '../chromaKeyService';

// Mock ImageData for tests
beforeAll(() => {
  if (typeof ImageData === 'undefined') {
    // Create a simple ImageData mock for testing
    global.ImageData = class ImageData {
      data: Uint8ClampedArray;
      width: number;
      height: number;

      constructor(width: number, height: number) {
        this.width = width;
        this.height = height;
        this.data = new Uint8ClampedArray(width * height * 4);
      }
    } as unknown as typeof ImageData;
  }
});

describe('chromaKeyService', () => {
  describe('hexToRgb', () => {
    it('should convert green hex to RGB', () => {
      const result = hexToRgb('#00ff00');
      expect(result).toEqual({ r: 0, g: 1, b: 0 });
    });

    it('should convert blue hex to RGB', () => {
      const result = hexToRgb('#0000ff');
      expect(result).toEqual({ r: 0, g: 0, b: 1 });
    });

    it('should handle hex without hash', () => {
      const result = hexToRgb('00ff00');
      expect(result).toEqual({ r: 0, g: 1, b: 0 });
    });

    it('should return default green for invalid hex', () => {
      const result = hexToRgb('invalid');
      expect(result).toEqual({ r: 0, g: 1, b: 0 });
    });
  });

  describe('colorDistance', () => {
    it('should calculate distance between identical colors as 0', () => {
      const distance = colorDistance(1, 0, 0, 1, 0, 0);
      expect(distance).toBe(0);
    });

    it('should calculate distance between different colors', () => {
      const distance = colorDistance(1, 0, 0, 0, 1, 0);
      expect(distance).toBeGreaterThan(0);
    });

    it('should be symmetric', () => {
      const d1 = colorDistance(1, 0, 0, 0, 1, 0);
      const d2 = colorDistance(0, 1, 0, 1, 0, 0);
      expect(d1).toBe(d2);
    });
  });
});
