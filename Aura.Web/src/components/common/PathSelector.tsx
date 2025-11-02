import {
  makeStyles,
  tokens,
  Input,
  Button,
  Spinner,
  Text,
  Tooltip,
} from '@fluentui/react-components';
import {
  Folder24Regular,
  Checkmark24Filled,
  Dismiss24Filled,
  Info24Regular,
  ArrowClockwise24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';

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
  input: {
    flex: 1,
  },
  validationRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  validIcon: {
    color: tokens.colorPaletteGreenForeground1,
  },
  invalidIcon: {
    color: tokens.colorPaletteRedForeground1,
  },
  helpText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  defaultPath: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontFamily: tokens.fontFamilyMonospace,
  },
  versionText: {
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase200,
  },
});

export interface PathSelectorProps {
  label: string;
  placeholder?: string;
  value: string;
  onChange: (value: string) => void;
  onValidate?: (path: string) => Promise<{ isValid: boolean; message: string; version?: string }>;
  helpText?: string;
  defaultPath?: string;
  fileFilter?: string;
  dependencyId?: string;
  disabled?: boolean;
  autoDetect?: () => Promise<string | null>;
}

export const PathSelector: FC<PathSelectorProps> = ({
  label,
  placeholder = 'Click Browse to select file',
  value,
  onChange,
  onValidate,
  helpText,
  defaultPath,
  disabled = false,
  autoDetect,
}) => {
  const styles = useStyles();
  const [isValidating, setIsValidating] = useState(false);
  const [isAutoDetecting, setIsAutoDetecting] = useState(false);
  const [validationResult, setValidationResult] = useState<{
    isValid: boolean;
    message: string;
    version?: string;
  } | null>(null);

  const handleValidate = useCallback(
    async (pathToValidate: string) => {
      if (!pathToValidate.trim() || !onValidate) {
        setValidationResult(null);
        return;
      }

      setIsValidating(true);
      try {
        const result = await onValidate(pathToValidate);
        setValidationResult(result);
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : 'Validation failed';
        setValidationResult({
          isValid: false,
          message: errorMessage,
        });
      } finally {
        setIsValidating(false);
      }
    },
    [onValidate]
  );

  useEffect(() => {
    if (value && onValidate) {
      const timeoutId = setTimeout(() => {
        handleValidate(value);
      }, 500);

      return () => clearTimeout(timeoutId);
    } else {
      setValidationResult(null);
    }
  }, [value, onValidate, handleValidate]);

  const handleBrowse = useCallback(() => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.exe';

    input.onchange = (e) => {
      const target = e.target as HTMLInputElement;
      const file = target.files?.[0];
      if (file) {
        const path = (file as unknown as { path?: string }).path;
        if (path) {
          onChange(path);
        } else if (file.name) {
          onChange(file.name);
        }
      }
    };

    input.click();
  }, [onChange]);

  const handleAutoDetect = useCallback(async () => {
    if (!autoDetect) return;

    setIsAutoDetecting(true);
    try {
      const detectedPath = await autoDetect();
      if (detectedPath) {
        onChange(detectedPath);
      }
    } catch (error: unknown) {
      console.error('Auto-detect failed:', error);
    } finally {
      setIsAutoDetecting(false);
    }
  }, [autoDetect, onChange]);

  return (
    <div className={styles.container}>
      <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
        <Text weight="semibold">{label}</Text>
        {helpText && (
          <Tooltip content={helpText} relationship="description">
            <Info24Regular style={{ cursor: 'help', color: tokens.colorNeutralForeground3 }} />
          </Tooltip>
        )}
      </div>

      {defaultPath && <Text className={styles.defaultPath}>Default: {defaultPath}</Text>}

      <div className={styles.inputRow}>
        <Input
          className={styles.input}
          placeholder={placeholder}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled || isValidating}
          contentBefore={<Folder24Regular />}
        />
        <Button
          appearance="secondary"
          onClick={handleBrowse}
          disabled={disabled || isValidating}
          icon={<Folder24Regular />}
        >
          Browse...
        </Button>
        {autoDetect && (
          <Button
            appearance="secondary"
            onClick={handleAutoDetect}
            disabled={disabled || isAutoDetecting}
            icon={isAutoDetecting ? <Spinner size="tiny" /> : <ArrowClockwise24Regular />}
          >
            {isAutoDetecting ? 'Detecting...' : 'Auto-Detect'}
          </Button>
        )}
      </div>

      {isValidating && (
        <div className={styles.validationRow}>
          <Spinner size="tiny" />
          <Text size={200}>Validating path...</Text>
        </div>
      )}

      {!isValidating && validationResult && (
        <div className={styles.validationRow}>
          {validationResult.isValid ? (
            <Checkmark24Filled className={styles.validIcon} />
          ) : (
            <Dismiss24Filled className={styles.invalidIcon} />
          )}
          <Text
            size={200}
            style={{
              color: validationResult.isValid
                ? tokens.colorPaletteGreenForeground1
                : tokens.colorPaletteRedForeground1,
            }}
          >
            {validationResult.message}
          </Text>
          {validationResult.isValid && validationResult.version && (
            <Text className={styles.versionText}>({validationResult.version})</Text>
          )}
        </div>
      )}
    </div>
  );
};
