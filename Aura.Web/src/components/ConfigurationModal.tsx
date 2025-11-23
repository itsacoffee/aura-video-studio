/**
 * Configuration Modal - A modal wrapper for the First Run Wizard
 *
 * This component provides a modal interface for configuration that can be
 * launched from anywhere in the app, particularly from the Welcome page.
 */

import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  Button,
} from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import { FirstRunWizard } from '../pages/Onboarding/FirstRunWizard';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '95vw',
    maxHeight: '95vh',
    width: '1200px',
    minHeight: '700px',
    height: '90vh',
    padding: 0,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
  },
  dialogBody: {
    padding: 0,
    overflow: 'hidden',
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minHeight: 0, // Critical for flexbox scrolling
  },
  dialogTitle: {
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexShrink: 0, // Prevent title from shrinking
  },
  wizardContainer: {
    flex: 1,
    overflow: 'auto',
    minHeight: 0, // Critical for flexbox scrolling
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalL}`,
  },
  closeButton: {
    marginLeft: 'auto',
  },
});

export interface ConfigurationModalProps {
  open: boolean;
  onClose: () => void;
  onComplete?: () => void;
  allowDismiss?: boolean;
  title?: string;
}

export function ConfigurationModal({
  open,
  onClose,
  onComplete,
  allowDismiss = true,
  title = 'System Configuration',
}: ConfigurationModalProps) {
  const styles = useStyles();
  const [isCompleting, setIsCompleting] = useState(false);

  const handleComplete = async () => {
    setIsCompleting(true);
    try {
      if (onComplete) {
        await onComplete();
      }
      onClose();
    } catch (error) {
      console.error('Error completing configuration:', error);
    } finally {
      setIsCompleting(false);
    }
  };

  const handleDismiss = () => {
    if (allowDismiss) {
      onClose();
    } else {
      const confirmed = window.confirm(
        'Configuration is not yet complete. Are you sure you want to close this? Video generation will not work until configuration is finished.'
      );
      if (confirmed) {
        onClose();
      }
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => data.open || handleDismiss()}>
      <DialogSurface className={styles.dialogSurface}>
        <div className={styles.dialogTitle}>
          <DialogTitle>{title}</DialogTitle>
          {allowDismiss && (
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={handleDismiss}
              className={styles.closeButton}
              aria-label="Close"
            />
          )}
        </div>
        <DialogBody className={styles.dialogBody}>
          <div className={styles.wizardContainer}>
            <FirstRunWizard onComplete={handleComplete} />
          </div>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
