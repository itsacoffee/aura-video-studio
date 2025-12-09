/**
 * Tests for SnapIndicator component
 */

import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SnapIndicator, type SnapType } from '../SnapIndicator';

describe('SnapIndicator', () => {
  describe('Visibility', () => {
    it('should not render when visible is false', () => {
      const { container } = render(<SnapIndicator position={100} visible={false} />);

      // AnimatePresence should not render children when not visible
      expect(container.firstChild).toBeNull();
    });

    it('should render when visible is true', () => {
      const { container } = render(<SnapIndicator position={100} visible={true} />);

      expect(container.firstChild).toBeInTheDocument();
    });
  });

  describe('Positioning', () => {
    it('should position at specified pixel position', () => {
      const { container } = render(<SnapIndicator position={250} visible={true} />);

      // The container should have left position set (position - 1 for centering)
      const indicator = container.firstChild as HTMLElement;
      expect(indicator).toHaveStyle({ left: '249px' });
    });

    it('should handle zero position', () => {
      const { container } = render(<SnapIndicator position={0} visible={true} />);

      const indicator = container.firstChild as HTMLElement;
      expect(indicator).toHaveStyle({ left: '-1px' });
    });
  });

  describe('Snap Types', () => {
    const snapTypes: SnapType[] = ['clip-edge', 'marker', 'playhead', 'time-zero'];

    snapTypes.forEach((snapType) => {
      it(`should render with ${snapType} snap type`, () => {
        const { container } = render(
          <SnapIndicator position={100} visible={true} snapType={snapType} />
        );

        expect(container.firstChild).toBeInTheDocument();
      });
    });

    it('should default to clip-edge snap type', () => {
      const { container } = render(<SnapIndicator position={100} visible={true} />);

      // Should render without error with default snap type
      expect(container.firstChild).toBeInTheDocument();
    });
  });

  describe('Custom className', () => {
    it('should apply custom className', () => {
      const { container } = render(
        <SnapIndicator position={100} visible={true} className="custom-snap-class" />
      );

      expect(container.firstChild).toHaveClass('custom-snap-class');
    });
  });

  describe('Structure', () => {
    it('should render diamond and line elements', () => {
      const { container } = render(<SnapIndicator position={100} visible={true} />);

      // Should have child elements for the visual indicator
      const indicator = container.firstChild as HTMLElement;
      expect(indicator.children.length).toBeGreaterThan(0);
    });
  });
});
