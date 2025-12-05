/**
 * SaveTemplateDialog Component
 *
 * Dialog for saving the current project configuration as a reusable template.
 * Captures name, description, category, and tags.
 */

import {
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Input,
  Label,
  Textarea,
  Dropdown,
  Option,
  TagGroup,
  Tag,
  makeStyles,
  tokens,
  Text,
} from '@fluentui/react-components';
import { Dismiss24Regular, Add20Regular } from '@fluentui/react-icons';
import { useState, useCallback } from 'react';
import type { FC } from 'react';
import { useTemplatesStore, type TemplateData } from '../../../stores/opencutTemplates';
import { openCutTokens } from '../../../styles/designTokens';

export interface SaveTemplateDialogProps {
  open: boolean;
  onClose: () => void;
  templateData: TemplateData;
  aspectRatio?: string;
  onSaved?: (templateId: string) => void;
}

const useStyles = makeStyles({
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.lg,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  tagInput: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  tagInputField: {
    flex: 1,
  },
  tagGroup: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalXS,
  },
  preview: {
    padding: openCutTokens.spacing.md,
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: openCutTokens.radius.md,
    marginTop: tokens.spacingVerticalS,
  },
  previewLabel: {
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalXS,
  },
  previewItem: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground2,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
});

const CATEGORIES = ['Custom', 'Social Media', 'Business', 'Entertainment', 'Education', 'Personal'];

export const SaveTemplateDialog: FC<SaveTemplateDialogProps> = ({
  open,
  onClose,
  templateData,
  aspectRatio = '16:9',
  onSaved,
}) => {
  const styles = useStyles();
  const { createTemplate } = useTemplatesStore();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Custom');
  const [tags, setTags] = useState<string[]>([]);
  const [tagInput, setTagInput] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleAddTag = useCallback(() => {
    const trimmed = tagInput.trim().toLowerCase();
    if (trimmed && !tags.includes(trimmed)) {
      setTags([...tags, trimmed]);
      setTagInput('');
    }
  }, [tagInput, tags]);

  const handleRemoveTag = useCallback((tagToRemove: string) => {
    setTags((current) => current.filter((t) => t !== tagToRemove));
  }, []);

  const handleTagInputKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        handleAddTag();
      }
    },
    [handleAddTag]
  );

  const handleSave = useCallback(() => {
    if (!name.trim()) {
      setError('Please enter a template name');
      return;
    }

    const templateId = createTemplate(
      name.trim(),
      description.trim(),
      templateData,
      aspectRatio,
      category,
      tags
    );

    onSaved?.(templateId);
    onClose();

    // Reset form
    setName('');
    setDescription('');
    setCategory('Custom');
    setTags([]);
    setTagInput('');
    setError(null);
  }, [
    name,
    description,
    templateData,
    aspectRatio,
    category,
    tags,
    createTemplate,
    onSaved,
    onClose,
  ]);

  const handleClose = useCallback(() => {
    onClose();
    setError(null);
  }, [onClose]);

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && handleClose()}>
      <DialogSurface>
        <DialogTitle
          action={
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={handleClose}
              aria-label="Close"
            />
          }
        >
          Save as Template
        </DialogTitle>

        <DialogBody>
          <DialogContent>
            <div className={styles.form}>
              <div className={styles.field}>
                <Label htmlFor="template-name" required>
                  Template Name
                </Label>
                <Input
                  id="template-name"
                  value={name}
                  onChange={(_, data) => {
                    setName(data.value);
                    setError(null);
                  }}
                  placeholder="Enter a name for your template"
                  maxLength={100}
                />
                {error && <Text className={styles.error}>{error}</Text>}
              </div>

              <div className={styles.field}>
                <Label htmlFor="template-description">Description</Label>
                <Textarea
                  id="template-description"
                  value={description}
                  onChange={(_, data) => setDescription(data.value)}
                  placeholder="Describe what this template is for"
                  rows={3}
                  maxLength={500}
                />
              </div>

              <div className={styles.field}>
                <Label htmlFor="template-category">Category</Label>
                <Dropdown
                  id="template-category"
                  value={category}
                  selectedOptions={[category]}
                  onOptionSelect={(_, data) => setCategory(data.optionValue || 'Custom')}
                >
                  {CATEGORIES.map((cat) => (
                    <Option key={cat} value={cat}>
                      {cat}
                    </Option>
                  ))}
                </Dropdown>
              </div>

              <div className={styles.field}>
                <Label htmlFor="template-tags">Tags</Label>
                <div className={styles.tagInput}>
                  <Input
                    id="template-tags"
                    className={styles.tagInputField}
                    value={tagInput}
                    onChange={(_, data) => setTagInput(data.value)}
                    onKeyDown={handleTagInputKeyDown}
                    placeholder="Add tags (press Enter)"
                    maxLength={30}
                  />
                  <Button
                    appearance="secondary"
                    icon={<Add20Regular />}
                    onClick={handleAddTag}
                    disabled={!tagInput.trim()}
                  >
                    Add
                  </Button>
                </div>
                {tags.length > 0 && (
                  <TagGroup className={styles.tagGroup} aria-label="Template tags">
                    {tags.map((tag) => (
                      <Tag
                        key={tag}
                        value={tag}
                        dismissible
                        dismissIcon={{ 'aria-label': `Remove ${tag}` }}
                        onClick={() => handleRemoveTag(tag)}
                      >
                        {tag}
                      </Tag>
                    ))}
                  </TagGroup>
                )}
              </div>

              <div className={styles.preview}>
                <Text className={styles.previewLabel} size={200}>
                  Template will include:
                </Text>
                <Text className={styles.previewItem} block>
                  • {templateData.tracks.length} tracks
                </Text>
                <Text className={styles.previewItem} block>
                  • {templateData.clips.length} clips
                </Text>
                <Text className={styles.previewItem} block>
                  • {templateData.markers.length} markers
                </Text>
                <Text className={styles.previewItem} block>
                  • Aspect ratio: {aspectRatio}
                </Text>
              </div>
            </div>
          </DialogContent>
        </DialogBody>

        <DialogActions>
          <Button appearance="secondary" onClick={handleClose}>
            Cancel
          </Button>
          <Button appearance="primary" onClick={handleSave}>
            Save Template
          </Button>
        </DialogActions>
      </DialogSurface>
    </Dialog>
  );
};

export default SaveTemplateDialog;
