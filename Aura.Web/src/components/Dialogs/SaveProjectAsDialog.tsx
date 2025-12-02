/**
 * Save Project As Dialog
 *
 * Dialog component for saving a project with a new name (Save As functionality).
 * Used by the ProjectContext when handling Save As operations from menu commands.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Input,
  Label,
  Textarea,
  Spinner,
  useId,
} from '@fluentui/react-components';
import { Save20Regular, Dismiss20Regular } from '@fluentui/react-icons';
import { useState, useCallback, useEffect } from 'react';
import type { FC } from 'react';

interface SaveProjectAsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (name: string, description?: string) => Promise<void>;
  currentName?: string;
  currentDescription?: string;
}

/**
 * Dialog component for Save As functionality
 */
const SaveProjectAsDialog: FC<SaveProjectAsDialogProps> = ({
  isOpen,
  onClose,
  onSave,
  currentName = '',
  currentDescription = '',
}) => {
  const nameInputId = useId();
  const descriptionInputId = useId();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset form when dialog opens
  useEffect(() => {
    if (isOpen) {
      // Generate a new name based on current name or provide default
      const newName = currentName
        ? `${currentName} (Copy)`
        : `Untitled Project ${new Date().toLocaleDateString()}`;
      setName(newName);
      setDescription(currentDescription);
      setError(null);
    }
  }, [isOpen, currentName, currentDescription]);

  const handleSave = useCallback(async () => {
    const trimmedName = name.trim();

    if (!trimmedName) {
      setError('Project name is required');
      return;
    }

    if (trimmedName.length < 3) {
      setError('Project name must be at least 3 characters');
      return;
    }

    if (trimmedName.length > 100) {
      setError('Project name must be less than 100 characters');
      return;
    }

    setIsSaving(true);
    setError(null);

    try {
      await onSave(trimmedName, description.trim() || undefined);
      onClose();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : String(err);
      setError(`Failed to save project: ${errorMessage}`);
    } finally {
      setIsSaving(false);
    }
  }, [name, description, onSave, onClose]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey && !isSaving && name.trim()) {
        e.preventDefault();
        handleSave();
      }
    },
    [handleSave, isSaving, name]
  );

  const handleClose = useCallback(() => {
    if (!isSaving) {
      onClose();
    }
  }, [isSaving, onClose]);

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && handleClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Save Project As</DialogTitle>
          <DialogContent>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div>
                <Label htmlFor={nameInputId} required>
                  Project Name
                </Label>
                <Input
                  id={nameInputId}
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    setError(null);
                  }}
                  onKeyDown={handleKeyDown}
                  placeholder="Enter a name for your project"
                  disabled={isSaving}
                  style={{ marginTop: '4px', width: '100%' }}
                  autoFocus
                />
              </div>

              <div>
                <Label htmlFor={descriptionInputId}>Description (optional)</Label>
                <Textarea
                  id={descriptionInputId}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Add a description for your project"
                  disabled={isSaving}
                  rows={3}
                  style={{ marginTop: '4px', width: '100%' }}
                />
              </div>

              {error && (
                <div
                  style={{
                    color: 'var(--colorStatusDangerForeground1)',
                    fontSize: '12px',
                    padding: '8px',
                    backgroundColor: 'var(--colorStatusDangerBackground1)',
                    borderRadius: '4px',
                  }}
                >
                  {error}
                </div>
              )}
            </div>
          </DialogContent>
          <DialogActions>
            <Button
              appearance="secondary"
              onClick={handleClose}
              disabled={isSaving}
              icon={<Dismiss20Regular />}
            >
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleSave}
              disabled={isSaving || !name.trim()}
              icon={isSaving ? <Spinner size="tiny" /> : <Save20Regular />}
            >
              {isSaving ? 'Saving...' : 'Save'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};

export default SaveProjectAsDialog;
