/**
 * Tests for Premium Visual Effects Components
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { AcrylicPanel } from '../AcrylicPanel';
import { ElevatedCard } from '../ElevatedCard';
import { MagneticButton } from '../MagneticButton';
import { GlowBorder } from '../GlowBorder';
import { PulseRing } from '../PulseRing';
import { ShimmerText } from '../ShimmerText';

// Mock the useGraphics hook
const mockUseGraphics = vi.fn();
vi.mock('../../../contexts/GraphicsContext', () => ({
  useGraphics: () => mockUseGraphics(),
}));

describe('Premium Visual Effects Components', () => {
  beforeEach(() => {
    // Reset mock to default enabled state
    mockUseGraphics.mockReturnValue({
      settings: {
        effects: {
          animations: true,
          blurEffects: true,
          shadows: true,
          transparency: true,
          smoothScrolling: true,
          springPhysics: true,
          parallaxEffects: true,
          glowEffects: true,
          microInteractions: true,
          staggeredAnimations: true,
        },
      },
      animationsEnabled: true,
      blurEnabled: true,
      shadowsEnabled: true,
      gpuEnabled: true,
      reducedMotion: false,
    });
  });

  describe('AcrylicPanel', () => {
    it('renders children content', () => {
      render(
        <AcrylicPanel>
          <span>Test Content</span>
        </AcrylicPanel>
      );
      expect(screen.getByText('Test Content')).toBeInTheDocument();
    });

    it('renders solid panel when blur is disabled', () => {
      mockUseGraphics.mockReturnValue({
        settings: {
          effects: {
            microInteractions: false,
          },
        },
        blurEnabled: false,
        animationsEnabled: false,
        shadowsEnabled: false,
        gpuEnabled: false,
        reducedMotion: false,
      });

      render(
        <AcrylicPanel>
          <span>Test Content</span>
        </AcrylicPanel>
      );
      expect(screen.getByText('Test Content')).toBeInTheDocument();
    });

    it('applies custom className', () => {
      const { container } = render(
        <AcrylicPanel className="custom-class">
          <span>Test Content</span>
        </AcrylicPanel>
      );
      expect(container.querySelector('.custom-class')).toBeInTheDocument();
    });
  });

  describe('ElevatedCard', () => {
    it('renders children content', () => {
      render(
        <ElevatedCard>
          <span>Card Content</span>
        </ElevatedCard>
      );
      expect(screen.getByText('Card Content')).toBeInTheDocument();
    });

    it('calls onClick when clicked', () => {
      const handleClick = vi.fn();
      render(
        <ElevatedCard onClick={handleClick}>
          <span>Card Content</span>
        </ElevatedCard>
      );
      fireEvent.click(screen.getByText('Card Content'));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('supports keyboard activation with Enter', () => {
      const handleClick = vi.fn();
      render(
        <ElevatedCard onClick={handleClick}>
          <span>Card Content</span>
        </ElevatedCard>
      );
      const card = screen.getByRole('button');
      fireEvent.keyDown(card, { key: 'Enter' });
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('renders static card when animations disabled', () => {
      mockUseGraphics.mockReturnValue({
        settings: {
          effects: {
            springPhysics: false,
          },
        },
        animationsEnabled: false,
        blurEnabled: false,
        shadowsEnabled: false,
        gpuEnabled: false,
        reducedMotion: false,
      });

      render(
        <ElevatedCard>
          <span>Card Content</span>
        </ElevatedCard>
      );
      expect(screen.getByText('Card Content')).toBeInTheDocument();
    });
  });

  describe('MagneticButton', () => {
    it('renders children content', () => {
      render(<MagneticButton>Click Me</MagneticButton>);
      expect(screen.getByText('Click Me')).toBeInTheDocument();
    });

    it('calls onClick when clicked', () => {
      const handleClick = vi.fn();
      render(<MagneticButton onClick={handleClick}>Click Me</MagneticButton>);
      fireEvent.click(screen.getByText('Click Me'));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('renders simple button when animations disabled', () => {
      mockUseGraphics.mockReturnValue({
        settings: {
          effects: {
            microInteractions: false,
            glowEffects: false,
          },
        },
        animationsEnabled: false,
        blurEnabled: false,
        shadowsEnabled: false,
        gpuEnabled: false,
        reducedMotion: false,
      });

      render(<MagneticButton>Click Me</MagneticButton>);
      expect(screen.getByText('Click Me')).toBeInTheDocument();
    });

    it('renders disabled button correctly', () => {
      render(<MagneticButton disabled>Click Me</MagneticButton>);
      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });
  });

  describe('GlowBorder', () => {
    it('renders children content', () => {
      render(
        <GlowBorder>
          <span>Glowing Content</span>
        </GlowBorder>
      );
      expect(screen.getByText('Glowing Content')).toBeInTheDocument();
    });

    it('applies custom className', () => {
      const { container } = render(
        <GlowBorder className="custom-glow">
          <span>Content</span>
        </GlowBorder>
      );
      expect(container.querySelector('.custom-glow')).toBeInTheDocument();
    });
  });

  describe('PulseRing', () => {
    it('renders children content', () => {
      render(
        <PulseRing>
          <span>Pulsing Content</span>
        </PulseRing>
      );
      expect(screen.getByText('Pulsing Content')).toBeInTheDocument();
    });

    it('renders with custom size', () => {
      const { container } = render(
        <PulseRing size={60}>
          <span>Content</span>
        </PulseRing>
      );
      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveStyle({ width: '60px', height: '60px' });
    });
  });

  describe('ShimmerText', () => {
    it('renders children content', () => {
      render(<ShimmerText>Shimmering Text</ShimmerText>);
      expect(screen.getByText('Shimmering Text')).toBeInTheDocument();
    });

    it('applies custom className', () => {
      const { container } = render(<ShimmerText className="custom-shimmer">Text</ShimmerText>);
      expect(container.querySelector('.custom-shimmer')).toBeInTheDocument();
    });
  });
});
