import { useState, useRef, DragEvent } from 'react';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  Spinner,
  Card,
  CardHeader,
  CardPreview,
} from '@fluentui/react-components';
import {
  Add24Regular,
  VideoClip24Regular,
  MusicNote224Regular,
  Image24Regular,
  Dismiss24Regular,
} from '@fluentui/react-icons';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    marginBottom: tokens.spacingVerticalXS,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  uploadArea: {
    margin: tokens.spacingVerticalM,
    padding: tokens.spacingVerticalXXL,
    border: `2px dashed ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
    textAlign: 'center',
    cursor: 'pointer',
    backgroundColor: tokens.colorNeutralBackground3,
    transitionProperty: 'all',
    transitionDuration: '0.2s',
    transitionTimingFunction: 'ease',
    '&:hover': {
      border: `2px dashed ${tokens.colorBrandStroke1}`,
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  uploadAreaDragging: {
    border: `2px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  uploadIcon: {
    fontSize: '48px',
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalM,
  },
  uploadText: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    marginBottom: tokens.spacingVerticalS,
  },
  uploadHint: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  libraryContent: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingVerticalM,
  },
  clipGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  clipCard: {
    cursor: 'grab',
    position: 'relative',
    '&:hover': {
      boxShadow: tokens.shadow8,
    },
    '&:active': {
      cursor: 'grabbing',
    },
  },
  clipPreview: {
    height: '80px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground4,
    fontSize: '32px',
  },
  clipHeader: {
    padding: tokens.spacingVerticalXS,
  },
  clipName: {
    fontSize: tokens.fontSizeBase200,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  clipType: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  removeButton: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    minWidth: '24px',
    minHeight: '24px',
    padding: '4px',
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalM,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
  },
});

export interface MediaClip {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'image';
  file: File;
  duration?: number;
}

interface MediaLibraryPanelProps {
  onClipDragStart?: (clip: MediaClip) => void;
  onClipDragEnd?: () => void;
}

export function MediaLibraryPanel({ onClipDragStart, onClipDragEnd }: MediaLibraryPanelProps) {
  const styles = useStyles();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [clips, setClips] = useState<MediaClip[]>([]);
  const [isDraggingOver, setIsDraggingOver] = useState(false);
  const [isUploading, setIsUploading] = useState(false);

  const getClipType = (file: File): 'video' | 'audio' | 'image' => {
    if (file.type.startsWith('video/')) return 'video';
    if (file.type.startsWith('audio/')) return 'audio';
    if (file.type.startsWith('image/')) return 'image';
    return 'video'; // default
  };

  const getClipIcon = (type: 'video' | 'audio' | 'image') => {
    switch (type) {
      case 'video':
        return <VideoClip24Regular />;
      case 'audio':
        return <MusicNote224Regular />;
      case 'image':
        return <Image24Regular />;
    }
  };

  const handleFiles = async (files: FileList | null) => {
    if (!files || files.length === 0) return;

    setIsUploading(true);

    // Simulate upload processing
    await new Promise((resolve) => setTimeout(resolve, 500));

    const newClips: MediaClip[] = Array.from(files).map((file) => ({
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      name: file.name,
      type: getClipType(file),
      file,
    }));

    setClips((prev) => [...prev, ...newClips]);
    setIsUploading(false);
  };

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    handleFiles(e.target.files);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDraggingOver(false);

    const files = e.dataTransfer.files;
    handleFiles(files);
  };

  const handleUploadAreaClick = () => {
    fileInputRef.current?.click();
  };

  const handleClipDragStart = (e: DragEvent<HTMLDivElement>, clip: MediaClip) => {
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('application/json', JSON.stringify(clip));
    onClipDragStart?.(clip);
  };

  const handleClipDragEnd = () => {
    onClipDragEnd?.();
  };

  const handleRemoveClip = (clipId: string) => {
    setClips((prev) => prev.filter((c) => c.id !== clipId));
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Text className={styles.title}>Media Library</Text>
        <Text className={styles.subtitle}>Drag clips to timeline</Text>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="video/*,audio/*,image/*"
        multiple
        style={{ display: 'none' }}
        onChange={handleFileInputChange}
      />

      <div
        className={`${styles.uploadArea} ${isDraggingOver ? styles.uploadAreaDragging : ''}`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={handleUploadAreaClick}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            handleUploadAreaClick();
          }
        }}
        aria-label="Upload media files"
      >
        <div className={styles.uploadIcon}>
          <Add24Regular />
        </div>
        <Text className={styles.uploadText}>Drop files here or click to upload</Text>
        <Text className={styles.uploadHint}>Supports video, audio, and image files</Text>
      </div>

      <div className={styles.libraryContent}>
        {isUploading && (
          <div className={styles.loadingContainer}>
            <Spinner size="large" label="Processing files..." />
          </div>
        )}

        {!isUploading && clips.length === 0 && (
          <div className={styles.emptyState}>
            <Text>No clips in library</Text>
            <Text size={200}>Upload files to get started</Text>
          </div>
        )}

        {!isUploading && clips.length > 0 && (
          <div className={styles.clipGrid}>
            {clips.map((clip) => (
              <Card
                key={clip.id}
                className={styles.clipCard}
                draggable
                onDragStart={(e) => handleClipDragStart(e, clip)}
                onDragEnd={handleClipDragEnd}
              >
                <Button
                  className={styles.removeButton}
                  appearance="subtle"
                  size="small"
                  icon={<Dismiss24Regular />}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleRemoveClip(clip.id);
                  }}
                  aria-label="Remove clip"
                />
                <CardPreview className={styles.clipPreview}>
                  {getClipIcon(clip.type)}
                </CardPreview>
                <CardHeader
                  className={styles.clipHeader}
                  header={
                    <div>
                      <Text className={styles.clipName} title={clip.name}>
                        {clip.name}
                      </Text>
                      <Text className={styles.clipType}>{clip.type}</Text>
                    </div>
                  }
                />
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
