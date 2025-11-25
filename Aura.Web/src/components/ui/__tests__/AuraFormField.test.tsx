import { FluentProvider, webLightTheme, Input } from '@fluentui/react-components';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { AuraFormField } from '../AuraFormField';

// Wrapper component for Fluent UI context
const TestWrapper = ({ children }: { children: React.ReactNode }) => (
  <FluentProvider theme={webLightTheme}>{children}</FluentProvider>
);

describe('AuraFormField', () => {
  describe('Basic Rendering', () => {
    it('renders with label', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Email')).toBeInTheDocument();
    });

    it('renders child control', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email">
            <Input placeholder="Enter email" />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByPlaceholderText('Enter email')).toBeInTheDocument();
    });

    it('renders without label', () => {
      render(
        <TestWrapper>
          <AuraFormField>
            <Input placeholder="No label" />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByPlaceholderText('No label')).toBeInTheDocument();
    });
  });

  describe('Required Field', () => {
    it('shows required indicator', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" required>
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('*')).toBeInTheDocument();
    });

    it('does not show required indicator when not required', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Optional field">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.queryByText('*')).not.toBeInTheDocument();
    });
  });

  describe('Error Handling', () => {
    it('displays error message', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" error="Invalid email address">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Invalid email address')).toBeInTheDocument();
    });

    it('sets aria-invalid on input when error exists', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" error="Invalid">
            <Input data-testid="email-input" />
          </AuraFormField>
        </TestWrapper>
      );

      const input = screen.getByTestId('email-input');
      expect(input).toHaveAttribute('aria-invalid', 'true');
    });

    it('has alert role for error message', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" error="Error message">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  describe('Helper Text', () => {
    it('displays helper text', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" helperText="We will never share your email">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('We will never share your email')).toBeInTheDocument();
    });

    it('shows error instead of helper text when both are present', () => {
      render(
        <TestWrapper>
          <AuraFormField
            label="Email"
            error="Invalid email"
            helperText="We will never share your email"
          >
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Invalid email')).toBeInTheDocument();
      expect(screen.queryByText('We will never share your email')).not.toBeInTheDocument();
    });
  });

  describe('Success Message', () => {
    it('displays success message', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" successMessage="Email is valid!">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Email is valid!')).toBeInTheDocument();
    });

    it('shows error instead of success when both are present', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" error="Error" successMessage="Success">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Error')).toBeInTheDocument();
      expect(screen.queryByText('Success')).not.toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('associates label with input', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email">
            <Input data-testid="email-input" />
          </AuraFormField>
        </TestWrapper>
      );

      const input = screen.getByTestId('email-input');
      const label = screen.getByText('Email');

      // Label should have htmlFor matching input id
      expect(label).toHaveAttribute('for');
      expect(input).toHaveAttribute('id');
      expect(label.getAttribute('for')).toBe(input.getAttribute('id'));
    });

    it('connects input to error message via aria-describedby', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" error="Invalid email">
            <Input data-testid="email-input" />
          </AuraFormField>
        </TestWrapper>
      );

      const input = screen.getByTestId('email-input');
      expect(input).toHaveAttribute('aria-describedby');
    });
  });

  describe('Orientation', () => {
    it('renders vertical orientation by default', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Email')).toBeInTheDocument();
    });

    it('supports horizontal orientation', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" orientation="horizontal">
            <Input />
          </AuraFormField>
        </TestWrapper>
      );

      expect(screen.getByText('Email')).toBeInTheDocument();
    });
  });

  describe('Hidden Label', () => {
    it('hides label visually but keeps it accessible', () => {
      render(
        <TestWrapper>
          <AuraFormField label="Email" hideLabel>
            <Input data-testid="email-input" />
          </AuraFormField>
        </TestWrapper>
      );

      // Label should still be in the document for accessibility
      expect(screen.getByText('Email')).toBeInTheDocument();
    });
  });
});
