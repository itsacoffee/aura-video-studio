import { makeStyles, tokens, Text, Spinner, Tab, TabList } from '@fluentui/react-components';
import React, { useState, useRef, forwardRef, useImperativeHandle } from 'react';
import {
  generateVideoThumbnails,
  generateWaveform,
  getMediaDuration,
  isSupportedMediaType,
  getMediaPreview,
} from '../../utils/mediaProcessing';
import { FileSystemBrowser } from '../MediaLibrary/FileSystemBrowser';
import { MetadataPanel } from '../MediaLibrary/MetadataPanel';
import { ProjectBin, MediaAsset } from '../MediaLibrary/ProjectBin';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalL,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase500,
    marginBottom: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground1,
  },
  subtitle: {
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
  },
  tabContainer: {
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  dualPane: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden',
  },
  leftPane: {
    flex: 1,
    minWidth: '240px', // Increased from 200px to prevent file browser clipping
    maxWidth: '50%', // Prevent left pane from taking too much space
    borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  rightPane: {
    flex: 1,
    minWidth: '240px', // Increased from 200px to prevent project bin clipping
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  singlePane: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalM,
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
});

export interface MediaClip {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'image';
  file: File;
  duration?: number;
  fileSize?: number;
  resolution?: string;
  thumbnails?: Array<{ dataUrl: string; timestamp: number }>;
  waveform?: { peaks: number[]; duration: number };
  preview?: string;
  uploadProgress?: number;
  filePath?: string;
  frameRate?: number;
  codec?: string;
  creationDate?: string;
}

interface MediaLibraryPanelProps {
  onClipDragStart?: (clip: MediaClip) => void;
  onClipDragEnd?: () => void;
}

export interface MediaLibraryPanelRef {
  openFilePicker: () => void;
}

