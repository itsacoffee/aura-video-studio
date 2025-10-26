/**
 * ValidatedInput component with real-time validation feedback
 */

import { Input, InputProps, Field, Spinner, makeStyles, tokens } from '@fluentui/react-components';
import { Checkmark20Regular, ErrorCircle20Regular, Info20Regular } from '@fluentui/react-icons';

export interface ValidatedInputProps extends Omit<InputProps, 'onChange' | 'contentAfter'> {
  label?: string;
  error?: string;
  isValid?: boolean;
  isValidating?: boolean;
  required?: boolean;
  hint?: string;
  successMessage?: string;
  onChange?: (value: string) => void;
}

const useStyles = makeStyles({
  field: {
    marginBottom: tokens.spacingVerticalM,
  },
  inputWrapper: {
    position: 'relative',
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  input: {
    flex: 1,
  },
  validationIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: '24px',
    height: '24px',
  },
  validIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  invalidIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  errorMessage: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalXS,
  },
  hintMessage: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalXS,
  },
  successMessage: {
    color: tokens.colorPaletteGreenForeground1,
    fontSize: tokens.fontSizeBase200,
    marginTop: tokens.spacingVerticalXS,
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalXS,
  },
  required: {
    color: tokens.colorPaletteRedForeground1,
    marginLeft: tokens.spacingHorizontalXXS,
  },
});

/**
 * Input component with built-in validation state display
 * Shows checkmark when valid, error icon when invalid, and spinner when validating
 *
 * @example
 * ```tsx
 * <ValidatedInput
 *   label="Email"
 *   value={email}
 *   onChange={setEmail}
 *   error={errors.email}
 *   isValid={!errors.email && email.length > 0}
 *   required
 *   hint="Enter your email address"
 * />
 * ```
 */
export function ValidatedInput({
  label,
  error,
  isValid,
  isValidating,
  required,
  hint,
  successMessage,
  onChange,
  ...inputProps
}: ValidatedInputProps) {
  const styles = useStyles();

  const showValidIcon = isValid && !isValidating && !error && inputProps.value;
  const showInvalidIcon = error && !isValidating;
  const showHint = !error && !successMessage && hint;
  const showSuccess = isValid && !error && successMessage && inputProps.value;

  const validationState = error ? 'error' : isValid && inputProps.value ? 'success' : undefined;

  return (
    <Field
      className={styles.field}
      label={
        label ? (
          <>
            {label}
            {required && <span className={styles.required}>*</span>}
          </>
        ) : undefined
      }
      validationMessage={
        error ? (
          <div className={styles.errorMessage}>
            <ErrorCircle20Regular />
            <span>{error}</span>
          </div>
        ) : showSuccess ? (
          <div className={styles.successMessage}>
            <Checkmark20Regular />
            <span>{successMessage}</span>
          </div>
        ) : showHint ? (
          <div className={styles.hintMessage}>
            <Info20Regular />
            <span>{hint}</span>
          </div>
        ) : undefined
      }
      validationState={validationState}
    >
      <div className={styles.inputWrapper}>
        <Input
          {...inputProps}
          className={styles.input}
          onChange={(_, data) => onChange?.(data.value)}
        />
        <div className={styles.validationIcon}>
          {isValidating && <Spinner size="tiny" />}
          {showValidIcon && <Checkmark20Regular className={styles.validIcon} />}
          {showInvalidIcon && <ErrorCircle20Regular className={styles.invalidIcon} />}
        </div>
      </div>
    </Field>
  );
}
