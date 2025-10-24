import { useState } from 'react';
import {
  makeStyles,
  tokens,
  Input,
  Button,
  Text,
  Spinner,
} from '@fluentui/react-components';
import {
  Eye20Regular,
  EyeOff20Regular,
  Checkmark20Regular,
  Dismiss20Regular,
} from '@fluentui/react-icons';

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
  errorText: {
    color: tokens.colorPaletteRedForeground1,
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

export interface ApiKeyInputProps {
  provider: string;
  value: string;
  onChange: (value: string) => void;
  onValidate: () => void;
  validationStatus: 'idle' | 'validating' | 'valid' | 'invalid';
  error?: string;
  accountInfo?: string;
  disabled?: boolean;
}

export function ApiKeyInput({
  provider,
  value,
  onChange,
  onValidate,
  validationStatus,
  error,
  accountInfo,
  disabled = false,
}: ApiKeyInputProps) {
  const styles = useStyles();
  const [showKey, setShowKey] = useState(false);

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
            onChange={(e) => onChange(e.target.value)}
            placeholder={`Enter your ${provider} API key`}
            disabled={disabled || validationStatus === 'validating'}
            contentAfter={
              <Button
                appearance="transparent"
                className={styles.toggleButton}
                icon={showKey ? <EyeOff20Regular /> : <Eye20Regular />}
                onClick={() => setShowKey(!showKey)}
                aria-label={showKey ? 'Hide API key' : 'Show API key'}
              />
            }
          />
        </div>
        {renderStatusIcon()}
        <Button
          appearance="secondary"
          onClick={onValidate}
          disabled={
            !value || disabled || validationStatus === 'validating' || validationStatus === 'valid'
          }
        >
          {validationStatus === 'validating' ? 'Validating...' : 'Validate'}
        </Button>
      </div>

      {validationStatus === 'valid' && (
        <Text className={styles.successText}>✓ Valid! API key verified successfully.</Text>
      )}

      {validationStatus === 'valid' && accountInfo && (
        <div className={styles.accountInfo}>
          <Text size={200}>{accountInfo}</Text>
        </div>
      )}

      {validationStatus === 'invalid' && error && (
        <Text className={styles.errorText} size={200}>
          {error}
        </Text>
      )}
    </div>
  );
}
