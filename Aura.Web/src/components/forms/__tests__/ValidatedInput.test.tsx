/**
 * Tests for ValidatedInput component
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ValidatedInput } from '../ValidatedInput';

const renderWithProvider = (component: React.ReactElement) => {
  return render(<FluentProvider theme={webLightTheme}>{component}</FluentProvider>);
};

describe('ValidatedInput', () => {
  it('should render input with label', () => {
    renderWithProvider(<ValidatedInput label="Test Field" value="" onChange={() => {}} />);
    expect(screen.getByText('Test Field')).toBeInTheDocument();
  });

  it('should show required indicator', () => {
    renderWithProvider(<ValidatedInput label="Required Field" required value="" onChange={() => {}} />);
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('should display hint text', () => {
    renderWithProvider(
      <ValidatedInput label="Email" value="" onChange={() => {}} hint="Enter your email address" />
    );
    expect(screen.getByText('Enter your email address')).toBeInTheDocument();
  });

  it('should display error message', () => {
    renderWithProvider(
      <ValidatedInput
        label="Email"
        value="invalid"
        onChange={() => {}}
        error="Invalid email format"
      />
    );
    expect(screen.getByText('Invalid email format')).toBeInTheDocument();
  });

  it('should call onChange handler', async () => {
    const handleChange = vi.fn();
    const user = userEvent.setup();
    
    renderWithProvider(<ValidatedInput label="Name" value="" onChange={handleChange} />);
    
    const input = screen.getByRole('textbox');
    await user.type(input, 'John');
    
    expect(handleChange).toHaveBeenCalled();
  });

  it('should show validation spinner when validating', () => {
    renderWithProvider(
      <ValidatedInput label="Field" value="test" onChange={() => {}} isValidating={true} />
    );
    // The spinner should be in the document
    const spinners = document.querySelectorAll('[role="progressbar"]');
    expect(spinners.length).toBeGreaterThan(0);
  });

  it('should show success message when valid', () => {
    renderWithProvider(
      <ValidatedInput
        label="Field"
        value="test"
        onChange={() => {}}
        isValid={true}
        successMessage="Looks good!"
      />
    );
    expect(screen.getByText('Looks good!')).toBeInTheDocument();
  });

  it('should prefer error over hint', () => {
    renderWithProvider(
      <ValidatedInput
        label="Field"
        value="test"
        onChange={() => {}}
        error="Error message"
        hint="Hint message"
      />
    );
    expect(screen.getByText('Error message')).toBeInTheDocument();
    expect(screen.queryByText('Hint message')).not.toBeInTheDocument();
  });

  it('should render with placeholder', () => {
    renderWithProvider(
      <ValidatedInput
        label="Field"
        value=""
        onChange={() => {}}
        placeholder="Enter value here"
      />
    );
    expect(screen.getByPlaceholderText('Enter value here')).toBeInTheDocument();
  });

  it('should support password type', () => {
    renderWithProvider(
      <ValidatedInput label="Password" type="password" value="secret" onChange={() => {}} />
    );
    const input = screen.getByLabelText('Password');
    expect(input).toHaveAttribute('type', 'password');
  });
});
