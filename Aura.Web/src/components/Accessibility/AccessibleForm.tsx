/**
 * Accessible Form Components
 * 
 * Provides accessible form field components with proper ARIA labels,
 * error announcements, and validation messages.
 */

import React, { useId, useCallback } from 'react';
import {
  Input,
  Textarea,
  Label,
  makeStyles,
  tokens,
  Text,
} from '@fluentui/react-components';
import { useAccessibility } from '../../contexts/AccessibilityContext';
import { ErrorCircle20Regular, Checkmark20Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  fieldContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginBottom: tokens.spacingVerticalM,
  },
  label: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
  },
  required: {
    color: tokens.colorPaletteRedForeground1,
    marginLeft: tokens.spacingHorizontalXXS,
  },
  hint: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  error: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteRedForeground1,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  success: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteGreenForeground1,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  errorInput: {
    borderColor: tokens.colorPaletteRedBorder1,
    ':focus': {
      borderColor: tokens.colorPaletteRedBorder1,
    },
  },
  successInput: {
    borderColor: tokens.colorPaletteGreenBorder1,
  },
});

interface AccessibleFieldProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  error?: string;
  hint?: string;
  required?: boolean;
  disabled?: boolean;
  type?: 'text' | 'email' | 'password' | 'url' | 'tel' | 'number';
  placeholder?: string;
  autoComplete?: string;
  showSuccess?: boolean;
  multiline?: boolean;
  rows?: number;
}

export function AccessibleField({
  label,
  name,
  value,
  onChange,
  onBlur,
  error,
  hint,
  required = false,
  disabled = false,
  type = 'text',
  placeholder,
  autoComplete,
  showSuccess = false,
  multiline = false,
  rows = 3,
}: AccessibleFieldProps) {
  const styles = useStyles();
  const { announce } = useAccessibility();
  const inputId = useId();
  const hintId = useId();
  const errorId = useId();
  const successId = useId();

  // Announce validation errors for screen readers
  const handleBlur = useCallback(() => {
    if (error && announce) {
      announce(`${label}: ${error}`, 'polite');
    } else if (showSuccess && !error && announce) {
      announce(`${label}: Valid`, 'polite');
    }
    onBlur?.();
  }, [error, showSuccess, label, announce, onBlur]);

  const describedBy = [
    hint ? hintId : null,
    error ? errorId : null,
    showSuccess && !error ? successId : null,
  ]
    .filter(Boolean)
    .join(' ');

  const commonProps = {
    id: inputId,
    name,
    value,
    onChange: (_: unknown, data: { value: string }) => onChange(data.value),
    onBlur: handleBlur,
    disabled,
    placeholder,
    autoComplete,
    required,
    'aria-invalid': !!error,
    'aria-describedby': describedBy || undefined,
    className: error ? styles.errorInput : showSuccess ? styles.successInput : undefined,
  };

  return (
    <div className={styles.fieldContainer}>
      <Label htmlFor={inputId} className={styles.label} required={required}>
        {label}
        {required && (
          <span className={styles.required} aria-label="required">
            *
          </span>
        )}
      </Label>

      {hint && (
        <Text id={hintId} className={styles.hint}>
          {hint}
        </Text>
      )}

      {multiline ? (
        <Textarea {...commonProps} rows={rows} />
      ) : (
        <Input {...commonProps} type={type} />
      )}

      {error && (
        <Text id={errorId} className={styles.error} role="alert" aria-live="polite">
          <ErrorCircle20Regular />
          {error}
        </Text>
      )}

      {showSuccess && !error && (
        <Text id={successId} className={styles.success} role="status">
          <Checkmark20Regular />
          Valid
        </Text>
      )}
    </div>
  );
}
