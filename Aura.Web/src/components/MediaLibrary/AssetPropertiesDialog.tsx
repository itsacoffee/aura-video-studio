/**
 * AssetPropertiesDialog - Dialog component for viewing and editing asset properties
 *
 * Allows users to view metadata and edit properties like name and tags
 * for media assets in the library.
 */

import {
  Dialog,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Field,
  Input,
  makeStyles,
  tokens,
} from '@fluentui/react-components';
import { Save24Regular, Dismiss24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect, useCallback } from 'react';

const useStyles = makeStyles({
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalS,
  },
  fieldGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  readOnlyInput: {
    backgroundColor: tokens.colorNeutralBackground3,
  },
  dialogSurface: {
    maxWidth: '500px',
    width: '90vw',
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    justifyContent: 'flex-end',
  },
});

export interface AssetPropertiesDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (assetId: string, updates: { name: string; tags: string[] }) => void;
  asset: {
    id: string;
    name: string;
    type: 'video' | 'audio' | 'image';
    filePath?: string;
    duration?: number;
    fileSize?: number;
    tags?: string[];
  } | null;
}

/**
 * Format file size to human-readable string
 */
function formatFileSize(bytes: number | undefined): string {
  if (bytes === undefined) return 'Unknown';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Format duration in seconds to MM:SS or HH:MM:SS
 */
function formatDuration(seconds: number | undefined): string {
  if (seconds === undefined) return 'Unknown';

  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);

  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }
  return `${minutes}:${secs.toString().padStart(2, '0')}`;
}

export function AssetPropertiesDialog({
  isOpen,
  onClose,
  onSave,
  asset,
}: AssetPropertiesDialogProps) {
  const styles = useStyles();
  const [name, setName] = useState('');
  const [tags, setTags] = useState('');

  // Reset form when asset changes
  useEffect(() => {
    if (asset) {
      setName(asset.name);
      setTags((asset.tags || []).join(', '));
    }
  }, [asset]);

  const handleSave = useCallback(() => {
    if (!asset) return;

    const parsedTags = tags
      .split(',')
      .map((t) => t.trim())
      .filter(Boolean);

    onSave(asset.id, {
      name: name.trim() || asset.name,
      tags: parsedTags,
    });
    onClose();
  }, [asset, name, tags, onSave, onClose]);

  const handleNameChange = useCallback(
    (_e: React.ChangeEvent<HTMLInputElement>, data: { value: string }) => {
      setName(data.value);
    },
    []
  );

  const handleTagsChange = useCallback(
    (_e: React.ChangeEvent<HTMLInputElement>, data: { value: string }) => {
      setTags(data.value);
    },
    []
  );

  if (!asset) return null;

  return (
    <Dialog open={isOpen} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle>Asset Properties</DialogTitle>
          <DialogContent className={styles.content}>
            <div className={styles.fieldGroup}>
              <Field label="Name">
                <Input value={name} onChange={handleNameChange} />
              </Field>

              <Field label="Tags (comma separated)">
                <Input value={tags} onChange={handleTagsChange} placeholder="tag1, tag2, tag3" />
              </Field>

              <Field label="Type">
                <Input
                  value={asset.type.charAt(0).toUpperCase() + asset.type.slice(1)}
                  readOnly
                  disabled
                  className={styles.readOnlyInput}
                />
              </Field>

              {(asset.type === 'video' || asset.type === 'audio') && (
                <Field label="Duration">
                  <Input
                    value={formatDuration(asset.duration)}
                    readOnly
                    disabled
                    className={styles.readOnlyInput}
                  />
                </Field>
              )}

              <Field label="File Size">
                <Input
                  value={formatFileSize(asset.fileSize)}
                  readOnly
                  disabled
                  className={styles.readOnlyInput}
                />
              </Field>

              {asset.filePath && (
                <Field label="File Path">
                  <Input
                    value={asset.filePath}
                    readOnly
                    disabled
                    className={styles.readOnlyInput}
                  />
                </Field>
              )}
            </div>
          </DialogContent>
          <DialogActions className={styles.actions}>
            <Button appearance="secondary" icon={<Dismiss24Regular />} onClick={onClose}>
              Cancel
            </Button>
            <Button appearance="primary" icon={<Save24Regular />} onClick={handleSave}>
              Save
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
