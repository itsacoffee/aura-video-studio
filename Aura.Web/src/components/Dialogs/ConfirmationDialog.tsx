/**
 * Confirmation Dialog Component
 * Shows confirmation for destructive operations with cancel as default
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Text,
  Caption1,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Warning24Regular, Delete24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { loggingService } from '../../services/loggingService';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  warningSection: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteRedBackground2,
    borderRadius: tokens.borderRadiusMedium,
    borderLeft: `4px solid ${tokens.colorPaletteRedBorder2}`,
  },
  warningIcon: {
    color: tokens.colorPaletteRedForeground1,
    flexShrink: 0,
  },
  warningText: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  message: {
    color: tokens.colorNeutralForeground1,
  },
  caption: {
    color: tokens.colorNeutralForeground2,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

export interface ConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'destructive' | 'warning' | 'info';
  onConfirm: () => void;
  onCancel?: () => void;
}

export function ConfirmationDialog({
  open,
  onOpenChange,
  title,
  message,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'destructive',
  onConfirm,
  onCancel,
}: ConfirmationDialogProps) {
  const styles = useStyles();

  const handleConfirm = () => {
    loggingService.info(`User confirmed: ${title}`, 'ConfirmationDialog', 'confirm');
    onConfirm();
    onOpenChange(false);
  };

  const handleCancel = () => {
    loggingService.info(`User cancelled: ${title}`, 'ConfirmationDialog', 'cancel');
    if (onCancel) {
      onCancel();
    }
    onOpenChange(false);
  };

  const getWarningColor = () => {
    switch (variant) {
      case 'destructive':
        return {
          background: tokens.colorPaletteRedBackground2,
          border: tokens.colorPaletteRedBorder2,
          foreground: tokens.colorPaletteRedForeground1,
        };
      case 'warning':
        return {
          background: tokens.colorPaletteYellowBackground2,
          border: tokens.colorPaletteYellowBorder2,
          foreground: tokens.colorPaletteYellowForeground1,
        };
      case 'info':
        return {
          background: tokens.colorNeutralBackground3,
          border: tokens.colorNeutralStroke1,
          foreground: tokens.colorNeutralForeground1,
        };
      default:
        return {
          background: tokens.colorNeutralBackground3,
          border: tokens.colorNeutralStroke1,
          foreground: tokens.colorNeutralForeground1,
        };
    }
  };

  const colors = getWarningColor();

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent className={styles.content}>
            <div
              className={styles.warningSection}
              style={{
                backgroundColor: colors.background,
                borderLeft: `4px solid ${colors.border}`,
              }}
            >
              <Warning24Regular
                className={styles.warningIcon}
                style={{ color: colors.foreground }}
              />
              <div className={styles.warningText}>
                <Text className={styles.message} weight="semibold">
                  {message}
                </Text>
                {variant === 'destructive' && (
                  <Caption1 className={styles.caption}>This action cannot be undone.</Caption1>
                )}
              </div>
            </div>
          </DialogContent>
          <DialogActions className={styles.actions}>
            {/* Cancel is the default button for destructive operations */}
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={handleCancel}
              autoFocus={variant === 'destructive'}
            >
              {cancelLabel}
            </Button>
            <Button
              appearance={variant === 'destructive' ? 'primary' : 'primary'}
              icon={variant === 'destructive' ? <Delete24Regular /> : undefined}
              onClick={handleConfirm}
              autoFocus={variant !== 'destructive'}
            >
              {confirmLabel}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
