import { Switch, Text, tokens, makeStyles } from '@fluentui/react-components';
import { DocumentSearch24Regular } from '@fluentui/react-icons';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
  },
  toggleRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  icon: {
    color: tokens.colorBrandForeground1,
  },
  helpText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    marginLeft: tokens.spacingHorizontalXXL,
  },
  warningText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteMarigoldForeground1,
    marginLeft: tokens.spacingHorizontalXXL,
  },
  successText: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorPaletteGreenForeground1,
    marginLeft: tokens.spacingHorizontalXXL,
  },
});

interface RagToggleProps {
  enabled: boolean;
  onChange: (enabled: boolean) => void;
  documentCount: number;
}

export const RagToggle: FC<RagToggleProps> = ({ enabled, onChange, documentCount }) => {
  const styles = useStyles();

  return (
    <div className={styles.container}>
      <div className={styles.toggleRow}>
        <DocumentSearch24Regular className={styles.icon} />
        <Switch
          checked={enabled}
          onChange={(_, data) => onChange(data.checked)}
          disabled={documentCount === 0}
          label={`Use Knowledge Base (${documentCount} documents)`}
        />
      </div>

      {documentCount === 0 && (
        <Text className={styles.warningText}>
          📚 No documents indexed. Upload documents to enable RAG.
        </Text>
      )}

      {enabled && documentCount > 0 && (
        <Text className={styles.successText}>
          ✅ Script will be enhanced with relevant context from your knowledge base.
        </Text>
      )}
    </div>
  );
};
