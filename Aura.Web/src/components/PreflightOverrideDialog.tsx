/**
 * Preflight Override Dialog Component
 * Shows a detailed warning when user attempts to override failed preflight checks
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Text,
} from '@fluentui/react-components';
import { Warning24Regular } from '@fluentui/react-icons';

interface PreflightOverrideDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  errors: string[];
  onConfirm: () => void;
}

export function PreflightOverrideDialog({
  open,
  onOpenChange,
  errors,
  onConfirm,
}: PreflightOverrideDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>
            <Warning24Regular
              style={{ marginRight: '8px', color: 'var(--colorStatusWarningForeground1)' }}
            />
            Override Preflight Checks?
          </DialogTitle>
          <DialogContent>
            <Text>The following issues were detected:</Text>
            <ul style={{ marginTop: '12px', marginBottom: '12px' }}>
              {errors.map((error, i) => (
                <li key={i} style={{ color: 'var(--colorStatusDangerForeground1)' }}>
                  {error}
                </li>
              ))}
            </ul>
            <Text weight="semibold">Proceeding anyway may result in:</Text>
            <ul style={{ marginTop: '8px' }}>
              <li>Incomplete or corrupted video output</li>
              <li>Missing audio or visuals</li>
              <li>Job failure after significant processing time</li>
              <li>Wasted computational resources</li>
            </ul>
            <Text style={{ marginTop: '12px', display: 'block' }}>
              Are you sure you want to proceed?
            </Text>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={onConfirm}>
              Proceed Anyway
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

export default PreflightOverrideDialog;
