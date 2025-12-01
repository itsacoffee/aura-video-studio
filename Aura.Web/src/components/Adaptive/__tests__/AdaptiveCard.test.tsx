import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DensityProvider, useDensity } from '../../../contexts/DensityContext';
import { AdaptiveCard } from '../AdaptiveCard';

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

describe('AdaptiveCard', () => {
  describe('Rendering', () => {
    it('renders children correctly', () => {
      render(
        <TestWrapper>
          <AdaptiveCard>Card Content</AdaptiveCard>
        </TestWrapper>
      );

      expect(screen.getByText('Card Content')).toBeInTheDocument();
    });

    it('applies custom className', () => {
      const { container } = render(
        <TestWrapper>
          <AdaptiveCard className="custom-class">Content</AdaptiveCard>
        </TestWrapper>
      );

      const card = container.querySelector('.custom-class');
      expect(card).toBeInTheDocument();
    });
  });

  describe('Variants', () => {
    it('renders elevated variant by default', () => {
      render(
        <TestWrapper>
          <AdaptiveCard data-testid="adaptive-card">Elevated Card</AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('adaptive-card');
      expect(card).toBeInTheDocument();
    });

    it('renders outlined variant', () => {
      render(
        <TestWrapper>
          <AdaptiveCard variant="outlined" data-testid="outlined-card">
            Outlined Card
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('outlined-card');
      expect(card).toBeInTheDocument();
    });

    it('renders filled variant', () => {
      render(
        <TestWrapper>
          <AdaptiveCard variant="filled" data-testid="filled-card">
            Filled Card
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('filled-card');
      expect(card).toBeInTheDocument();
    });
  });

  describe('Interactive mode', () => {
    it('renders as interactive when prop is true', () => {
      render(
        <TestWrapper>
          <AdaptiveCard interactive data-testid="interactive-card">
            Interactive Card
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('interactive-card');
      expect(card).toBeInTheDocument();
    });

    it('handles click events when interactive', () => {
      const handleClick = vi.fn();
      render(
        <TestWrapper>
          <AdaptiveCard interactive onClick={handleClick} data-testid="clickable-card">
            Clickable Card
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('clickable-card');
      fireEvent.click(card);
      expect(handleClick).toHaveBeenCalledTimes(1);
    });
  });

  describe('Full height mode', () => {
    it('renders full height when prop is true', () => {
      render(
        <TestWrapper>
          <AdaptiveCard fullHeight data-testid="full-height-card">
            Full Height Card
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('full-height-card');
      expect(card).toBeInTheDocument();
    });
  });

  describe('Density adaptation', () => {
    it('adapts to compact density', () => {
      render(
        <TestWrapper>
          <DensityController setMode="compact" />
          <AdaptiveCard data-testid="adaptive-card">Content</AdaptiveCard>
        </TestWrapper>
      );

      // Set density to compact
      fireEvent.click(screen.getByRole('button', { name: 'Set compact' }));

      const card = screen.getByTestId('adaptive-card');
      expect(card).toBeInTheDocument();
    });

    it('adapts to spacious density', () => {
      render(
        <TestWrapper>
          <DensityController setMode="spacious" />
          <AdaptiveCard data-testid="adaptive-card">Content</AdaptiveCard>
        </TestWrapper>
      );

      // Set density to spacious
      fireEvent.click(screen.getByRole('button', { name: 'Set spacious' }));

      const card = screen.getByTestId('adaptive-card');
      expect(card).toBeInTheDocument();
    });

    it('uses auto density when set', () => {
      render(
        <TestWrapper>
          <DensityController setMode="auto" />
          <AdaptiveCard data-testid="adaptive-card">Content</AdaptiveCard>
        </TestWrapper>
      );

      // Set density to auto
      fireEvent.click(screen.getByRole('button', { name: 'Set auto' }));

      const card = screen.getByTestId('adaptive-card');
      expect(card).toBeInTheDocument();
    });
  });

  describe('Props forwarding', () => {
    it('forwards additional props to the Card component', () => {
      render(
        <TestWrapper>
          <AdaptiveCard data-testid="card-with-props" aria-label="Test card">
            Content
          </AdaptiveCard>
        </TestWrapper>
      );

      const card = screen.getByTestId('card-with-props');
      expect(card).toHaveAttribute('aria-label', 'Test card');
    });
  });
});
