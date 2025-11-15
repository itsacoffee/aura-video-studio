import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { ArrowClockwise24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import type { WizardStatusResponse } from '../../services/api/setupApi';

const useStyles = makeStyles({
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  infoText: {
    color: tokens.colorNeutralForeground3,
  },
  stepInfo: {
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  stepLabel: {
    fontWeight: tokens.fontWeightSemibold,
  },
});

export interface ResumeWizardDialogProps {
  open: boolean;
  wizardStatus: WizardStatusResponse | null;
  onResume: () => void;
  onStartFresh: () => void;
}

export function ResumeWizardDialog({
  open,
  wizardStatus,
  onResume,
  onStartFresh,
}: ResumeWizardDialogProps) {
  const styles = useStyles();

  if (!wizardStatus || !wizardStatus.canResume) {
    return null;
  }

  const stepNames = [
    'Welcome',
    'FFmpeg Check',
    'FFmpeg Installation',
    'Provider Configuration',
    'Workspace Setup',
    'Complete',
  ];

  const lastStep = stepNames[wizardStatus.currentStep] || `Step ${wizardStatus.currentStep}`;
  const lastUpdated = wizardStatus.lastUpdated
    ? new Date(wizardStatus.lastUpdated).toLocaleString()
    : 'Unknown';

  return (
    <Dialog open={open}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Resume Setup Wizard?</DialogTitle>
          <DialogContent className={styles.dialogContent}>
            <Text className={styles.infoText}>
              You have an incomplete setup wizard from a previous session.
            </Text>

            <div className={styles.stepInfo}>
              <Text className={styles.stepLabel}>Last Step Completed:</Text>
              <Text>{lastStep}</Text>
              <Text size={200} className={styles.infoText}>
                Last updated: {lastUpdated}
              </Text>
            </div>

            <Text>Would you like to resume where you left off, or start fresh?</Text>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={onStartFresh}>
              Start Fresh
            </Button>
            <Button appearance="primary" icon={<ArrowClockwise24Regular />} onClick={onResume}>
              Resume Setup
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
