import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Input,
  Textarea,
  Field,
  Switch,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Add24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import type { ContentFilteringPolicy } from '../../state/userPreferences';
import { createDefaultPolicy } from '../../utils/userPreferencesDefaults';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    maxHeight: '70vh',
    overflowY: 'auto',
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  row: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: tokens.spacingHorizontalM,
  },
  switchField: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

interface CreatePolicyModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (policy: Omit<ContentFilteringPolicy, 'id' | 'createdAt' | 'updatedAt'>) => Promise<void>;
}

export const CreatePolicyModal: FC<CreatePolicyModalProps> = ({ open, onOpenChange, onSave }) => {
  const styles = useStyles();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    filteringEnabled: true,
    profanityFilter: 'Moderate',
    violenceThreshold: 5,
    blockGraphicContent: false,
  });

  const handleSubmit = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const policy = createDefaultPolicy(
        formData.name,
        formData.description,
        formData.filteringEnabled,
        formData.profanityFilter,
        formData.violenceThreshold,
        formData.blockGraphicContent
      );

      await onSave(policy);
      onOpenChange(false);

      setFormData({
        name: '',
        description: '',
        filteringEnabled: true,
        profanityFilter: 'Moderate',
        violenceThreshold: 5,
        blockGraphicContent: false,
      });
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    onOpenChange(false);
    setFormData({
      name: '',
      description: '',
      filteringEnabled: true,
      profanityFilter: 'Moderate',
      violenceThreshold: 5,
      blockGraphicContent: false,
    });
    setError(null);
  };

  const isValid = formData.name.trim().length > 0;

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Create Content Filtering Policy</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.fieldGroup}>
              <Field label="Policy Name" required>
                <Input
                  value={formData.name}
                  onChange={(_, data) => setFormData({ ...formData, name: data.value })}
                  placeholder="e.g., Family-Friendly Content"
                  disabled={isLoading}
                />
              </Field>

              <Field label="Description">
                <Textarea
                  value={formData.description}
                  onChange={(_, data) => setFormData({ ...formData, description: data.value })}
                  placeholder="Describe the filtering policy..."
                  rows={3}
                  disabled={isLoading}
                />
              </Field>

              <div className={styles.switchField}>
                <Switch
                  checked={formData.filteringEnabled}
                  onChange={(_, data) =>
                    setFormData({ ...formData, filteringEnabled: data.checked })
                  }
                  disabled={isLoading}
                />
                <Field label="Enable Content Filtering">
                  <div />
                </Field>
              </div>

              <div className={styles.row}>
                <Field label="Profanity Filter">
                  <Input
                    value={formData.profanityFilter}
                    onChange={(_, data) =>
                      setFormData({ ...formData, profanityFilter: data.value })
                    }
                    placeholder="None, Moderate, Strict"
                    disabled={isLoading}
                  />
                </Field>

                <Field label="Violence Threshold (1-10)">
                  <Input
                    type="number"
                    value={formData.violenceThreshold.toString()}
                    onChange={(_, data) =>
                      setFormData({
                        ...formData,
                        violenceThreshold: parseInt(data.value) || 5,
                      })
                    }
                    min={1}
                    max={10}
                    disabled={isLoading}
                  />
                </Field>
              </div>

              <div className={styles.switchField}>
                <Switch
                  checked={formData.blockGraphicContent}
                  onChange={(_, data) =>
                    setFormData({ ...formData, blockGraphicContent: data.checked })
                  }
                  disabled={isLoading}
                />
                <Field label="Block Graphic Content">
                  <div />
                </Field>
              </div>

              {error && (
                <Field validationMessage={error} validationState="error">
                  <div />
                </Field>
              )}
            </div>
          </DialogContent>
          <DialogActions className={styles.actions}>
            <Button
              appearance="secondary"
              icon={<Dismiss24Regular />}
              onClick={handleCancel}
              disabled={isLoading}
            >
              Cancel
            </Button>
            <Button
              appearance="primary"
              icon={isLoading ? <Spinner size="tiny" /> : <Add24Regular />}
              onClick={handleSubmit}
              disabled={!isValid || isLoading}
            >
              {isLoading ? 'Creating...' : 'Create Policy'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
