/**
 * Tests for GraphicsContext
 *
 * These tests verify the basic structure and error handling of the GraphicsContext.
 * The useGraphicsSettings hook (used internally) is tested separately in
 * hooks/__tests__/useGraphicsSettings.test.ts
 */

import { renderHook } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { useGraphics } from '../GraphicsContext';

describe('GraphicsContext', () => {
  describe('useGraphics hook', () => {
    it('should throw error when used outside provider', () => {
      expect(() => {
        renderHook(() => useGraphics());
      }).toThrow('useGraphics must be used within a GraphicsProvider');
    });
  });
});
