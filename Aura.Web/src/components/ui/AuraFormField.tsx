/**
 * AuraFormField - Consistent form field layout component
 *
 * Provides a standardized layout for form controls with:
 * - Label above the control (sentence case, medium weight)
 * - Helper text below the control
 * - Error messages in consistent style
 * - Consistent spacing
 *
 * @example
 * ```tsx
 * <AuraFormField
 *   label="Email address"
 *   required
 *   error={errors.email?.message}
 *   helperText="We'll never share your email"
 * >
 *   <Input value={email} onChange={handleChange} />
 * </AuraFormField>
 * ```
 */

import { Field, Label, makeStyles, mergeClasses, tokens } from '@fluentui/react-components';
import { ErrorCircle12Regular, Info12Regular } from '@fluentui/react-icons';
import { cloneElement, useId, type ReactElement } from 'react';

export interface AuraFormFieldProps {
  /** Label text for the field */
  label?: string;
  /** Whether the field is required */
  required?: boolean;
  /** Error message to display */
  error?: string;
  /** Helper text to display below the control */
  helperText?: string;
  /** Success message to display when valid */
  successMessage?: string;
  /** The form control element */
  children: ReactElement;
  /** Additional class name */
  className?: string;
  /** Layout orientation - vertical (default) or horizontal */
  orientation?: 'vertical' | 'horizontal';
  /** Label width when using horizontal layout */
  labelWidth?: string;
  /** Whether to hide the label visually (still accessible) */
  hideLabel?: boolean;
}

const useStyles = makeStyles({
  field: {
    marginBottom: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },

  fieldHorizontal: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
  },

  labelContainer: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    marginBottom: tokens.spacingVerticalXS,
  },

  labelContainerHorizontal: {
    marginBottom: 0,
    paddingTop: tokens.spacingVerticalS,
    flexShrink: 0,
  },

  label: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground1,
  },

  required: {
    color: tokens.colorPaletteRedForeground1,
    marginLeft: tokens.spacingHorizontalXXS,
  },

  controlContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    flex: 1,
  },

  message: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalXS,
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
    marginTop: tokens.spacingVerticalXXS,
    // Reserve space to prevent layout shift
    minHeight: tokens.lineHeightBase200,
  },

  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
  },

  helperMessage: {
    color: tokens.colorNeutralForeground3,
  },

  successMessage: {
    color: tokens.colorPaletteGreenForeground1,
  },

  messageIcon: {
    flexShrink: 0,
    marginTop: '2px',
  },

  // Visually hidden but accessible
  visuallyHidden: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    padding: '0',
    margin: '-1px',
    overflow: 'hidden',
    clip: 'rect(0, 0, 0, 0)',
    whiteSpace: 'nowrap',
    border: '0',
  },
});

/**
 * AuraFormField - Standardized form field layout component
 *
 * Features:
 * - Consistent label styling (sentence case, semibold)
 * - Required field indicator
 * - Error, helper, and success message support
 * - No layout shift when messages appear
 * - Vertical and horizontal orientations
 * - Accessibility support
 */
export function AuraFormField({
  label,
  required = false,
  error,
  helperText,
  successMessage,
  children,
  className,
  orientation = 'vertical',
  labelWidth = '120px',
  hideLabel = false,
}: AuraFormFieldProps) {
  const styles = useStyles();
  const inputId = useId();

  const isHorizontal = orientation === 'horizontal';

  // Determine which message to show (error takes precedence)
  const showError = !!error;
  const showSuccess = !showError && !!successMessage;
  const showHelper = !showError && !showSuccess && !!helperText;

  // Clone the child element and add the id for accessibility
  const childWithId = cloneElement(children, {
    id: inputId,
    'aria-invalid': showError ? 'true' : undefined,
    'aria-describedby': error || helperText || successMessage ? `${inputId}-message` : undefined,
  } as Record<string, unknown>);

  const fieldClassName = mergeClasses(
    styles.field,
    isHorizontal && styles.fieldHorizontal,
    className
  );

  const labelContainerClassName = mergeClasses(
    styles.labelContainer,
    isHorizontal && styles.labelContainerHorizontal,
    hideLabel && styles.visuallyHidden
  );

  return (
    <Field className={fieldClassName}>
      {label && (
        <div
          className={labelContainerClassName}
          style={isHorizontal ? { width: labelWidth } : undefined}
        >
          <Label className={styles.label} htmlFor={inputId}>
            {label}
          </Label>
          {required && (
            <span className={styles.required} aria-hidden="true">
              *
            </span>
          )}
        </div>
      )}

      <div className={styles.controlContainer}>
        {childWithId}

        {/* Message area - always present to prevent layout shift */}
        <div
          id={`${inputId}-message`}
          className={mergeClasses(
            styles.message,
            showError && styles.errorMessage,
            showHelper && styles.helperMessage,
            showSuccess && styles.successMessage
          )}
          role={showError ? 'alert' : undefined}
          aria-live={showError ? 'polite' : undefined}
        >
          {showError && (
            <>
              <ErrorCircle12Regular className={styles.messageIcon} aria-hidden="true" />
              <span>{error}</span>
            </>
          )}
          {showHelper && (
            <>
              <Info12Regular className={styles.messageIcon} aria-hidden="true" />
              <span>{helperText}</span>
            </>
          )}
          {showSuccess && <span>{successMessage}</span>}
        </div>
      </div>
    </Field>
  );
}

export default AuraFormField;
