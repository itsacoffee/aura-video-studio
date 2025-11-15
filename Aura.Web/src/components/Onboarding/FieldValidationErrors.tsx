import { makeStyles, tokens, Text, MessageBar, MessageBarBody } from '@fluentui/react-components';
import { Info20Regular, Warning20Regular, ErrorCircle20Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  errorItem: {
    marginBottom: tokens.spacingVerticalXS,
  },
  suggestedFix: {
    marginTop: tokens.spacingVerticalXXS,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  fieldName: {
    fontWeight: tokens.fontWeightSemibold,
  },
});

export interface FieldValidationError {
  fieldName: string;
  errorCode: string;
  errorMessage: string;
  suggestedFix?: string | null;
}

export interface FieldValidationErrorsProps {
  errors: FieldValidationError[];
}

export function FieldValidationErrors({ errors }: FieldValidationErrorsProps) {
  const styles = useStyles();

  if (!errors || errors.length === 0) {
    return null;
  }

  const getSeverity = (errorCode: string): 'error' | 'warning' | 'info' => {
    if (errorCode === 'REQUIRED' || errorCode === 'INVALID_FORMAT') {
      return 'error';
    }
    if (errorCode === 'INVALID_URL') {
      return 'warning';
    }
    return 'info';
  };

  const getIcon = (errorCode: string) => {
    const severity = getSeverity(errorCode);
    switch (severity) {
      case 'error':
        return <ErrorCircle20Regular />;
      case 'warning':
        return <Warning20Regular />;
      case 'info':
        return <Info20Regular />;
    }
  };

  return (
    <div className={styles.container}>
      {errors.map((error, index) => (
        <MessageBar
          key={`${error.fieldName}-${index}`}
          intent={getSeverity(error.errorCode)}
          icon={getIcon(error.errorCode)}
          className={styles.errorItem}
        >
          <MessageBarBody>
            <Text>
              <span className={styles.fieldName}>{error.fieldName}:</span> {error.errorMessage}
            </Text>
            {error.suggestedFix && (
              <Text className={styles.suggestedFix}>💡 {error.suggestedFix}</Text>
            )}
          </MessageBarBody>
        </MessageBar>
      ))}
    </div>
  );
}

export interface FieldValidationStatusProps {
  fieldStatuses: Record<string, boolean>;
}

export function FieldValidationStatus({ fieldStatuses }: FieldValidationStatusProps) {
  const styles = useStyles();

  if (!fieldStatuses || Object.keys(fieldStatuses).length === 0) {
    return null;
  }

  return (
    <div className={styles.container}>
      {Object.entries(fieldStatuses).map(([fieldName, isValid]) => (
        <div
          key={fieldName}
          style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalXS }}
        >
          {isValid ? (
            <span style={{ color: tokens.colorPaletteGreenForeground1 }}>✓</span>
          ) : (
            <span style={{ color: tokens.colorPaletteRedForeground1 }}>✗</span>
          )}
          <Text size={200} className={styles.fieldName}>
            {fieldName}
          </Text>
        </div>
      ))}
    </div>
  );
}
