/**
 * Modal component for previewing a template
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
  makeStyles,
  tokens,
  Badge,
} from '@fluentui/react-components';
import { Dismiss24Regular, Star24Filled } from '@fluentui/react-icons';
import { useState } from 'react';
import { TemplateListItem } from '../../types/templates';

const useStyles = makeStyles({
  dialogSurface: {
    maxWidth: '900px',
    width: '90vw',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  titleRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  previewContainer: {
    width: '100%',
    height: '400px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: tokens.spacingVerticalL,
    overflow: 'hidden',
  },
  previewImage: {
    maxWidth: '100%',
    maxHeight: '100%',
    objectFit: 'contain',
  },
  video: {
    width: '100%',
    height: '100%',
    objectFit: 'contain',
  },
  metadata: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  metadataRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalL,
    alignItems: 'center',
  },
  label: {
    fontWeight: tokens.fontWeightSemibold,
    minWidth: '100px',
  },
  tags: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
  },
  rating: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
});

export interface TemplatePreviewProps {
  template: TemplateListItem | null;
  open: boolean;
  onClose: () => void;
  onUseTemplate: (template: TemplateListItem) => void;
}

export function TemplatePreview({ template, open, onClose, onUseTemplate }: TemplatePreviewProps) {
  const styles = useStyles();
  const [isVideoPlaying, setIsVideoPlaying] = useState(false);

  if (!template) return null;

  const handleUseTemplate = () => {
    onUseTemplate(template);
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <div className={styles.header}>
            <DialogTitle>
              <div className={styles.titleRow}>
                {template.name}
                <Badge appearance="outline">{template.subCategory || template.category}</Badge>
              </div>
            </DialogTitle>
            <Button
              appearance="subtle"
              icon={<Dismiss24Regular />}
              onClick={onClose}
              aria-label="Close"
            />
          </div>
          <DialogContent>
            <div className={styles.previewContainer}>
              {template.previewVideo && isVideoPlaying ? (
                <video
                  className={styles.video}
                  src={template.previewVideo}
                  controls
                  autoPlay
                  loop
                  onError={() => setIsVideoPlaying(false)}
                />
              ) : template.previewImage ? (
                <img
                  src={template.previewImage}
                  alt={template.name}
                  className={styles.previewImage}
                  onClick={() => template.previewVideo && setIsVideoPlaying(true)}
                  style={{ cursor: template.previewVideo ? 'pointer' : 'default' }}
                  onError={(e) => {
                    (e.target as HTMLImageElement).src = '';
                    (e.target as HTMLImageElement).alt = 'No preview available';
                  }}
                />
              ) : (
                <Text>No preview available</Text>
              )}
            </div>

            <div className={styles.metadata}>
              <div className={styles.metadataRow}>
                <Text className={styles.label}>Description:</Text>
                <Text>{template.description}</Text>
              </div>
              <div className={styles.metadataRow}>
                <Text className={styles.label}>Category:</Text>
                <Text>
                  {template.category}
                  {template.subCategory && ` > ${template.subCategory}`}
                </Text>
              </div>
              <div className={styles.metadataRow}>
                <Text className={styles.label}>Rating:</Text>
                <div className={styles.rating}>
                  <Star24Filled color={tokens.colorPaletteYellowForeground1} />
                  <Text>{template.rating.toFixed(1)}</Text>
                </div>
              </div>
              <div className={styles.metadataRow}>
                <Text className={styles.label}>Used:</Text>
                <Text>{template.usageCount} times</Text>
              </div>
              {template.tags.length > 0 && (
                <div className={styles.metadataRow}>
                  <Text className={styles.label}>Tags:</Text>
                  <div className={styles.tags}>
                    {template.tags.map((tag) => (
                      <Badge key={tag} size="small" appearance="tint">
                        {tag}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleUseTemplate}>
              Use Template
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
