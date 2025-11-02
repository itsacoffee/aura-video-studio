import {
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Input,
  Label,
  makeStyles,
  tokens,
  Textarea,
} from '@fluentui/react-components';
import { useState, useEffect } from 'react';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
});

interface SaveWorkspaceDialogProps {
  open: boolean;
  onClose: () => void;
  onSave: (name: string, description: string) => void;
}

export function SaveWorkspaceDialog({ open, onClose, onSave }: SaveWorkspaceDialogProps) {
  const styles = useStyles();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  useEffect(() => {
    if (open) {
      setName('');
      setDescription('');
    }
  }, [open]);

  const handleSave = () => {
    if (name.trim()) {
      onSave(name.trim(), description.trim());
      onClose();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSave();
    }
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Save Workspace</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.field}>
              <Label htmlFor="workspace-name" required>
                Workspace Name
              </Label>
              <Input
                id="workspace-name"
                value={name}
                onChange={(_, data) => setName(data.value)}
                onKeyDown={handleKeyDown}
                placeholder="e.g., My Custom Layout"
              />
            </div>
            <div className={styles.field}>
              <Label htmlFor="workspace-description">Description</Label>
              <Textarea
                id="workspace-description"
                value={description}
                onChange={(_, data) => setDescription(data.value)}
                placeholder="Optional description of this workspace layout"
                rows={3}
              />
            </div>
          </DialogContent>
          <DialogActions>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary" onClick={onClose}>
                Cancel
              </Button>
            </DialogTrigger>
            <Button appearance="primary" onClick={handleSave} disabled={!name.trim()}>
              Save
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
