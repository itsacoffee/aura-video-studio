/**
 * OpenCut Native Editor
 *
 * A native React implementation of the OpenCut video editor that runs
 * directly in Aura.Web without requiring a separate server process.
 * This replaces the iframe-based approach with integrated components.
 */

import { useState, useCallback, useRef, useEffect } from 'react';
import {
  makeStyles,
  tokens,
  Button,
  Text,
  Tooltip,
  Slider,
  Badge,
} from '@fluentui/react-components';
import {
  Play24Regular,
  Pause24Regular,
  Previous24Regular,
  Next24Regular,
  Speaker224Regular,
  SpeakerMute24Regular,
  FullScreenMaximize24Regular,
  Add24Regular,
  Video24Regular,
  MusicNote224Regular,
  Image24Regular,
  TextT24Regular,
  Delete24Regular,
  Cut24Regular,
  Copy24Regular,
} from '@fluentui/react-icons';
import { useOpenCutPlaybackStore } from '../../stores/opencutPlayback';
import { useOpenCutMediaStore } from '../../stores/opencutMedia';
import { useOpenCutProjectStore } from '../../stores/opencutProject';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    width: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  mainContent: {
    display: 'flex',
    flex: 1,
    minHeight: 0,
    overflow: 'hidden',
  },
  leftPanel: {
    width: '280px',
    minWidth: '200px',
    maxWidth: '400px',
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  centerPanel: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    minWidth: 0,
  },
  rightPanel: {
    width: '280px',
    minWidth: '200px',
    maxWidth: '400px',
    borderLeft: `1px solid ${tokens.colorNeutralStroke2}`,
    display: 'flex',
    flexDirection: 'column',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  previewContainer: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingHorizontalL,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  previewCanvas: {
    maxWidth: '100%',
    maxHeight: '100%',
    aspectRatio: '16 / 9',
    backgroundColor: '#000000',
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  },
  previewPlaceholder: {
    color: tokens.colorNeutralForeground4,
    textAlign: 'center',
  },
  playbackControls: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    gap: tokens.spacingHorizontalM,
    padding: tokens.spacingVerticalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  timeDisplay: {
    fontFamily: 'monospace',
    fontSize: tokens.fontSizeBase300,
    minWidth: '120px',
    textAlign: 'center',
  },
  volumeControl: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    marginLeft: tokens.spacingHorizontalL,
  },
  volumeSlider: {
    width: '80px',
  },
  timelineContainer: {
    height: '200px',
    minHeight: '150px',
    maxHeight: '400px',
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground2,
    display: 'flex',
    flexDirection: 'column',
  },
  timelineHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  timelineContent: {
    flex: 1,
    overflow: 'auto',
    position: 'relative',
  },
  timelineRuler: {
    height: '24px',
    backgroundColor: tokens.colorNeutralBackground3,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    position: 'relative',
  },
  timelineTracks: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    padding: tokens.spacingVerticalXS,
  },
  timelineTrack: {
    height: '48px',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    alignItems: 'center',
    position: 'relative',
  },
  trackLabel: {
    width: '100px',
    padding: `0 ${tokens.spacingHorizontalS}`,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    borderRight: `1px solid ${tokens.colorNeutralStroke3}`,
    height: '100%',
  },
  trackContent: {
    flex: 1,
    position: 'relative',
    height: '100%',
  },
  panelHeader: {
    padding: tokens.spacingHorizontalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  panelContent: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  mediaGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: tokens.spacingHorizontalS,
  },
  mediaItem: {
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'pointer',
    border: `1px solid transparent`,
    transition: 'border-color 0.2s, background-color 0.2s',
    ':hover': {
      border: `1px solid ${tokens.colorBrandStroke1}`,
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  mediaItemSelected: {
    border: `1px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  mediaItemImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
    borderRadius: tokens.borderRadiusSmall,
  },
  dropZone: {
    border: `2px dashed ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalL,
    textAlign: 'center',
    marginTop: tokens.spacingVerticalM,
    transition: 'border-color 0.2s, background-color 0.2s',
  },
  dropZoneActive: {
    border: `2px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXL,
    color: tokens.colorNeutralForeground4,
    textAlign: 'center',
    gap: tokens.spacingVerticalM,
  },
  playhead: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    width: '2px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    zIndex: 10,
  },
  playheadHandle: {
    position: 'absolute',
    top: '-4px',
    left: '-6px',
    width: '14px',
    height: '14px',
    backgroundColor: tokens.colorPaletteRedBackground3,
    borderRadius: '50%',
    cursor: 'ew-resize',
  },
});

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const frames = Math.floor((seconds % 1) * 30);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}:${frames.toString().padStart(2, '0')}`;
}

export function OpenCutEditor() {
  const styles = useStyles();
  const [isDraggingMedia, setIsDraggingMedia] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Stores
  const playbackStore = useOpenCutPlaybackStore();
  const mediaStore = useOpenCutMediaStore();
  const projectStore = useOpenCutProjectStore();

  // Initialize project on mount
  useEffect(() => {
    if (!projectStore.activeProject) {
      projectStore.createProject('Untitled Project');
    }
  }, [projectStore]);

  // Handle file import
  const handleFileSelect = useCallback(
    async (files: FileList | null) => {
      if (!files) return;

      for (let i = 0; i < files.length; i++) {
        const file = files[i];
        await mediaStore.addMediaFile(file);
      }
    },
    [mediaStore]
  );

  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      setIsDraggingMedia(false);

      if (e.dataTransfer.files.length > 0) {
        await handleFileSelect(e.dataTransfer.files);
      }
    },
    [handleFileSelect]
  );

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDraggingMedia(true);
  }, []);

  const handleDragLeave = useCallback(() => {
    setIsDraggingMedia(false);
  }, []);

  return (
    <div className={styles.root}>
      {/* Main Content Area */}
      <div className={styles.mainContent}>
        {/* Left Panel - Media Library */}
        <div className={styles.leftPanel}>
          <div className={styles.panelHeader}>
            <Text weight="semibold" size={400}>
              Media
            </Text>
            <Button
              appearance="subtle"
              icon={<Add24Regular />}
              size="small"
              onClick={() => fileInputRef.current?.click()}
            >
              Import
            </Button>
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept="video/*,audio/*,image/*"
              style={{ display: 'none' }}
              onChange={(e) => handleFileSelect(e.target.files)}
            />
          </div>
          <div
            className={styles.panelContent}
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
          >
            {mediaStore.mediaFiles.length === 0 ? (
              <div className={styles.emptyState}>
                <Video24Regular style={{ fontSize: '48px' }} />
                <Text>No media files</Text>
                <Text size={200}>Drop files here or click Import</Text>
              </div>
            ) : (
              <div className={styles.mediaGrid}>
                {mediaStore.mediaFiles.map((file) => (
                  <Tooltip key={file.id} content={file.name} relationship="label">
                    <div
                      className={`${styles.mediaItem} ${
                        mediaStore.selectedMediaId === file.id ? styles.mediaItemSelected : ''
                      }`}
                      onClick={() => mediaStore.selectMedia(file.id)}
                    >
                      {file.thumbnailUrl ? (
                        <img
                          src={file.thumbnailUrl}
                          alt={file.name}
                          className={styles.mediaItemImage}
                        />
                      ) : file.type === 'video' ? (
                        <Video24Regular />
                      ) : file.type === 'audio' ? (
                        <MusicNote224Regular />
                      ) : (
                        <Image24Regular />
                      )}
                    </div>
                  </Tooltip>
                ))}
              </div>
            )}

            <div className={`${styles.dropZone} ${isDraggingMedia ? styles.dropZoneActive : ''}`}>
              <Text size={200}>Drop media files here</Text>
            </div>
          </div>
        </div>

        {/* Center Panel - Preview */}
        <div className={styles.centerPanel}>
          <div className={styles.previewContainer}>
            <div className={styles.previewCanvas}>
              <div className={styles.previewPlaceholder}>
                <Video24Regular style={{ fontSize: '64px', opacity: 0.5 }} />
                <Text size={300} style={{ display: 'block', marginTop: tokens.spacingVerticalM }}>
                  Add media to the timeline to preview
                </Text>
              </div>
            </div>
          </div>

          {/* Playback Controls */}
          <div className={styles.playbackControls}>
            <Tooltip content="Previous frame" relationship="label">
              <Button
                appearance="subtle"
                icon={<Previous24Regular />}
                onClick={() => {
                  const fps = projectStore.activeProject?.fps || 30;
                  playbackStore.skipBackward(1 / fps);
                }}
              />
            </Tooltip>

            <Tooltip content={playbackStore.isPlaying ? 'Pause' : 'Play'} relationship="label">
              <Button
                appearance="primary"
                icon={playbackStore.isPlaying ? <Pause24Regular /> : <Play24Regular />}
                onClick={playbackStore.toggle}
              />
            </Tooltip>

            <Tooltip content="Next frame" relationship="label">
              <Button
                appearance="subtle"
                icon={<Next24Regular />}
                onClick={() => {
                  const fps = projectStore.activeProject?.fps || 30;
                  playbackStore.skipForward(1 / fps);
                }}
              />
            </Tooltip>

            <div className={styles.timeDisplay}>
              <Text>{formatTime(playbackStore.currentTime)}</Text>
              <Text color="foreground2"> / </Text>
              <Text color="foreground2">{formatTime(playbackStore.duration)}</Text>
            </div>

            <div className={styles.volumeControl}>
              <Tooltip content={playbackStore.muted ? 'Unmute' : 'Mute'} relationship="label">
                <Button
                  appearance="subtle"
                  icon={
                    playbackStore.muted || playbackStore.volume === 0 ? (
                      <SpeakerMute24Regular />
                    ) : (
                      <Speaker224Regular />
                    )
                  }
                  onClick={playbackStore.toggleMute}
                />
              </Tooltip>
              <Slider
                className={styles.volumeSlider}
                min={0}
                max={100}
                value={playbackStore.volume * 100}
                onChange={(_, data) => playbackStore.setVolume(data.value / 100)}
              />
            </div>

            <Tooltip content="Fullscreen" relationship="label">
              <Button appearance="subtle" icon={<FullScreenMaximize24Regular />} />
            </Tooltip>
          </div>
        </div>

        {/* Right Panel - Properties */}
        <div className={styles.rightPanel}>
          <div className={styles.panelHeader}>
            <Text weight="semibold" size={400}>
              Properties
            </Text>
          </div>
          <div className={styles.panelContent}>
            <div className={styles.emptyState}>
              <TextT24Regular style={{ fontSize: '48px' }} />
              <Text>No selection</Text>
              <Text size={200}>Select an element to edit its properties</Text>
            </div>
          </div>
        </div>
      </div>

      {/* Timeline */}
      <div className={styles.timelineContainer}>
        <div className={styles.timelineHeader}>
          <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalM }}>
            <Text weight="semibold">Timeline</Text>
            <Badge appearance="outline" size="small">
              {projectStore.activeProject?.fps || 30} fps
            </Badge>
          </div>
          <div style={{ display: 'flex', gap: tokens.spacingHorizontalS }}>
            <Tooltip content="Split at playhead" relationship="label">
              <Button appearance="subtle" icon={<Cut24Regular />} size="small" />
            </Tooltip>
            <Tooltip content="Duplicate" relationship="label">
              <Button appearance="subtle" icon={<Copy24Regular />} size="small" />
            </Tooltip>
            <Tooltip content="Delete" relationship="label">
              <Button appearance="subtle" icon={<Delete24Regular />} size="small" />
            </Tooltip>
          </div>
        </div>
        <div className={styles.timelineContent}>
          <div className={styles.timelineRuler}>{/* Time markers would go here */}</div>
          <div className={styles.timelineTracks}>
            {/* Video Track */}
            <div className={styles.timelineTrack}>
              <div className={styles.trackLabel}>
                <Video24Regular style={{ fontSize: '14px' }} />
                <span>Video</span>
              </div>
              <div className={styles.trackContent}>{/* Clips would render here */}</div>
            </div>

            {/* Audio Track */}
            <div className={styles.timelineTrack}>
              <div className={styles.trackLabel}>
                <MusicNote224Regular style={{ fontSize: '14px' }} />
                <span>Audio</span>
              </div>
              <div className={styles.trackContent}>{/* Audio clips would render here */}</div>
            </div>

            {/* Text Track */}
            <div className={styles.timelineTrack}>
              <div className={styles.trackLabel}>
                <TextT24Regular style={{ fontSize: '14px' }} />
                <span>Text</span>
              </div>
              <div className={styles.trackContent}>{/* Text clips would render here */}</div>
            </div>

            {/* Playhead */}
            <div
              className={styles.playhead}
              style={{
                left: `${100 + (playbackStore.duration > 0 ? (playbackStore.currentTime / playbackStore.duration) * 80 : 0)}%`,
              }}
            >
              <div className={styles.playheadHandle} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default OpenCutEditor;
