import { makeStyles, tokens, Text, Card, Badge, Button } from '@fluentui/react-components';
import { ArrowSyncCheckmark24Regular, Lightbulb24Regular } from '@fluentui/react-icons';
import React from 'react';
import type { FC } from 'react';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalS,
  },
  diffContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  textBox: {
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: '14px',
    lineHeight: '1.6',
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  originalText: {
    backgroundColor: tokens.colorPaletteRedBackground2,
    border: `2px solid ${tokens.colorPaletteRedBorder1}`,
  },
  modifiedText: {
    backgroundColor: tokens.colorPaletteGreenBackground2,
    border: `2px solid ${tokens.colorPaletteGreenBorder1}`,
  },
  label: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginBottom: tokens.spacingVerticalXS,
  },
  highlightRemoved: {
    backgroundColor: tokens.colorPaletteRedBackground3,
    textDecoration: 'line-through',
    padding: '2px 4px',
    borderRadius: tokens.borderRadiusSmall,
  },
  highlightAdded: {
    backgroundColor: tokens.colorPaletteGreenBackground3,
    fontWeight: 600,
    padding: '2px 4px',
    borderRadius: tokens.borderRadiusSmall,
  },
  inlineContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  inlineDiff: {
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground2,
    fontFamily: 'monospace',
    fontSize: '14px',
    lineHeight: '1.8',
  },
  explanation: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorBrandForeground1}`,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    justifyContent: 'flex-end',
    marginTop: tokens.spacingVerticalM,
  },
});

export interface PromptDiffViewerProps {
  originalPrompt: string;
  modifiedPrompt?: string;
  explanation?: string;
  showInlineDiff?: boolean;
  onAcceptModified?: () => void;
  onReject?: () => void;
}

export const PromptDiffViewer: FC<PromptDiffViewerProps> = ({
  originalPrompt,
  modifiedPrompt,
  explanation,
  showInlineDiff = true,
  onAcceptModified,
  onReject,
}) => {
  const styles = useStyles();

  const generateInlineDiff = (original: string, modified: string): JSX.Element[] => {
    const originalWords = original.split(/(\s+)/);
    const modifiedWords = modified.split(/(\s+)/);

    const diff: JSX.Element[] = [];
    let origIdx = 0;
    let modIdx = 0;

    while (origIdx < originalWords.length || modIdx < modifiedWords.length) {
      if (origIdx < originalWords.length && modIdx < modifiedWords.length) {
        if (originalWords[origIdx] === modifiedWords[modIdx]) {
          diff.push(<span key={`same-${origIdx}`}>{originalWords[origIdx]}</span>);
          origIdx++;
          modIdx++;
        } else {
          const origChunk = originalWords.slice(
            origIdx,
            Math.min(origIdx + 3, originalWords.length)
          );
          const modChunk = modifiedWords.slice(modIdx, Math.min(modIdx + 3, modifiedWords.length));

          const origText = origChunk.join('');
          const modText = modChunk.join('');

          diff.push(
            <span key={`removed-${origIdx}`} className={styles.highlightRemoved}>
              {origText}
            </span>
          );
          diff.push(
            <span key={`added-${modIdx}`} className={styles.highlightAdded}>
              {modText}
            </span>
          );

          origIdx += origChunk.length;
          modIdx += modChunk.length;
        }
      } else if (origIdx < originalWords.length) {
        diff.push(
          <span key={`removed-end-${origIdx}`} className={styles.highlightRemoved}>
            {originalWords[origIdx]}
          </span>
        );
        origIdx++;
      } else if (modIdx < modifiedWords.length) {
        diff.push(
          <span key={`added-end-${modIdx}`} className={styles.highlightAdded}>
            {modifiedWords[modIdx]}
          </span>
        );
        modIdx++;
      }
    }

    return diff;
  };

  if (!modifiedPrompt) {
    return (
      <Card style={{ padding: tokens.spacingVerticalM }}>
        <Text>No modifications suggested</Text>
      </Card>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <ArrowSyncCheckmark24Regular />
        <Text weight="semibold" size={400}>
          Suggested Content Modification
        </Text>
        <Badge appearance="filled" color="warning">
          Safety Fix
        </Badge>
      </div>

      {explanation && (
        <div className={styles.explanation}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: tokens.spacingHorizontalS,
              marginBottom: tokens.spacingVerticalXS,
            }}
          >
            <Lightbulb24Regular style={{ color: tokens.colorBrandForeground1 }} />
            <Text weight="semibold">Why this change?</Text>
          </div>
          <Text size={200}>{explanation}</Text>
        </div>
      )}

      {showInlineDiff ? (
        <div className={styles.inlineContainer}>
          <div className={styles.label}>
            <Text weight="semibold">Changes (inline view):</Text>
          </div>
          <div className={styles.inlineDiff}>
            {generateInlineDiff(originalPrompt, modifiedPrompt)}
          </div>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            <span className={styles.highlightRemoved}>Red (strikethrough)</span> = Removed text |{' '}
            <span className={styles.highlightAdded}>Green (bold)</span> = Added text
          </Text>
        </div>
      ) : (
        <div className={styles.diffContainer}>
          <div>
            <div className={styles.label}>
              <Badge appearance="filled" color="danger">
                Original
              </Badge>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                (flagged by safety check)
              </Text>
            </div>
            <div className={`${styles.textBox} ${styles.originalText}`}>{originalPrompt}</div>
          </div>

          <div>
            <div className={styles.label}>
              <Badge appearance="filled" color="success">
                Modified
              </Badge>
              <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                (safe alternative)
              </Text>
            </div>
            <div className={`${styles.textBox} ${styles.modifiedText}`}>{modifiedPrompt}</div>
          </div>
        </div>
      )}

      {(onAcceptModified || onReject) && (
        <div className={styles.actions}>
          {onReject && (
            <Button appearance="secondary" onClick={onReject}>
              Reject Modification
            </Button>
          )}
          {onAcceptModified && (
            <Button appearance="primary" onClick={onAcceptModified}>
              Accept Modification
            </Button>
          )}
        </div>
      )}
    </div>
  );
};
