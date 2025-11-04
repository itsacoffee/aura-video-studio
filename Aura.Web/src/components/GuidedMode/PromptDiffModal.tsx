import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogContent,
  DialogActions,
  Button,
  Text,
  Card,
  Badge,
} from '@fluentui/react-components';
import {
  CheckmarkCircle24Regular,
  DismissCircle24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import { useGuidedMode } from '../../state/guidedMode';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  section: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  promptBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    whiteSpace: 'pre-wrap',
    wordBreak: 'break-word',
  },
  changesList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  changeItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusSmall,
  },
  changeIcon: {
    marginTop: '2px',
  },
  changeContent: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  valueChange: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    marginTop: tokens.spacingVerticalXS,
    fontSize: tokens.fontSizeBase200,
  },
  oldValue: {
    color: tokens.colorPaletteRedForeground1,
    textDecoration: 'line-through',
  },
  newValue: {
    color: tokens.colorPaletteGreenForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  outcomeCard: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorBrandBackground2,
  },
});

export const PromptDiffModal: FC = () => {
  const styles = useStyles();
  const { promptDiffModal, confirmPromptDiff, cancelPromptDiff } = useGuidedMode();

  if (!promptDiffModal.visible || !promptDiffModal.promptDiff) {
    return null;
  }

  const { promptDiff } = promptDiffModal;

  return (
    <Dialog open={promptDiffModal.visible}>
      <DialogSurface>
        <DialogTitle>Review Prompt Changes</DialogTitle>
        <DialogBody>
          <DialogContent className={styles.content}>
            <div className={styles.section}>
              <Text weight="semibold">Intended Outcome</Text>
              <Card className={styles.outcomeCard}>
                <div
                  style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
                >
                  <Info24Regular />
                  <Text>{promptDiff.intendedOutcome}</Text>
                </div>
              </Card>
            </div>

            <div className={styles.section}>
              <Text weight="semibold">Changes to Prompt</Text>
              <div className={styles.changesList}>
                {promptDiff.changes.map((change, index) => (
                  <div key={index} className={styles.changeItem}>
                    <Badge appearance="tint" color="informative" className={styles.changeIcon}>
                      {change.type}
                    </Badge>
                    <div className={styles.changeContent}>
                      <Text weight="semibold">{change.description}</Text>
                      {(change.oldValue || change.newValue) && (
                        <div className={styles.valueChange}>
                          {change.oldValue && (
                            <Text className={styles.oldValue}>Previous: {change.oldValue}</Text>
                          )}
                          {change.newValue && (
                            <Text className={styles.newValue}>New: {change.newValue}</Text>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className={styles.section}>
              <Text weight="semibold">Original Prompt</Text>
              <div className={styles.promptBox}>{promptDiff.originalPrompt}</div>
            </div>

            <div className={styles.section}>
              <Text weight="semibold">Modified Prompt</Text>
              <div className={styles.promptBox}>{promptDiff.modifiedPrompt}</div>
            </div>
          </DialogContent>
        </DialogBody>
        <DialogActions>
          <Button
            appearance="secondary"
            icon={<DismissCircle24Regular />}
            onClick={cancelPromptDiff}
          >
            Cancel
          </Button>
          <Button
            appearance="primary"
            icon={<CheckmarkCircle24Regular />}
            onClick={confirmPromptDiff}
          >
            Proceed with Changes
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};
