import {
  makeStyles,
  tokens,
  Dialog,
  DialogSurface,
  DialogTitle,
  DialogBody,
  DialogActions,
  DialogContent,
  Button,
  Card,
  Body1,
  Caption1,
  Badge,
  Divider,
  TabList,
  Tab,
} from '@fluentui/react-components';
import {
  VideoRegular,
  MusicNote2Regular,
  DismissRegular,
  FolderOpenRegular,
} from '@fluentui/react-icons';
import { useState } from 'react';
import type { FileMetadata } from './FileContext';

const useStyles = makeStyles({
  dialogSurface: {
    width: '800px',
    maxWidth: '90vw',
    maxHeight: '80vh',
  },
  tabContent: {
    marginTop: tokens.spacingVerticalM,
    maxHeight: '400px',
    overflowY: 'auto',
  },
  fileList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  fileCard: {
    padding: tokens.spacingVerticalM,
    cursor: 'pointer',
    transition: 'all 0.2s ease',
  },
  fileCardSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  fileInfo: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
  },
  icon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  fileDetails: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
  },
  metadata: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  emptyState: {
    textAlign: 'center',
    padding: tokens.spacingVerticalXXL,
    color: tokens.colorNeutralForeground3,
  },
});

interface FileSelectorProps {
  open: boolean;
  onClose: () => void;
  onSelect: (file: FileMetadata) => void;
  onBrowse: () => void;
}

export function FileSelector({ open, onClose, onSelect, onBrowse }: FileSelectorProps) {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<'recent' | 'generated'>('recent');
  const [selectedFile, setSelectedFile] = useState<FileMetadata | null>(null);

  // Mock data for demonstration
  const recentFiles: FileMetadata[] = [
    {
      name: 'Marketing_Video_Q4.mp4',
      path: '/Users/username/Videos/Marketing_Video_Q4.mp4',
      type: 'video',
      duration: 125,
      size: 85 * 1024 * 1024,
      resolution: { width: 1920, height: 1080 },
      codec: 'H.264',
      bitrate: 8000000,
      fps: 30,
      audioCodec: 'AAC',
      sampleRate: 48000,
    },
    {
      name: 'Product_Demo_v2.mov',
      path: '/Users/username/Videos/Product_Demo_v2.mov',
      type: 'video',
      duration: 95,
      size: 320 * 1024 * 1024,
      resolution: { width: 1920, height: 1080 },
      codec: 'ProRes',
      bitrate: 28000000,
      fps: 30,
      audioCodec: 'PCM',
      sampleRate: 48000,
    },
    {
      name: 'Interview_Audio.mp3',
      path: '/Users/username/Audio/Interview_Audio.mp3',
      type: 'audio',
      duration: 1845,
      size: 42 * 1024 * 1024,
      codec: 'MP3',
      bitrate: 192000,
      sampleRate: 44100,
    },
  ];

  const generatedFiles: FileMetadata[] = [
    {
      name: 'Aura_Generated_2024_01_15.mp4',
      path: '/Users/username/.aura/outputs/Aura_Generated_2024_01_15.mp4',
      type: 'video',
      duration: 45,
      size: 35 * 1024 * 1024,
      resolution: { width: 1920, height: 1080 },
      codec: 'H.264',
      bitrate: 6000000,
      fps: 30,
      audioCodec: 'AAC',
      sampleRate: 48000,
    },
    {
      name: 'Aura_Generated_2024_01_14.mp4',
      path: '/Users/username/.aura/outputs/Aura_Generated_2024_01_14.mp4',
      type: 'video',
      duration: 60,
      size: 48 * 1024 * 1024,
      resolution: { width: 1920, height: 1080 },
      codec: 'H.264',
      bitrate: 6000000,
      fps: 30,
      audioCodec: 'AAC',
      sampleRate: 48000,
    },
  ];

  const files = selectedTab === 'recent' ? recentFiles : generatedFiles;

  const formatDuration = (seconds: number): string => {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}:${secs.toString().padStart(2, '0')}`;
  };

  const formatFileSize = (bytes: number): string => {
    const mb = bytes / (1024 * 1024);
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(2)} GB`;
    }
    return `${mb.toFixed(2)} MB`;
  };

  const handleSelect = () => {
    if (selectedFile) {
      onSelect(selectedFile);
      onClose();
    }
  };

  const handleBrowse = () => {
    onBrowse();
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface className={styles.dialogSurface}>
        <DialogBody>
          <DialogTitle
            action={
              <Button
                appearance="subtle"
                aria-label="close"
                icon={<DismissRegular />}
                onClick={onClose}
              />
            }
          >
            Select File to Re-encode
          </DialogTitle>
          <DialogContent>
            <TabList
              selectedValue={selectedTab}
              onTabSelect={(_, data) => setSelectedTab(data.value as 'recent' | 'generated')}
            >
              <Tab value="recent">Recent Projects</Tab>
              <Tab value="generated">Generated Videos</Tab>
            </TabList>

            <div className={styles.tabContent}>
              {files.length === 0 ? (
                <div className={styles.emptyState}>
                  <Body1>No files found</Body1>
                  <Caption1>Browse your computer to select a file</Caption1>
                </div>
              ) : (
                <div className={styles.fileList}>
                  {files.map((file, index) => (
                    <Card
                      key={index}
                      className={`${styles.fileCard} ${selectedFile === file ? styles.fileCardSelected : ''}`}
                      onClick={() => setSelectedFile(file)}
                    >
                      <div className={styles.fileInfo}>
                        {file.type === 'video' ? (
                          <VideoRegular className={styles.icon} />
                        ) : (
                          <MusicNote2Regular className={styles.icon} />
                        )}

                        <div className={styles.fileDetails}>
                          <Body1>{file.name}</Body1>
                          <div className={styles.metadata}>
                            <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                              {formatDuration(file.duration)}
                            </Caption1>
                            <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>•</Caption1>
                            <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                              {formatFileSize(file.size)}
                            </Caption1>
                            {file.resolution && (
                              <>
                                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                                  •
                                </Caption1>
                                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                                  {file.resolution.width}×{file.resolution.height}
                                </Caption1>
                              </>
                            )}
                            {file.codec && (
                              <>
                                <Caption1 style={{ color: tokens.colorNeutralForeground3 }}>
                                  •
                                </Caption1>
                                <Badge size="small">{file.codec}</Badge>
                              </>
                            )}
                          </div>
                        </div>
                      </div>
                    </Card>
                  ))}
                </div>
              )}
            </div>

            <Divider style={{ marginTop: tokens.spacingVerticalM }} />

            <Button
              appearance="secondary"
              icon={<FolderOpenRegular />}
              onClick={handleBrowse}
              style={{ marginTop: tokens.spacingVerticalM, width: '100%' }}
            >
              Browse Computer
            </Button>
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button appearance="primary" onClick={handleSelect} disabled={!selectedFile}>
              Select File
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
