import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { Play24Regular } from '@fluentui/react-icons';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { AuraButton } from '../AuraButton';

// Wrapper component for Fluent UI context
const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <FluentProvider theme={webLightTheme}>{children}</FluentProvider>
);

describe('AuraButton', () => {
  describe('Variants', () => {
    it('renders primary variant by default', () => {
      render(
        <TestWrapper>
          <AuraButton>Primary Button</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Primary Button' });
      expect(button).toBeInTheDocument();
    });

    it('renders secondary variant', () => {
      render(
        <TestWrapper>
          <AuraButton variant="secondary">Secondary Button</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Secondary Button' });
      expect(button).toBeInTheDocument();
    });

    it('renders tertiary variant', () => {
      render(
        <TestWrapper>
          <AuraButton variant="tertiary">Tertiary Button</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Tertiary Button' });
      expect(button).toBeInTheDocument();
    });

    it('renders destructive variant', () => {
      render(
        <TestWrapper>
          <AuraButton variant="destructive">Delete</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Delete' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Sizes', () => {
    it('renders small size', () => {
      render(
        <TestWrapper>
          <AuraButton size="small">Small</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Small' });
      expect(button).toBeInTheDocument();
    });

    it('renders medium size by default', () => {
      render(
        <TestWrapper>
          <AuraButton>Medium</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Medium' });
      expect(button).toBeInTheDocument();
    });

    it('renders large size', () => {
      render(
        <TestWrapper>
          <AuraButton size="large">Large</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Large' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Loading State', () => {
    it('shows spinner when loading', () => {
      render(
        <TestWrapper>
          <AuraButton loading>Loading</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button');
      expect(button).toHaveAttribute('aria-busy', 'true');
    });

    it('disables button when loading', () => {
      render(
        <TestWrapper>
          <AuraButton loading>Loading</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button');
      expect(button).toBeDisabled();
    });

    it('shows loading text when provided', () => {
      render(
        <TestWrapper>
          <AuraButton loading loadingText="Saving...">
            Save
          </AuraButton>
        </TestWrapper>
      );

      expect(screen.getByText('Saving...')).toBeInTheDocument();
    });
  });

  describe('Icons', () => {
    it('renders start icon', () => {
      render(
        <TestWrapper>
          <AuraButton iconStart={<Play24Regular data-testid="start-icon" />}>Play</AuraButton>
        </TestWrapper>
      );

      expect(screen.getByTestId('start-icon')).toBeInTheDocument();
    });

    it('renders end icon', () => {
      render(
        <TestWrapper>
          <AuraButton iconEnd={<Play24Regular data-testid="end-icon" />}>Play</AuraButton>
        </TestWrapper>
      );

      expect(screen.getByTestId('end-icon')).toBeInTheDocument();
    });
  });

  describe('Interaction', () => {
    it('calls onClick when clicked', () => {
      const handleClick = vi.fn();

      render(
        <TestWrapper>
          <AuraButton onClick={handleClick}>Click me</AuraButton>
        </TestWrapper>
      );

      fireEvent.click(screen.getByRole('button', { name: 'Click me' }));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it('does not call onClick when disabled', () => {
      const handleClick = vi.fn();

      render(
        <TestWrapper>
          <AuraButton disabled onClick={handleClick}>
            Click me
          </AuraButton>
        </TestWrapper>
      );

      fireEvent.click(screen.getByRole('button', { name: 'Click me' }));
      expect(handleClick).not.toHaveBeenCalled();
    });

    it('does not call onClick when loading', () => {
      const handleClick = vi.fn();

      render(
        <TestWrapper>
          <AuraButton loading onClick={handleClick}>
            Click me
          </AuraButton>
        </TestWrapper>
      );

      fireEvent.click(screen.getByRole('button'));
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe('Full Width', () => {
    it('renders full width when prop is true', () => {
      render(
        <TestWrapper>
          <AuraButton fullWidth>Full Width</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Full Width' });
      expect(button).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('can receive focus', () => {
      render(
        <TestWrapper>
          <AuraButton>Focusable</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Focusable' });
      button.focus();
      expect(document.activeElement).toBe(button);
    });

    it('has correct aria-disabled when disabled', () => {
      render(
        <TestWrapper>
          <AuraButton disabled>Disabled</AuraButton>
        </TestWrapper>
      );

      const button = screen.getByRole('button', { name: 'Disabled' });
      expect(button).toHaveAttribute('aria-disabled', 'true');
    });
  });
});
