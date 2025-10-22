/**
 * FormField component with label, error display, and validation
 */

import { ReactNode } from 'react';
import { Field, Label, makeStyles, tokens } from '@fluentui/react-components';
import { FieldError } from 'react-hook-form';

interface FormFieldProps {
  label?: string;
  error?: FieldError | string;
  required?: boolean;
  helpText?: string;
  children: ReactNode;
}

const useStyles = makeStyles({
  field: {
    marginBottom: '1rem',
  },
  label: {
    marginBottom: '0.25rem',
    fontWeight: 600,
  },
  required: {
    color: tokens.colorPaletteRedForeground1,
    marginLeft: '0.25rem',
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: '0.875rem',
    marginTop: '0.25rem',
  },
  helpText: {
    color: tokens.colorNeutralForeground2,
    fontSize: '0.875rem',
    marginTop: '0.25rem',
  },
});

export function FormField({ label, error, required, helpText, children }: FormFieldProps) {
  const styles = useStyles();
  const errorMessage = typeof error === 'string' ? error : error?.message;

  return (
    <Field className={styles.field}>
      {label && (
        <Label className={styles.label}>
          {label}
          {required && <span className={styles.required}>*</span>}
        </Label>
      )}
      {children}
      {errorMessage && <div className={styles.error}>{errorMessage}</div>}
      {!errorMessage && helpText && <div className={styles.helpText}>{helpText}</div>}
    </Field>
  );
}
