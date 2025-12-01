import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { Play24Regular } from '@fluentui/react-icons';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DensityProvider, useDensity } from '../../../contexts/DensityContext';
import { AdaptiveButton } from '../AdaptiveButton';

// Mock useDisplayEnvironment to provide consistent values
vi.mock('../../../hooks/useDisplayEnvironment', () => ({
  useDisplayEnvironment: () => ({
    screenWidth: 1920,
    screenHeight: 1080,
    viewportWidth: 1920,
    viewportHeight: 1080,
    devicePixelRatio: 1,
    sizeClass: 'regular',
    densityClass: 'high',
    aspectRatio: 'landscape',
    effectiveWidth: 1920,
    contentColumns: 3,
    panelLayout: 'side-by-side',
    baseSpacing: 6,
    baseFontSize: 15,
    canShowSecondaryPanels: true,
    canShowDetailInspector: true,
    preferCompactControls: false,
    enableTouchOptimizations: false,
  }),
}));

// Wrapper component for FluentUI and Density context
const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <FluentProvider theme={webLightTheme}>
    <DensityProvider>{children}</DensityProvider>
  </FluentProvider>
);

// Density control component for testing density changes
const DensityController = ({
  setMode,
}: {
  setMode: 'compact' | 'comfortable' | 'spacious' | 'auto';
}) => {
  const { setDensity } = useDensity();
  return <button onClick={() => setDensity(setMode)}>Set {setMode}</button>;
};

describe('AdaptiveButton', () => {
  describe('Rendering', () => {
    it('renders children correctly', () => {
      render(
        <TestWrapper>
          <AdaptiveButton>Button Text</AdaptiveButton>
        </TestWrapper>
      );

      expect(screen.getByRole('button', { name: 'Button Text' })).toBeInTheDocument();
    });

    it('applies custom className', () => {
      render(
        <TestWrapper>
          <AdaptiveButton className="custom-button-class">Custom</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Custom' });
      expect(button).toHaveClass('custom-button-class');
    });
  });

  describe('Full width mode', () => {
    it('renders full width when prop is true', () => {
      render(
        <TestWrapper>
          <AdaptiveButton fullWidth>Full Width Button</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Full Width Button' });
      expect(button).toBeInTheDocument();
    });

    it('does not render full width by default', () => {
      render(
        <TestWrapper>
          <AdaptiveButton>Normal Width</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Normal Width' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Interaction', () => {
    it('calls onClick when clicked', () => {
      const handleClick = vi.fn();

      render(
        <TestWrapper>
          <AdaptiveButton onClick={handleClick}>Click me</AdaptiveButton>
        </TestWrapper>
      );

      fireEvent.click(screen.getByRole('button', { name: 'Click me' }));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('does not call onClick when disabled', () => {
      const handleClick = vi.fn();

      render(
        <TestWrapper>
          <AdaptiveButton disabled onClick={handleClick}>
            Click me
          </AdaptiveButton>
        </TestWrapper>
      );

      fireEvent.click(screen.getByRole('button', { name: 'Click me' }));
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('Density adaptation', () => {
    it('adapts to compact density', () => {
      render(
        <TestWrapper>
          <DensityController setMode="compact" />
          <AdaptiveButton>Adaptive Button</AdaptiveButton>
        </TestWrapper>
      );

      // Set density to compact
      fireEvent.click(screen.getByRole('button', { name: 'Set compact' }));

      const button = screen.getByRole('button', { name: 'Adaptive Button' });
      expect(button).toBeInTheDocument();
    });

    it('adapts to spacious density', () => {
      render(
        <TestWrapper>
          <DensityController setMode="spacious" />
          <AdaptiveButton>Adaptive Button</AdaptiveButton>
        </TestWrapper>
      );

      // Set density to spacious
      fireEvent.click(screen.getByRole('button', { name: 'Set spacious' }));

      const button = screen.getByRole('button', { name: 'Adaptive Button' });
      expect(button).toBeInTheDocument();
    });

    it('uses comfortable density by default', () => {
      render(
        <TestWrapper>
          <AdaptiveButton>Comfortable Button</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Comfortable Button' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Fluent UI integration', () => {
    it('renders with primary appearance', () => {
      render(
        <TestWrapper>
          <AdaptiveButton appearance="primary">Primary</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Primary' });
      expect(button).toBeInTheDocument();
    });

    it('renders with subtle appearance', () => {
      render(
        <TestWrapper>
          <AdaptiveButton appearance="subtle">Subtle</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Subtle' });
      expect(button).toBeInTheDocument();
    });

    it('renders with icon', () => {
      render(
        <TestWrapper>
          <AdaptiveButton icon={<Play24Regular data-testid="icon" />}>With Icon</AdaptiveButton>
        </TestWrapper>
      );

      expect(screen.getByTestId('icon')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('can receive focus', () => {
      render(
        <TestWrapper>
          <AdaptiveButton>Focusable</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Focusable' });
      button.focus();
      expect(document.activeElement).toBe(button);
    });

    it('has correct aria-disabled when disabled', () => {
      render(
        <TestWrapper>
          <AdaptiveButton disabled>Disabled</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Disabled' });
      expect(button).toBeDisabled();
    });

    it('forwards aria attributes', () => {
      render(
        <TestWrapper>
          <AdaptiveButton aria-label="Custom label">Button</AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Custom label' });
      expect(button).toHaveAttribute('aria-label', 'Custom label');
    });
  });

  describe('Props forwarding', () => {
    it('forwards additional props to the Button component', () => {
      render(
        <TestWrapper>
          <AdaptiveButton data-testid="custom-button" type="submit">
            Submit
          </AdaptiveButton>
        </TestWrapper>
      );

      const button = screen.getByTestId('custom-button');
      expect(button).toHaveAttribute('type', 'submit');
    });
  });
});
