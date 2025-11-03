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
import { Edit24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import { useState, useEffect } from 'react';
import type { FC } from 'react';
import type { CustomAudienceProfile } from '../../state/userPreferences';

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

interface EditProfileModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  profile: CustomAudienceProfile | null;
  onSave: (id: string, profile: Partial<CustomAudienceProfile>) => Promise<void>;
}

export const EditProfileModal: FC<EditProfileModalProps> = ({
  open,
  onOpenChange,
  profile,
  onSave,
}) => {
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

  useEffect(() => {
    if (profile) {
      setFormData({
        name: profile.name,
        description: profile.description || '',
        minAge: profile.minAge,
        maxAge: profile.maxAge,
        formalityLevel: profile.formalityLevel,
        educationLevel: profile.educationLevel,
      });
    }
  }, [profile]);

  const handleSubmit = async () => {
    if (!profile) return;

    setIsLoading(true);
    setError(null);

    try {
      await onSave(profile.id, {
        name: formData.name,
        description: formData.description,
        minAge: formData.minAge,
        maxAge: formData.maxAge,
        formalityLevel: formData.formalityLevel,
        educationLevel: formData.educationLevel,
        updatedAt: new Date(),
      });
      onOpenChange(false);
    } catch (err: unknown) {
      const errorObj = err instanceof Error ? err : new Error(String(err));
      setError(errorObj.message);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    onOpenChange(false);
    setError(null);
  };

  const isValid = formData.name.trim().length > 0 && formData.minAge < formData.maxAge;

  return (
    <Dialog open={open} onOpenChange={(_, data) => onOpenChange(data.open)}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Edit Audience Profile</DialogTitle>
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
              icon={isLoading ? <Spinner size="tiny" /> : <Edit24Regular />}
              onClick={handleSubmit}
              disabled={!isValid || isLoading}
            >
              {isLoading ? 'Saving...' : 'Save Changes'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
