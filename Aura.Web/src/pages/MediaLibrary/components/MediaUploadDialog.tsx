import React, { useState, useCallback } from 'react';
import {
  makeStyles,
  shorthands,
  tokens,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Input,
  Textarea,
  Dropdown,
  Option,
  TagPicker,
  TagPickerControl,
  TagPickerGroup,
  TagPickerInput,
  TagPickerList,
  TagPickerOption,
  Tag,
  ProgressBar,
  Text,
  Label,
} from '@fluentui/react-components';
import { CloudArrowUp24Regular } from '@fluentui/react-icons';
import { useMutation } from '@tanstack/react-query';
import { mediaLibraryApi } from '../../../api/mediaLibraryApi';
import type {
  MediaCollectionResponse,
  MediaUploadRequest,
  MediaType,
} from '../../../types/mediaLibrary';

const useStyles = makeStyles({
  dropzone: {
    ...shorthands.border('2px', 'dashed', tokens.colorNeutralStroke1),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalXXL),
    textAlign: 'center',
    cursor: 'pointer',
    backgroundColor: tokens.colorNeutralBackground1,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground2,
    },
  },
  dropzoneActive: {
    ...shorthands.borderColor(tokens.colorBrandBackground),
    backgroundColor: tokens.colorBrandBackground2,
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalM),
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
  },
  fileList: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.gap(tokens.spacingVerticalXS),
  },
  fileItem: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    ...shorthands.padding(tokens.spacingVerticalS),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    backgroundColor: tokens.colorNeutralBackground2,
  },
});

interface MediaUploadDialogProps {
  collections?: MediaCollectionResponse[];
  tags?: string[];
  onClose: () => void;
  onSuccess: () => void;
}

export const MediaUploadDialog: React.FC<MediaUploadDialogProps> = ({
  collections,
  tags,
  onClose,
  onSuccess,
}) => {
  const styles = useStyles();
  const [files, setFiles] = useState<File[]>([]);
  const [dragActive, setDragActive] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});

  const [formData, setFormData] = useState<Omit<MediaUploadRequest, 'fileName' | 'type'>>({
    description: '',
    tags: [],
    collectionId: undefined,
    generateThumbnail: true,
    extractMetadata: true,
  });

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      const type = getMediaType(file);
      const request: MediaUploadRequest = {
        ...formData,
        fileName: file.name,
        type,
      };
      return mediaLibraryApi.uploadMedia(file, request);
    },
    onSuccess: (_, file) => {
      setUploadProgress((prev) => ({ ...prev, [file.name]: 100 }));
    },
  });

  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const newFiles = Array.from(e.dataTransfer.files);
      setFiles((prev) => [...prev, ...newFiles]);
    }
  }, []);

  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const newFiles = Array.from(e.target.files);
      setFiles((prev) => [...prev, ...newFiles]);
    }
  }, []);

  const handleUpload = useCallback(async () => {
    for (const file of files) {
      setUploadProgress((prev) => ({ ...prev, [file.name]: 0 }));
      await uploadMutation.mutateAsync(file);
    }
    onSuccess();
  }, [files, uploadMutation, onSuccess]);

  const getMediaType = (file: File): MediaType => {
    if (file.type.startsWith('video/')) return 'Video';
    if (file.type.startsWith('image/')) return 'Image';
    if (file.type.startsWith('audio/')) return 'Audio';
    return 'Other';
  };

  return (
    <Dialog open onOpenChange={onClose}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Upload Media</DialogTitle>
          <DialogContent>
            <div className={styles.form}>
              {files.length === 0 ? (
                <div
                  className={`${styles.dropzone} ${
                    dragActive ? styles.dropzoneActive : ''
                  }`}
                  onDragEnter={handleDrag}
                  onDragLeave={handleDrag}
                  onDragOver={handleDrag}
                  onDrop={handleDrop}
                  onClick={() => document.getElementById('file-input')?.click()}
                >
                  <CloudArrowUp24Regular style={{ fontSize: '48px' }} />
                  <Text size={400} weight="semibold">
                    Drag & drop files here or click to browse
                  </Text>
                  <Text size={200}>
                    Supports videos, images, audio, and documents
                  </Text>
                  <input
                    id="file-input"
                    type="file"
                    multiple
                    hidden
                    onChange={handleFileSelect}
                  />
                </div>
              ) : (
                <div className={styles.fileList}>
                  {files.map((file) => (
                    <div key={file.name} className={styles.fileItem}>
                      <Text>{file.name}</Text>
                      {uploadProgress[file.name] !== undefined && (
                        <ProgressBar value={uploadProgress[file.name]} />
                      )}
                    </div>
                  ))}
                  <Button
                    appearance="subtle"
                    onClick={() => document.getElementById('file-input')?.click()}
                  >
                    Add more files
                  </Button>
                  <input
                    id="file-input"
                    type="file"
                    multiple
                    hidden
                    onChange={handleFileSelect}
                  />
                </div>
              )}

              <div className={styles.field}>
                <Label>Description</Label>
                <Textarea
                  value={formData.description}
                  onChange={(_, data) =>
                    setFormData((prev) => ({ ...prev, description: data.value }))
                  }
                  placeholder="Add a description..."
                />
              </div>

              {collections && collections.length > 0 && (
                <div className={styles.field}>
                  <Label>Collection</Label>
                  <Dropdown
                    placeholder="Select collection"
                    value={
                      collections.find((c) => c.id === formData.collectionId)?.name
                    }
                    onOptionSelect={(_, data) =>
                      setFormData((prev) => ({
                        ...prev,
                        collectionId: data.optionValue,
                      }))
                    }
                  >
                    <Option value="">None</Option>
                    {collections.map((collection) => (
                      <Option key={collection.id} value={collection.id}>
                        {collection.name}
                      </Option>
                    ))}
                  </Dropdown>
                </div>
              )}

              <div className={styles.field}>
                <Label>Tags</Label>
                <Input
                  placeholder="Add tags (comma-separated)"
                  onChange={(_, data) =>
                    setFormData((prev) => ({
                      ...prev,
                      tags: data.value.split(',').map((t) => t.trim()),
                    }))
                  }
                />
              </div>
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleUpload}
              disabled={files.length === 0 || uploadMutation.isPending}
            >
              {uploadMutation.isPending ? 'Uploading...' : 'Upload'}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
};
