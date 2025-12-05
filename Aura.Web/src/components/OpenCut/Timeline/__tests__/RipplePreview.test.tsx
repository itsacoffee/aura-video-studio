/**
 * Tests for RipplePreview component
 */

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { RipplePreview, type RippleClipPreview } from '../RipplePreview';

describe('RipplePreview', () => {
  const mockAffectedClips: RippleClipPreview[] = [
    {
      clipId: 'clip-1',
      currentPosition: 100,
      newPosition: 200,
      width: 150,
      name: 'Clip 1',
    },
    {
      clipId: 'clip-2',
      currentPosition: 300,
      newPosition: 400,
      width: 100,
      name: 'Clip 2',
    },
  ];

  describe('Visibility', () => {
    it('should not render when visible is false', () => {
      const { container } = render(
        <RipplePreview
          visible={false}
          direction="right"
          timeShift={1}
          affectedClips={mockAffectedClips}
        />
      );

      // AnimatePresence should not render children when not visible
      expect(container.firstChild).toBeNull();
    });

    it('should not render when affectedClips is empty', () => {
      const { container } = render(
        <RipplePreview visible={true} direction="right" timeShift={1} affectedClips={[]} />
      );

      expect(container.firstChild).toBeNull();
    });

    it('should render when visible and has affected clips', () => {
      const { container } = render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={1}
          affectedClips={mockAffectedClips}
        />
      );

      expect(container.firstChild).toBeInTheDocument();
    });
  });

  describe('Direction', () => {
    it('should render with right direction', () => {
      render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={1.5}
          affectedClips={mockAffectedClips}
        />
      );

      // Check for clip names being rendered
      expect(screen.getByText('Clip 1')).toBeInTheDocument();
      expect(screen.getByText('Clip 2')).toBeInTheDocument();
    });

    it('should render with left direction', () => {
      render(
        <RipplePreview
          visible={true}
          direction="left"
          timeShift={1.5}
          affectedClips={mockAffectedClips}
        />
      );

      expect(screen.getByText('Clip 1')).toBeInTheDocument();
      expect(screen.getByText('Clip 2')).toBeInTheDocument();
    });
  });

  describe('Time Shift Display', () => {
    it('should display time shift in seconds', () => {
      render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={2.5}
          affectedClips={mockAffectedClips}
        />
      );

      expect(screen.getByText('+2.50s')).toBeInTheDocument();
    });

    it('should display time shift in milliseconds for small values', () => {
      render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={0.5}
          affectedClips={mockAffectedClips}
        />
      );

      expect(screen.getByText('+500ms')).toBeInTheDocument();
    });
  });

  describe('Ghost Clips', () => {
    it('should render ghost clip for each affected clip', () => {
      render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={1}
          affectedClips={mockAffectedClips}
        />
      );

      // Both clip names should be visible
      expect(screen.getByText('Clip 1')).toBeInTheDocument();
      expect(screen.getByText('Clip 2')).toBeInTheDocument();
    });
  });

  describe('Custom className', () => {
    it('should apply custom className', () => {
      const { container } = render(
        <RipplePreview
          visible={true}
          direction="right"
          timeShift={1}
          affectedClips={mockAffectedClips}
          className="custom-class"
        />
      );

      // The container should have the custom class
      expect(container.firstChild).toHaveClass('custom-class');
    });
  });
});