export const MediaLibraryPanel = forwardRef<MediaLibraryPanelRef, MediaLibraryPanelProps>(
  ({ onClipDragStart, onClipDragEnd }, ref) => {
    const styles = useStyles();
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [clips, setClips] = useState<MediaClip[]>([]);
    const [isUploading, setIsUploading] = useState(false);
    const [selectedTab, setSelectedTab] = useState<'dual' | 'project'>('dual');
    const [selectedAsset, setSelectedAsset] = useState<MediaClip | null>(null);

    // Expose methods to parent component
    useImperativeHandle(ref, () => ({
      openFilePicker: () => {
        fileInputRef.current?.click();
      },
    }));

    const getClipType = (file: File): 'video' | 'audio' | 'image' => {
      if (file.type.startsWith('video/')) return 'video';
      if (file.type.startsWith('audio/')) return 'audio';
      if (file.type.startsWith('image/')) return 'image';
      return 'video'; // default
    };

    // Convert MediaClip to MediaAsset for ProjectBin
    const assetsFromClips: MediaAsset[] = clips.map((clip) => ({
      id: clip.id,
      name: clip.name,
      type: clip.type,
      file: clip.file,
      preview: clip.preview,
      duration: clip.duration,
      fileSize: clip.fileSize,
      filePath: clip.filePath,
      resolution: clip.resolution,
      frameRate: clip.frameRate,
      codec: clip.codec,
      creationDate: clip.creationDate,
    }));

    const processMediaFile = async (
      file: File,
      clipId: string,
      type: 'video' | 'audio' | 'image'
    ) => {
      try {
        // Update progress
        setClips((prev) =>
          prev.map((clip) => (clip.id === clipId ? { ...clip, uploadProgress: 10 } : clip))
        );

        // Get duration for audio/video files
        let duration: number | undefined;
        if (type === 'video' || type === 'audio') {
          try {
            duration = await getMediaDuration(file);
          } catch (error) {
            console.warn('Failed to get media duration:', error);
          }
        }

        setClips((prev) =>
          prev.map((clip) =>
            clip.id === clipId ? { ...clip, uploadProgress: 30, duration } : clip
          )
        );

        // Generate thumbnails for video files
        let thumbnails: Array<{ dataUrl: string; timestamp: number }> | undefined;
        if (type === 'video') {
          try {
            thumbnails = await generateVideoThumbnails(file, 5);
          } catch (error) {
            console.warn('Failed to generate thumbnails:', error);
          }
        }

        setClips((prev) =>
          prev.map((clip) =>
            clip.id === clipId ? { ...clip, uploadProgress: 60, thumbnails } : clip
          )
        );

        // Generate waveform for audio/video files
        let waveform: { peaks: number[]; duration: number } | undefined;
        if (type === 'video' || type === 'audio') {
          try {
            waveform = await generateWaveform(file, 100);
          } catch (error) {
            console.warn('Failed to generate waveform:', error);
          }
        }

        setClips((prev) =>
          prev.map((clip) =>
            clip.id === clipId ? { ...clip, uploadProgress: 80, waveform } : clip
          )
        );

        // Get preview image
        let preview: string | undefined;
        try {
          const previewUrl = await getMediaPreview(file, type);
          preview = previewUrl || undefined;
        } catch (error) {
          console.warn('Failed to generate preview:', error);
        }

        // Mark as complete
        setClips((prev) =>
          prev.map((clip) =>
            clip.id === clipId ? { ...clip, uploadProgress: 100, preview } : clip
          )
        );

        // Remove progress after a short delay
        setTimeout(() => {
          setClips((prev) =>
            prev.map((clip) => (clip.id === clipId ? { ...clip, uploadProgress: undefined } : clip))
          );
        }, 1000);
      } catch (error) {
        console.error('Error processing media file:', error);
        const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
        console.warn(`Failed to process ${file.name}: ${errorMessage}`);
        // Mark the clip with an error state rather than removing it
        setClips((prev) =>
          prev.map((clip) =>
            clip.id === clipId ? { ...clip, uploadProgress: undefined, preview: undefined } : clip
          )
        );
      }
    };

    const handleFiles = async (files: FileList | null) => {
      if (!files || files.length === 0) return;

      setIsUploading(true);

      const validFiles = Array.from(files).filter((file) => {
        if (!isSupportedMediaType(file)) {
          alert(`Unsupported file type: ${file.name}`);
          return false;
        }
        return true;
      });

      if (validFiles.length === 0) {
        setIsUploading(false);
        return;
      }

      const newClips: MediaClip[] = validFiles.map((file) => ({
        id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        name: file.name,
        type: getClipType(file),
        file,
        fileSize: file.size,
        uploadProgress: 0,
      }));

      // Deduplicate clips based on (name, size, lastModified) before adding
      setClips((prev) => {
        const allClips = [...prev, ...newClips];
        return allClips.filter(
          (clip, index, self) =>
            index ===
            self.findIndex(
              (c) =>
                c.name === clip.name &&
                c.file.size === clip.file.size &&
                c.file.lastModified === clip.file.lastModified
            )
        );
      });

      // Process each file
      for (const clip of newClips) {
        processMediaFile(clip.file, clip.id, clip.type);
      }

      setIsUploading(false);
    };

    const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      handleFiles(e.target.files);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    };

    const handleUploadAreaClick = () => {
      fileInputRef.current?.click();
    };

    const handleClipDragStart = (asset: MediaAsset) => {
      // Find the original clip
      const clip = clips.find((c) => c.id === asset.id);
      if (clip) {
        onClipDragStart?.(clip);
      }
    };

    const handleClipDragEnd = () => {
      // Always reset drag state, even if drop was canceled
      onClipDragEnd?.();
    };

    const handleRemoveClip = (clipId: string) => {
      setClips((prev) => prev.filter((c) => c.id !== clipId));
      if (selectedAsset?.id === clipId) {
        setSelectedAsset(null);
      }
    };

    const handleAssetSelect = (asset: MediaAsset) => {
      const clip = clips.find((c) => c.id === asset.id);
      if (clip) {
        setSelectedAsset(clip);
      }
    };

    return (
      <div className={styles.container}>
        <div className={styles.header}>
          <Text className={styles.title}>Media Library</Text>
          <Text className={styles.subtitle}>Browse and manage your media assets</Text>
        </div>

        <div className={styles.tabContainer}>
          <TabList
            selectedValue={selectedTab}
            onTabSelect={(_, data) => setSelectedTab(data.value as 'dual' | 'project')}
          >
            <Tab value="dual">Dual View</Tab>
            <Tab value="project">Project Only</Tab>
          </TabList>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept="video/*,audio/*,image/*"
          multiple
          style={{ display: 'none' }}
          onChange={handleFileInputChange}
        />

        {isUploading && (
          <div className={styles.loadingContainer}>
            <Spinner size="large" label="Processing files..." />
          </div>
        )}

        {!isUploading && selectedTab === 'dual' && (
          <div className={styles.dualPane}>
            <div className={styles.leftPane}>
              <FileSystemBrowser
                onFileSelect={() => {
                  /* File selection handled by parent component */
                }}
                onFileDragStart={() => {
                  /* Drag start handled by parent component */
                }}
              />
            </div>
            <div className={styles.rightPane}>
              <ProjectBin
                assets={assetsFromClips}
                onAddAssets={handleUploadAreaClick}
                onRemoveAsset={handleRemoveClip}
                onAssetDragStart={handleClipDragStart}
                onAssetDragEnd={handleClipDragEnd}
                onAssetSelect={handleAssetSelect}
              />
              {selectedAsset && (
                <MetadataPanel
                  filePath={selectedAsset.filePath}
                  fileSize={selectedAsset.fileSize}
                  resolution={selectedAsset.resolution}
                  duration={selectedAsset.duration}
                  frameRate={selectedAsset.frameRate}
                  codec={selectedAsset.codec}
                  creationDate={selectedAsset.creationDate}
                />
              )}
            </div>
          </div>
        )}

        {!isUploading && selectedTab === 'project' && (
          <div className={styles.singlePane}>
            <ProjectBin
              assets={assetsFromClips}
              onAddAssets={handleUploadAreaClick}
              onRemoveAsset={handleRemoveClip}
              onAssetDragStart={handleClipDragStart}
              onAssetDragEnd={handleClipDragEnd}
              onAssetSelect={handleAssetSelect}
            />
            {selectedAsset && (
              <MetadataPanel
                filePath={selectedAsset.filePath}
                fileSize={selectedAsset.fileSize}
                resolution={selectedAsset.resolution}
                duration={selectedAsset.duration}
                frameRate={selectedAsset.frameRate}
                codec={selectedAsset.codec}
                creationDate={selectedAsset.creationDate}
              />
            )}
          </div>
        )}
      </div>
    );
  }
);

MediaLibraryPanel.displayName = 'MediaLibraryPanel';
