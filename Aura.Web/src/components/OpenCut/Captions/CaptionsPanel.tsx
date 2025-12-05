/**
 * CaptionsPanel Component
 *
 * Panel for managing captions and subtitles in the OpenCut editor.
 * Provides caption track management, caption list, styling options,
 * import/export, and integration with auto-captioning.
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  Input,
  Dropdown,
  Option,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Subtitles24Regular,
  Add24Regular,
  Delete24Regular,
  Edit24Regular,
  Eye24Regular,
  EyeOff24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Mic24Regular,
  MoreHorizontal24Regular,
  Settings24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useRef } from 'react';
import type { FC, ChangeEvent } from 'react';
import {
  useOpenCutCaptionsStore,
  type Caption,
  type CaptionStyle,
} from '../../../stores/opencutCaptions';
import { useOpenCutMediaStore } from '../../../stores/opencutMedia';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { EmptyState } from '../EmptyState';
import { AutoCaptionDialog } from './AutoCaptionDialog';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground2,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '48px',
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    gap: tokens.spacingHorizontalS,
  },
  trackSelector: {
    flex: 1,
    maxWidth: '200px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    display: 'flex',
    flexDirection: 'column',
  },
  captionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingHorizontalM,
  },
  captionItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusSmall,
    border: `1px solid transparent`,
    cursor: 'pointer',
    transition: 'all 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground4,
      border: `1px solid ${tokens.colorBrandStroke1}`,
    },
  },
  captionItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  captionItemActive: {
    border: `1px solid ${tokens.colorPaletteGreenBorder1}`,
    boxShadow: `0 0 0 1px ${tokens.colorPaletteGreenBorder1}`,
  },
  captionHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  captionTime: {
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground3,
  },
  captionText: {
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
  },
  captionActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
    opacity: 0,
    transition: 'opacity 150ms ease-out',
  },
  captionActionsVisible: {
    opacity: 1,
  },
  actionButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
  addCaptionForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingHorizontalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  formRow: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  timeInput: {
    width: '80px',
  },
  textInput: {
    flex: 1,
  },
  styleSection: {
    padding: tokens.spacingHorizontalM,
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  styleRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalM,
  },
  colorInput: {
    width: '32px',
    height: '32px',
    padding: 0,
    border: 'none',
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
  },
  hiddenInput: {
    display: 'none',
  },
});

/**
 * Format seconds to display format MM:SS.ms
 */
function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 100);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
}

/**
 * Parse time string to seconds
 */
function parseTime(timeStr: string): number {
  // eslint-disable-next-line security/detect-unsafe-regex
  const match = timeStr.match(/^(\d{1,2}):(\d{2})(?:\.(\d{1,2}))?$/);
  if (!match) return 0;

  const mins = parseInt(match[1], 10);
  const secs = parseInt(match[2], 10);
  const ms = match[3] ? parseInt(match[3].padEnd(2, '0'), 10) : 0;

  return mins * 60 + secs + ms / 100;
}

export interface CaptionsPanelProps {
  /** Optional class name */
  className?: string;
}

