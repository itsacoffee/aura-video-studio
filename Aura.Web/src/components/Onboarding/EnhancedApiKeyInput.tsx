import { makeStyles, tokens, Input, Button, Text, Spinner } from '@fluentui/react-components';
import {
  Eye20Regular,
  EyeOff20Regular,
  Checkmark20Regular,
  Dismiss20Regular,
} from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import { FieldValidationErrors, type FieldValidationError } from './FieldValidationErrors';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  inputRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'flex-start',
  },
  inputWrapper: {
    flex: 1,
    position: 'relative',
  },
  input: {
    width: '100%',
  },
  toggleButton: {
    position: 'absolute',
    right: tokens.spacingHorizontalS,
    top: '50%',
    transform: 'translateY(-50%)',
    minWidth: 'auto',
    padding: tokens.spacingHorizontalXS,
  },
  statusIcon: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: '32px',
  },
  successText: {
    color: tokens.colorPaletteGreenForeground1,
  },
  accountInfo: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export interface EnhancedApiKeyInputProps {
  providerDisplayName: string;
  value: string;
  onChange: (value: string) => void;
  onValidate: () => void;
  validationStatus: 'idle' | 'validating' | 'valid' | 'invalid';
  fieldErrors?: FieldValidationError[];
  accountInfo?: string;
  disabled?: boolean;
  onSkipValidation?: () => void;
}

export function EnhancedApiKeyInput({
  providerDisplayName,
  value,
  onChange,
  onValidate,
  validationStatus,
  fieldErrors,
  accountInfo,
  disabled = false,
  onSkipValidation,
}: EnhancedApiKeyInputProps) {
  const styles = useStyles();
  const [showKey, setShowKey] = useState(false);

  const handleValidate = useCallback(() => {
    if (value.trim()) {
      onValidate();
    }
  }, [value, onValidate]);

  const renderStatusIcon = () => {
    switch (validationStatus) {
      case 'validating':
        return (
          <div className={styles.statusIcon}>
            <Spinner size="extra-small" />
          </div>
        );
      case 'valid':
        return (
          <div className={styles.statusIcon}>
            <Checkmark20Regular style={{ color: tokens.colorPaletteGreenForeground1 }} />
          </div>
        );
      case 'invalid':
        return (
          <div className={styles.statusIcon}>
            <Dismiss20Regular style={{ color: tokens.colorPaletteRedForeground1 }} />
          </div>
        );
      default:
        return null;
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.inputRow}>
        <div className={styles.inputWrapper}>
          <Input
            className={styles.input}
            type={showKey ? 'text' : 'password'}
            value={value}
            onChange={(_, data) => onChange(data.value)}
            placeholder={`Enter your ${providerDisplayName} API key`}
            disabled={disabled || validationStatus === 'validating'}
            contentAfter={
              <Button
                appearance="transparent"
                icon={showKey ? <EyeOff20Regular /> : <Eye20Regular />}
                onClick={() => setShowKey(!showKey)}
                className={styles.toggleButton}
                disabled={disabled}
              />
            }
          />
        </div>

        {renderStatusIcon()}

        <Button
          appearance="primary"
          onClick={handleValidate}
          disabled={disabled || !value.trim() || validationStatus === 'validating'}
        >
          {validationStatus === 'validating' ? 'Validating...' : 'Validate'}
        </Button>

        {onSkipValidation && (
          <Button
            appearance="secondary"
            onClick={onSkipValidation}
            disabled={disabled || validationStatus === 'validating'}
          >
            Skip
          </Button>
        )}
      </div>

      {validationStatus === 'valid' && accountInfo && (
        <div className={styles.accountInfo}>
          <Text className={styles.successText} weight="semibold">
            ✓ API key validated successfully
          </Text>
          <Text style={{ display: 'block', marginTop: tokens.spacingVerticalXXS }} size={200}>
            {accountInfo}
          </Text>
        </div>
      )}

      {fieldErrors && fieldErrors.length > 0 && <FieldValidationErrors errors={fieldErrors} />}
    </div>
  );
}
