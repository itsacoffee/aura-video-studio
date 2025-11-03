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
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Add24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState } from 'react';
import type { FC } from 'react';
import type { CustomAudienceProfile } from '../../state/userPreferences';
import { createDefaultProfile } from '../../utils/userPreferencesDefaults';

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
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
});

interface CreateProfileModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSave: (profile: Omit<CustomAudienceProfile, 'id' | 'createdAt' | 'updatedAt'>) => Promise<void>;
}

export const CreateProfileModal: FC<CreateProfileModalProps> = ({ open, onOpenChange, onSave }) => {
  const styles = useStyles();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    description: '',
    minAge: 18,
    maxAge: 65,
    formalityLevel: 5,
    educationLevel: 'College',
  });

  const handleSubmit = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const profile = createDefaultProfile(
        formData.name,
        formData.description,
        formData.minAge,
        formData.maxAge,
        formData.formalityLevel,
        formData.educationLevel
      );

      await onSave(profile);
      onOpenChange(false);

      setFormData({
        name: '',
        description: '',
        minAge: 18,
        maxAge: 65,
        formalityLevel: 5,
        educationLevel: 'College',
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
      minAge: 18,
      maxAge: 65,
      formalityLevel: 5,
      educationLevel: 'College',
    });
    setError(null);
  };

  const isValid = formData.name.trim().length > 0 && formData.minAge < formData.maxAge;

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Create Custom Audience Profile</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.fieldGroup}>
              <Field label="Profile Name" required>
                <Input
                  value={formData.name}
                  onChange={(_, data) => setFormData({ ...formData, name: data.value })}
                  placeholder="e.g., Tech-Savvy Millennials"
                  disabled={isLoading}
                />
              </Field>

              <Field label="Description">
                <Textarea
                  value={formData.description}
                  onChange={(_, data) => setFormData({ ...formData, description: data.value })}
                  placeholder="Describe the target audience..."
                  rows={3}
                  disabled={isLoading}
                />
              </Field>

              <div className={styles.row}>
                <Field label="Minimum Age">
                  <Input
                    type="number"
                    value={formData.minAge.toString()}
                    onChange={(_, data) =>
                      setFormData({ ...formData, minAge: parseInt(data.value) || 0 })
                    }
                    min={0}
                    max={100}
                    disabled={isLoading}
                  />
                </Field>

                <Field label="Maximum Age">
                  <Input
                    type="number"
                    value={formData.maxAge.toString()}
                    onChange={(_, data) =>
                      setFormData({ ...formData, maxAge: parseInt(data.value) || 0 })
                    }
                    min={0}
                    max={100}
                    disabled={isLoading}
                  />
                </Field>
              </div>

              <div className={styles.row}>
                <Field label="Education Level">
                  <Input
                    value={formData.educationLevel}
                    onChange={(_, data) => setFormData({ ...formData, educationLevel: data.value })}
                    placeholder="e.g., High School, College"
                    disabled={isLoading}
                  />
                </Field>

                <Field label="Formality Level (1-10)">
                  <Input
                    type="number"
                    value={formData.formalityLevel.toString()}
                    onChange={(_, data) =>
                      setFormData({ ...formData, formalityLevel: parseInt(data.value) || 5 })
                    }
                    min={1}
                    max={10}
                    disabled={isLoading}
                  />
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
              {isLoading ? 'Creating...' : 'Create Profile'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
