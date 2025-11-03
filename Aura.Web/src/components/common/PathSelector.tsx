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
  FolderOpen24Regular,
  Document24Regular,
  Checkmark24Filled,
  Dismiss24Filled,
  Info24Regular,
  ArrowClockwise24Regular,
  DeleteDismiss24Regular,
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
    flexWrap: 'wrap',
  },
  input: {
    flex: 1,
    minWidth: '250px',
  },
  buttonGroup: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
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
  exampleText: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    fontStyle: 'italic',
  },
});

export type PathType = 'file' | 'directory';

export interface PathSelectorProps {
  label: string;
  placeholder?: string;
  value: string;
  onChange: (value: string) => void;
  onValidate?: (path: string) => Promise<{ isValid: boolean; message: string; version?: string }>;
  helpText?: string;
  defaultPath?: string;
  examplePath?: string;
  type?: PathType;
  fileTypes?: string;
  dependencyId?: string;
  disabled?: boolean;
  autoDetect?: () => Promise<string | null>;
  showOpenFolder?: boolean;
  showClearButton?: boolean;
}

export const PathSelector: FC<PathSelectorProps> = ({
  label,
  placeholder,
  value,
  onChange,
  onValidate,
  helpText,
  defaultPath,
  examplePath,
  type = 'file',
  fileTypes,
  disabled = false,
  autoDetect,
  showOpenFolder = true,
  showClearButton = true,
}) => {
  const styles = useStyles();
  const [isValidating, setIsValidating] = useState(false);
  const [isAutoDetecting, setIsAutoDetecting] = useState(false);
  const [validationResult, setValidationResult] = useState<{
    isValid: boolean;
    message: string;
    version?: string;
  } | null>(null);

  const effectivePlaceholder =
    placeholder ||
    (type === 'directory' ? 'Click Browse to select folder' : 'Click Browse to select file');

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

    if (type === 'directory') {
      input.setAttribute('webkitdirectory', '');
      input.setAttribute('directory', '');
    }

    if (fileTypes) {
      input.accept = fileTypes;
    } else if (type === 'file') {
      input.accept = '.exe,.bat,.sh,.cmd';
    }

    input.onchange = (e) => {
      const target = e.target as HTMLInputElement;
      const file = target.files?.[0];
      if (file) {
        const path = (file as unknown as { path?: string }).path;
        if (path) {
          if (type === 'directory') {
            const lastSeparator = Math.max(path.lastIndexOf('/'), path.lastIndexOf('\\'));
            const dirPath = lastSeparator > 0 ? path.substring(0, lastSeparator) : path;
            onChange(dirPath);
          } else {
            onChange(path);
          }
        } else if (file.name) {
          onChange(file.name);
        }
      }
    };

    input.click();
  }, [onChange, type, fileTypes]);

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

  const handleClear = useCallback(() => {
    onChange('');
    setValidationResult(null);
  }, [onChange]);

  const handleOpenFolder = useCallback(async () => {
    if (!value) return;

    try {
      const response = await fetch('/api/system/open-folder', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ path: value }),
      });

      if (!response.ok) {
        console.warn(
          'Failed to open folder:',
          response.status,
          response.statusText,
          'path:',
          value
        );
      }
    } catch (error: unknown) {
      console.error('Failed to open folder:', error);
    }
  }, [value]);

  return (
    <div className={styles.container}>
      <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
        <Text weight="semibold">{label}</Text>
        {type === 'directory' ? (
          <Folder24Regular style={{ color: tokens.colorNeutralForeground3 }} />
        ) : (
          <Document24Regular style={{ color: tokens.colorNeutralForeground3 }} />
        )}
        {helpText && (
          <Tooltip content={helpText} relationship="description">
            <Info24Regular style={{ cursor: 'help', color: tokens.colorNeutralForeground3 }} />
          </Tooltip>
        )}
      </div>

      {defaultPath && <Text className={styles.defaultPath}>Default: {defaultPath}</Text>}
      {examplePath && <Text className={styles.exampleText}>e.g., {examplePath}</Text>}

      <div className={styles.inputRow}>
        <Input
          className={styles.input}
          placeholder={effectivePlaceholder}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          disabled={disabled || isValidating}
          contentBefore={type === 'directory' ? <Folder24Regular /> : <Document24Regular />}
        />
        <div className={styles.buttonGroup}>
          <Button
            appearance="secondary"
            onClick={handleBrowse}
            disabled={disabled || isValidating}
            icon={type === 'directory' ? <Folder24Regular /> : <Document24Regular />}
            title={type === 'directory' ? 'Browse for folder' : 'Browse for file'}
          >
            Browse...
          </Button>
          {autoDetect && (
            <Button
              appearance="secondary"
              onClick={handleAutoDetect}
              disabled={disabled || isAutoDetecting}
              icon={isAutoDetecting ? <Spinner size="tiny" /> : <ArrowClockwise24Regular />}
              title="Automatically detect path"
            >
              {isAutoDetecting ? 'Detecting...' : 'Auto-Detect'}
            </Button>
          )}
          {showOpenFolder && value && (
            <Button
              appearance="subtle"
              onClick={handleOpenFolder}
              disabled={disabled}
              icon={<FolderOpen24Regular />}
              title="Open in file explorer"
            >
              Open
            </Button>
          )}
          {showClearButton && value && (
            <Button
              appearance="subtle"
              onClick={handleClear}
              disabled={disabled}
              icon={<DeleteDismiss24Regular />}
              title="Clear selection"
            >
              Clear
            </Button>
          )}
        </div>
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