export const CaptionsPanel: FC<CaptionsPanelProps> = ({ className }) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();
  const mediaStore = useOpenCutMediaStore();
  const fileInputRef = useRef<HTMLInputElement>(null);

  // State
  const [showAddForm, setShowAddForm] = useState(false);
  const [showStylePanel, setShowStylePanel] = useState(false);
  const [showAutoCaptionDialog, setShowAutoCaptionDialog] = useState(false);
  const [newCaptionStart, setNewCaptionStart] = useState('');
  const [newCaptionEnd, setNewCaptionEnd] = useState('');
  const [newCaptionText, setNewCaptionText] = useState('');
  const [editingCaptionId, setEditingCaptionId] = useState<string | null>(null);
  const [editText, setEditText] = useState('');

  const { tracks, selectedTrackId, selectedCaptionId } = captionsStore;
  const currentTime = playbackStore.currentTime;
  const selectedTrack = tracks.find((t) => t.id === selectedTrackId);
  const trackCaptions = selectedTrackId ? captionsStore.getCaptionsForTrack(selectedTrackId) : [];

  // Get audio URL from selected media
  const selectedMedia = mediaStore.selectedMediaId
    ? mediaStore.getMediaById(mediaStore.selectedMediaId)
    : null;
  const audioUrl = selectedMedia?.url;

  const handleAddTrack = useCallback(() => {
    captionsStore.addTrack('New Track', 'en');
  }, [captionsStore]);

  const handleSelectTrack = useCallback(
    (trackId: string) => {
      captionsStore.selectTrack(trackId);
    },
    [captionsStore]
  );

  const handleToggleTrackVisibility = useCallback(
    (trackId: string) => {
      const track = tracks.find((t) => t.id === trackId);
      if (track) {
        captionsStore.updateTrack(trackId, { visible: !track.visible });
      }
    },
    [tracks, captionsStore]
  );

  const handleDeleteTrack = useCallback(
    (trackId: string) => {
      captionsStore.removeTrack(trackId);
    },
    [captionsStore]
  );

  const handleSelectCaption = useCallback(
    (captionId: string) => {
      captionsStore.selectCaption(captionId);
      const caption = trackCaptions.find((c) => c.id === captionId);
      if (caption) {
        playbackStore.seek(caption.startTime);
      }
    },
    [captionsStore, trackCaptions, playbackStore]
  );

  const handleStartEditCaption = useCallback((caption: Caption) => {
    setEditingCaptionId(caption.id);
    setEditText(caption.text);
  }, []);

  const handleSaveEditCaption = useCallback(() => {
    if (editingCaptionId) {
      captionsStore.updateCaption(editingCaptionId, { text: editText });
      setEditingCaptionId(null);
    }
  }, [editingCaptionId, editText, captionsStore]);

  const handleDeleteCaption = useCallback(
    (captionId: string) => {
      captionsStore.removeCaption(captionId);
    },
    [captionsStore]
  );

  const handleAddCaption = useCallback(() => {
    if (!selectedTrackId || !newCaptionText.trim()) return;

    const startTime = parseTime(newCaptionStart) || currentTime;
    const endTime = parseTime(newCaptionEnd) || startTime + 3;

    captionsStore.addCaption(selectedTrackId, startTime, endTime, newCaptionText.trim());

    setNewCaptionStart('');
    setNewCaptionEnd('');
    setNewCaptionText('');
    setShowAddForm(false);
  }, [selectedTrackId, newCaptionStart, newCaptionEnd, newCaptionText, currentTime, captionsStore]);

  const handleExportSrt = useCallback(() => {
    if (!selectedTrackId) return;

    const srtContent = captionsStore.exportSrt(selectedTrackId);
    const blob = new Blob([srtContent], { type: 'text/srt' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${selectedTrack?.name ?? 'captions'}.srt`;
    a.click();
    URL.revokeObjectURL(url);
  }, [selectedTrackId, selectedTrack, captionsStore]);

  const handleExportVtt = useCallback(() => {
    if (!selectedTrackId) return;

    const vttContent = captionsStore.exportVtt(selectedTrackId);
    const blob = new Blob([vttContent], { type: 'text/vtt' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${selectedTrack?.name ?? 'captions'}.vtt`;
    a.click();
    URL.revokeObjectURL(url);
  }, [selectedTrackId, selectedTrack, captionsStore]);

  const handleImportClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  const handleFileImport = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (!file || !selectedTrackId) return;

      const reader = new FileReader();
      reader.onload = (event) => {
        const content = event.target?.result as string;
        if (file.name.endsWith('.srt')) {
          captionsStore.importSrt(selectedTrackId, content);
        } else if (file.name.endsWith('.vtt')) {
          captionsStore.importVtt(selectedTrackId, content);
        }
      };
      reader.readAsText(file);

      // Reset input
      e.target.value = '';
    },
    [selectedTrackId, captionsStore]
  );

  const handleUpdateStyle = useCallback(
    (property: keyof CaptionStyle, value: string | number) => {
      if (!selectedTrackId || !selectedTrack) return;

      captionsStore.updateTrack(selectedTrackId, {
        style: { ...selectedTrack.style, [property]: value },
      });
    },
    [selectedTrackId, selectedTrack, captionsStore]
  );

  const isCaptionActive = (caption: Caption): boolean => {
    return currentTime >= caption.startTime && currentTime <= caption.endTime;
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Subtitles24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Captions
          </Text>
        </div>
        <div className={styles.headerActions}>
          <Tooltip content="Auto-generate captions" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Mic24Regular />}
              onClick={() => setShowAutoCaptionDialog(true)}
              disabled={!audioUrl}
            />
          </Tooltip>
          <Tooltip content="Add track" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Add24Regular />}
              onClick={handleAddTrack}
            />
          </Tooltip>
        </div>
      </div>

      {/* Toolbar with track selection */}
      {tracks.length > 0 && (
        <div className={styles.toolbar}>
          <Dropdown
            className={styles.trackSelector}
            value={selectedTrack?.name ?? 'Select track'}
            onOptionSelect={(_, data) => handleSelectTrack(data.optionValue as string)}
            size="small"
          >
            {tracks.map((track) => (
              <Option key={track.id} value={track.id}>
                {track.name}
              </Option>
            ))}
          </Dropdown>

          <div className={styles.headerActions}>
            {selectedTrack && (
              <>
                <Tooltip
                  content={selectedTrack.visible ? 'Hide track' : 'Show track'}
                  relationship="label"
                >
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={selectedTrack.visible ? <Eye24Regular /> : <EyeOff24Regular />}
                    onClick={() => handleToggleTrackVisibility(selectedTrack.id)}
                  />
                </Tooltip>
                <Tooltip content="Track settings" relationship="label">
                  <Button
                    appearance="subtle"
                    size="small"
                    icon={<Settings24Regular />}
                    onClick={() => setShowStylePanel(!showStylePanel)}
                  />
                </Tooltip>
                <Menu>
                  <MenuTrigger disableButtonEnhancement>
                    <Button appearance="subtle" size="small" icon={<MoreHorizontal24Regular />} />
                  </MenuTrigger>
                  <MenuPopover>
                    <MenuList>
                      <MenuItem icon={<ArrowUpload24Regular />} onClick={handleImportClick}>
                        Import SRT/VTT
                      </MenuItem>
                      <MenuItem icon={<ArrowDownload24Regular />} onClick={handleExportSrt}>
                        Export SRT
                      </MenuItem>
                      <MenuItem icon={<ArrowDownload24Regular />} onClick={handleExportVtt}>
                        Export VTT
                      </MenuItem>
                      <MenuDivider />
                      <MenuItem
                        icon={<Delete24Regular />}
                        onClick={() => handleDeleteTrack(selectedTrack.id)}
                      >
                        Delete Track
                      </MenuItem>
                    </MenuList>
                  </MenuPopover>
                </Menu>
              </>
            )}
          </div>
        </div>
      )}

      {/* Style panel */}
      {showStylePanel && selectedTrack && (
        <div className={styles.styleSection}>
          <Text weight="semibold" size={200}>
            Caption Style
          </Text>
          <div className={styles.styleRow}>
            <Text size={200}>Font Size</Text>
            <Input
              type="number"
              value={selectedTrack.style.fontSize.toString()}
              onChange={(_, data) => handleUpdateStyle('fontSize', parseInt(data.value) || 24)}
              size="small"
              style={{ width: '60px' }}
            />
          </div>
          <div className={styles.styleRow}>
            <Text size={200}>Text Color</Text>
            <input
              type="color"
              className={styles.colorInput}
              value={selectedTrack.style.color}
              onChange={(e) => handleUpdateStyle('color', e.target.value)}
            />
          </div>
          <div className={styles.styleRow}>
            <Text size={200}>Background</Text>
            <input
              type="color"
              className={styles.colorInput}
              value={selectedTrack.style.backgroundColor}
              onChange={(e) => handleUpdateStyle('backgroundColor', e.target.value)}
            />
          </div>
        </div>
      )}

      {/* Content */}
      <div className={styles.content}>
        {tracks.length === 0 ? (
          <EmptyState
            icon={<Subtitles24Regular />}
            title="No caption tracks"
            description="Add a caption track to start creating subtitles"
            action={{
              label: 'Add Track',
              onClick: handleAddTrack,
              icon: <Add24Regular />,
            }}
            size="medium"
          />
        ) : !selectedTrackId ? (
          <EmptyState
            icon={<Subtitles24Regular />}
            title="Select a track"
            description="Choose a caption track from the dropdown above"
            size="small"
          />
        ) : trackCaptions.length === 0 ? (
          <EmptyState
            icon={<Subtitles24Regular />}
            title="No captions"
            description="Add captions manually or use auto-generation"
            action={{
              label: 'Add Caption',
              onClick: () => {
                setShowAddForm(true);
                setNewCaptionStart(formatTime(currentTime));
                setNewCaptionEnd(formatTime(currentTime + 3));
              },
              icon: <Add24Regular />,
            }}
            secondaryAction={
              audioUrl
                ? {
                    label: 'Auto-Generate',
                    onClick: () => setShowAutoCaptionDialog(true),
                  }
                : undefined
            }
            size="small"
          />
        ) : (
          <div className={styles.captionList}>
            {trackCaptions.map((caption) => {
              const isSelected = selectedCaptionId === caption.id;
              const isActive = isCaptionActive(caption);
              const isEditing = editingCaptionId === caption.id;

              return (
                <div
                  key={caption.id}
                  className={mergeClasses(
                    styles.captionItem,
                    isSelected && styles.captionItemSelected,
                    isActive && styles.captionItemActive
                  )}
                  onClick={() => handleSelectCaption(caption.id)}
                  role="button"
                  tabIndex={0}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      handleSelectCaption(caption.id);
                    }
                  }}
                >
                  <div className={styles.captionHeader}>
                    <Text className={styles.captionTime}>
                      {formatTime(caption.startTime)} → {formatTime(caption.endTime)}
                    </Text>
                    <div
                      className={mergeClasses(
                        styles.captionActions,
                        (isSelected || isEditing) && styles.captionActionsVisible
                      )}
                    >
                      <Tooltip content="Edit" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Edit24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleStartEditCaption(caption);
                          }}
                        />
                      </Tooltip>
                      <Tooltip content="Delete" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          className={styles.actionButton}
                          icon={<Delete24Regular />}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteCaption(caption.id);
                          }}
                        />
                      </Tooltip>
                    </div>
                  </div>
                  {isEditing ? (
                    <Input
                      value={editText}
                      onChange={(_, data) => setEditText(data.value)}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                          handleSaveEditCaption();
                        } else if (e.key === 'Escape') {
                          setEditingCaptionId(null);
                        }
                      }}
                      onBlur={handleSaveEditCaption}
                      autoFocus
                    />
                  ) : (
                    <Text className={styles.captionText}>{caption.text}</Text>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Add caption form */}
      {showAddForm && selectedTrackId && (
        <div className={styles.addCaptionForm}>
          <div className={styles.formRow}>
            <Text size={200}>Start</Text>
            <Input
              className={styles.timeInput}
              value={newCaptionStart}
              onChange={(_, data) => setNewCaptionStart(data.value)}
              size="small"
              placeholder="00:00.00"
            />
            <Text size={200}>End</Text>
            <Input
              className={styles.timeInput}
              value={newCaptionEnd}
              onChange={(_, data) => setNewCaptionEnd(data.value)}
              size="small"
              placeholder="00:03.00"
            />
          </div>
          <Input
            className={styles.textInput}
            value={newCaptionText}
            onChange={(_, data) => setNewCaptionText(data.value)}
            placeholder="Enter caption text..."
            onKeyDown={(e) => {
              if (e.key === 'Enter' && newCaptionText.trim()) {
                handleAddCaption();
              } else if (e.key === 'Escape') {
                setShowAddForm(false);
              }
            }}
          />
          <div className={styles.formRow}>
            <Button appearance="secondary" onClick={() => setShowAddForm(false)}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              onClick={handleAddCaption}
              disabled={!newCaptionText.trim()}
            >
              Add Caption
            </Button>
          </div>
        </div>
      )}

      {/* Add caption button */}
      {!showAddForm && selectedTrackId && trackCaptions.length > 0 && (
        <div
          style={{
            padding: tokens.spacingHorizontalM,
            borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
          }}
        >
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            onClick={() => {
              setShowAddForm(true);
              setNewCaptionStart(formatTime(currentTime));
              setNewCaptionEnd(formatTime(currentTime + 3));
            }}
          >
            Add Caption
          </Button>
        </div>
      )}

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        className={styles.hiddenInput}
        accept=".srt,.vtt"
        onChange={handleFileImport}
      />

      {/* Auto-caption dialog */}
      {showAutoCaptionDialog && audioUrl && selectedMedia && (
        <AutoCaptionDialog
          open={showAutoCaptionDialog}
          onDismiss={() => setShowAutoCaptionDialog(false)}
          audioUrl={audioUrl}
          clipId={selectedMedia.id}
          clipName={selectedMedia.name}
        />
      )}
    </div>
  );
};

export default CaptionsPanel;
