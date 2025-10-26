import {
  makeStyles,
  tokens,
  Text,
  Button,
} from '@fluentui/react-components';
import { Copy24Regular } from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderTop: `1px solid ${tokens.colorNeutralStroke1}`,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    marginBottom: tokens.spacingVerticalS,
  },
  row: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  label: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  value: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    wordBreak: 'break-all',
  },
  pathRow: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
  },
  pathValue: {
    flex: 1,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    wordBreak: 'break-all',
  },
  copyButton: {
    minWidth: 'auto',
  },
});

interface MetadataPanelProps {
  filePath?: string;
  fileSize?: number;
  resolution?: string;
  duration?: number;
  frameRate?: number;
  codec?: string;
  creationDate?: string;
}

const formatFileSize = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(2)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
};

export const MetadataPanel: React.FC<MetadataPanelProps> = ({
  filePath,
  fileSize,
  resolution,
  duration,
  frameRate,
  codec,
  creationDate,
}) => {
  const styles = useStyles();

  const handleCopyPath = async () => {
    if (filePath) {
      try {
        await navigator.clipboard.writeText(filePath);
        // Could be enhanced with a toast notification in the future
      } catch (error) {
        console.error('Failed to copy to clipboard:', error);
        // Fallback: could show an error message to user
      }
    }
  };

  if (!filePath && !fileSize && !resolution && !duration) {
    return null;
  }

  return (
    <div className={styles.container}>
      <Text className={styles.title}>File Information</Text>
      
      {filePath && (
        <div className={styles.pathRow}>
          <div style={{ flex: 1 }}>
            <Text className={styles.label}>File Path</Text>
            <Text className={styles.pathValue}>{filePath}</Text>
          </div>
          <Button
            className={styles.copyButton}
            appearance="subtle"
            size="small"
            icon={<Copy24Regular />}
            onClick={handleCopyPath}
            aria-label="Copy file path"
          />
        </div>
      )}

      {fileSize !== undefined && (
        <div className={styles.row}>
          <Text className={styles.label}>Size</Text>
          <Text className={styles.value}>{formatFileSize(fileSize)}</Text>
        </div>
      )}

      {resolution && (
        <div className={styles.row}>
          <Text className={styles.label}>Resolution</Text>
          <Text className={styles.value}>{resolution}</Text>
        </div>
      )}

      {duration !== undefined && (
        <div className={styles.row}>
          <Text className={styles.label}>Duration</Text>
          <Text className={styles.value}>{duration.toFixed(2)}s</Text>
        </div>
      )}

      {frameRate && (
        <div className={styles.row}>
          <Text className={styles.label}>Frame Rate</Text>
          <Text className={styles.value}>{frameRate} fps</Text>
        </div>
      )}

      {codec && (
        <div className={styles.row}>
          <Text className={styles.label}>Codec</Text>
          <Text className={styles.value}>{codec}</Text>
        </div>
      )}

      {creationDate && (
        <div className={styles.row}>
          <Text className={styles.label}>Created</Text>
          <Text className={styles.value}>{new Date(creationDate).toLocaleString()}</Text>
        </div>
      )}
    </div>
  );
};
