import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect, vi, it } from 'vitest';
import { EnhancedApiKeyInput } from '../EnhancedApiKeyInput';
import type { FieldValidationError } from '../FieldValidationErrors';

describe('EnhancedApiKeyInput', () => {
  const defaultProps = {
    providerDisplayName: 'OpenAI',
    value: '',
    onChange: vi.fn(),
    onValidate: vi.fn(),
    validationStatus: 'idle' as const,
  };

  it('renders with provider name in placeholder', () => {
    render(<EnhancedApiKeyInput {...defaultProps} />);

    expect(screen.getByPlaceholderText('Enter your OpenAI API key')).toBeInTheDocument();
  });

  it('displays field validation errors when provided', () => {
    const fieldErrors: FieldValidationError[] = [
      {
        fieldName: 'ApiKey',
        errorCode: 'INVALID_FORMAT',
        errorMessage: 'OpenAI API keys must start with "sk-"',
        suggestedFix: 'Check your API key format',
      },
    ];

    render(<EnhancedApiKeyInput {...defaultProps} fieldErrors={fieldErrors} />);

    expect(screen.getByText(/ApiKey:/)).toBeInTheDocument();
    expect(screen.getByText(/OpenAI API keys must start with "sk-"/)).toBeInTheDocument();
    expect(screen.getByText(/💡 Check your API key format/)).toBeInTheDocument();
  });

  it('displays account info when validation is successful', () => {
    render(
      <EnhancedApiKeyInput
        {...defaultProps}
        validationStatus="valid"
        accountInfo="Organization: Test Org"
      />
    );

    expect(screen.getByText('✓ API key validated successfully')).toBeInTheDocument();
    expect(screen.getByText('Organization: Test Org')).toBeInTheDocument();
  });

  it('calls onChange when value changes', () => {
    const onChange = vi.fn();
    render(<EnhancedApiKeyInput {...defaultProps} onChange={onChange} />);

    const input = screen.getByPlaceholderText('Enter your OpenAI API key');
    fireEvent.change(input, { target: { value: 'sk-test123' } });

    expect(onChange).toHaveBeenCalledWith('sk-test123');
  });

  it('calls onValidate when validate button is clicked', () => {
    const onValidate = vi.fn();
    render(<EnhancedApiKeyInput {...defaultProps} value="sk-test123" onValidate={onValidate} />);

    const button = screen.getByText('Validate');
    fireEvent.click(button);

    expect(onValidate).toHaveBeenCalled();
  });

  it('disables validate button when validating', () => {
    render(
      <EnhancedApiKeyInput {...defaultProps} value="sk-test123" validationStatus="validating" />
    );

    const button = screen.getByText('Validating...');
    expect(button).toBeDisabled();
  });

  it('renders skip button when onSkipValidation is provided', () => {
    const onSkipValidation = vi.fn();
    render(<EnhancedApiKeyInput {...defaultProps} onSkipValidation={onSkipValidation} />);

    expect(screen.getByText('Skip')).toBeInTheDocument();
  });
});
