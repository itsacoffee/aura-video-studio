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
  ProgressBar,
  Text,
  Label,
} from '@fluentui/react-components';
import { CloudArrowUp24Regular } from '@fluentui/react-icons';
import { useMutation } from '@tanstack/react-query';
import React, { useState, useCallback } from 'react';
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
  onClose,
  onSuccess,
}) => {
  const styles = useStyles();
  const [files, setFiles] = useState<File[]>([]);
  const [dragActive, setDragActive] = useState(false);
  const [dragDepth, setDragDepth] = useState(0);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});
  const fileInputRef = React.useRef<HTMLInputElement>(null);

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

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragDepth((prev) => prev + 1);
    setDragActive(true);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragDepth((prev) => {
      const newDepth = prev - 1;
      if (newDepth === 0) {
        setDragActive(false);
      }
      return newDepth;
    });
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    setDragDepth(0);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const newFiles = Array.from(e.dataTransfer.files);
      setFiles((prev) => {
        const allFiles = [...prev, ...newFiles];
        // Deduplicate based on name, lastModified, and size
        return allFiles.filter(
          (file, index, self) =>
            index ===
            self.findIndex(
              (f) =>
                f.name === file.name &&
                f.lastModified === file.lastModified &&
                f.size === file.size
            )
        );
      });
    }
  }, []);

  const handleFileSelect = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const newFiles = Array.from(e.target.files);
      setFiles((prev) => {
        const allFiles = [...prev, ...newFiles];
        // Deduplicate based on name, lastModified, and size
        return allFiles.filter(
          (file, index, self) =>
            index ===
            self.findIndex(
              (f) =>
                f.name === file.name &&
                f.lastModified === file.lastModified &&
                f.size === file.size
            )
        );
      });
    }
    // Clear input value to allow reselecting the same file
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
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
                  onDragEnter={handleDragEnter}
                  onDragLeave={handleDragLeave}
                  onDragOver={handleDragOver}
                  onDrop={handleDrop}
                  onClick={() => fileInputRef.current?.click()}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      fileInputRef.current?.click();
                    }
                  }}
                  role="button"
                  tabIndex={0}
                  aria-label="Drop files here or click to select files"
                >
                  <CloudArrowUp24Regular style={{ fontSize: '48px' }} />
                  <Text size={400} weight="semibold">
                    Drag & drop files here or click to browse
                  </Text>
                  <Text size={200}>
                    Supports videos, images, audio, and documents
                  </Text>
                  <input
                    ref={fileInputRef}
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
                    onClick={() => fileInputRef.current?.click()}
                  >
                    Add more files
                  </Button>
                  <input
                    ref={fileInputRef}
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
