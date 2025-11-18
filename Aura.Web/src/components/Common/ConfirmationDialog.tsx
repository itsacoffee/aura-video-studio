/**
 * Confirmation Dialog Component
 * Reusable dialog for confirming risky operations
 */

import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  makeStyles,
  shorthands,
  Text,
  tokens,
} from '@fluentui/react-components';
import { Warning24Regular } from '@fluentui/react-icons';
import { ReactElement } from 'react';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  warningSection: {
    display: 'flex',
    ...shorthands.gap(tokens.spacingHorizontalS),
    ...shorthands.padding(tokens.spacingVerticalS),
    backgroundColor: tokens.colorPaletteYellowBackground1,
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.border('1px', 'solid', tokens.colorPaletteYellowBorder1),
  },
  warningIcon: {
    color: tokens.colorPaletteYellowForeground1,
    flexShrink: 0,
  },
  consequencesList: {
    marginTop: tokens.spacingVerticalXS,
    paddingLeft: tokens.spacingHorizontalL,
  },
});

export type ConfirmationSeverity = 'warning' | 'danger' | 'info';

export interface ConfirmationDialogProps {
  /** The trigger element (e.g., a button) - must be a ReactElement, not a string */
  trigger: ReactElement;

  /** Dialog title */
  title: string;

  /** Main message */
  message: string;

  /** List of consequences of the action */
  consequences?: string[];

  /** Severity level */
  severity?: ConfirmationSeverity;

  /** Confirm button text */
  confirmText?: string;

  /** Cancel button text */
  cancelText?: string;

  /** Callback when confirmed */
  onConfirm: () => void | Promise<void>;

  /** Callback when cancelled */
  onCancel?: () => void;

  /** Whether the dialog is open (controlled) */
  open?: boolean;

  /** Callback when open state changes */
  onOpenChange?: (open: boolean) => void;
}

export function ConfirmationDialog({
  trigger,
  title,
  message,
  consequences,
  severity = 'warning',
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  onConfirm,
  onCancel,
  open: controlledOpen,
  onOpenChange,
}: ConfirmationDialogProps) {
  const styles = useStyles();

  const handleConfirm = async () => {
    const result = onConfirm();
    if (result instanceof Promise) {
      await result;
    }
    onOpenChange?.(false);
  };

  const handleCancel = () => {
    onCancel?.();
    onOpenChange?.(false);
  };

  const getConfirmAppearance = ():
    | 'primary'
    | 'secondary'
    | 'outline'
    | 'subtle'
    | 'transparent' => {
    switch (severity) {
      case 'danger':
        return 'primary'; // Red background for danger
      case 'warning':
        return 'primary';
      case 'info':
        return 'primary';
      default:
        return 'primary';
    }
  };

  return (
    <Dialog open={controlledOpen} onOpenChange={(_, data) => onOpenChange?.(data.open)}>
      <DialogTrigger disableButtonEnhancement>{trigger}</DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{title}</DialogTitle>
          <DialogContent className={styles.content}>
            <Text>{message}</Text>

            {consequences && consequences.length > 0 && (
              <div className={styles.warningSection}>
                <Warning24Regular className={styles.warningIcon} />
                <div>
                  <Text weight="semibold" block>
                    This action will:
                  </Text>
                  <ul className={styles.consequencesList}>
                    {consequences.map((consequence, index) => (
                      <li key={index}>
                        <Text size={300}>{consequence}</Text>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            )}
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" onClick={handleCancel}>
                {cancelText}
              </Button>
            </DialogTrigger>
            <Button appearance={getConfirmAppearance()} onClick={handleConfirm}>
              {confirmText}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
