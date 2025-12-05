/**
 * CaptionsPanel Component
 *
 * Caption management panel for the OpenCut editor.
 * Allows creating caption tracks, adding/editing captions,
 * and importing/exporting SRT/VTT files.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  Button,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Divider,
  Textarea,
  Dialog,
  DialogTrigger,
  DialogSurface,
  DialogBody,
  DialogTitle,
  DialogContent,
  DialogActions,
  Field,
  Select,
  mergeClasses,
} from '@fluentui/react-components';
import {
  ClosedCaption24Regular,
  Add24Regular,
  Delete24Regular,
  Eye24Regular,
  EyeOff24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
  MoreHorizontal24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Copy24Regular,
  Search24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo, useRef } from 'react';
import type { FC, ChangeEvent } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { openCutTokens } from '../../../styles/designTokens';
import type { CaptionTrack } from '../../../types/opencut';
import { EmptyState } from '../EmptyState';

export interface CaptionsPanelProps {
  className?: string;
}

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
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.md}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: '56px',
    gap: openCutTokens.spacing.sm,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
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
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  searchInput: {
    width: '100%',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  trackCard: {
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalS,
    border: `1px solid transparent`,
    transitionProperty: 'border-color',
    transitionDuration: '0.15s',
    transitionTimingFunction: 'ease',
  },
  trackCardHover: {
    ':hover': {
      border: `1px solid ${tokens.colorNeutralStroke1}`,
    },
  },
  trackCardSelected: {
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  trackHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalS,
    cursor: 'pointer',
  },
  trackInfo: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  trackName: {
    fontWeight: tokens.fontWeightSemibold,
  },
  trackLanguage: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  trackActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXXS,
  },
  trackCaptions: {
    borderTop: `1px solid ${tokens.colorNeutralStroke3}`,
    padding: tokens.spacingVerticalS,
    maxHeight: '200px',
    overflow: 'auto',
  },
  captionItem: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    padding: tokens.spacingVerticalXS,
    borderRadius: tokens.borderRadiusSmall,
    cursor: 'pointer',
    transition: 'background-color 0.15s ease',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground4,
    },
  },
  captionItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
    ':hover': {
      backgroundColor: tokens.colorBrandBackground2,
    },
  },
  captionTime: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontFamily: 'monospace',
    whiteSpace: 'nowrap',
    marginRight: tokens.spacingHorizontalS,
  },
  captionText: {
    flex: 1,
    fontSize: tokens.fontSizeBase200,
    wordBreak: 'break-word',
  },
  captionActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalXXS,
    opacity: 0,
    transition: 'opacity 0.15s ease',
  },
  captionItemHover: {
    ':hover .caption-actions': {
      opacity: 1,
    },
  },
  emptyTrack: {
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  dialogContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  positionSelector: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
  positionButton: {
    flex: 1,
  },
});

/**
 * Format seconds to display timecode (MM:SS)
 */
function formatTimecode(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}

interface TrackCardProps {
  track: CaptionTrack;
  isSelected: boolean;
  selectedCaptionId: string | null;
  onSelect: () => void;
  onCaptionSelect: (captionId: string) => void;
  onToggleVisibility: () => void;
  onToggleLock: () => void;
  onDelete: () => void;
  onDuplicate: () => void;
  onExportSRT: () => void;
  onExportVTT: () => void;
}

const TrackCard: FC<TrackCardProps> = ({
  track,
  isSelected,
  selectedCaptionId,
  onSelect,
  onCaptionSelect,
  onToggleVisibility,
  onToggleLock,
  onDelete,
  onDuplicate,
  onExportSRT,
  onExportVTT,
}) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();

  const handleDeleteCaption = useCallback(
    (captionId: string, e: React.MouseEvent) => {
      e.stopPropagation();
      captionsStore.removeCaption(track.id, captionId);
    },
    [captionsStore, track.id]
  );

  return (
    <div
      className={mergeClasses(
        styles.trackCard,
        styles.trackCardHover,
        isSelected && styles.trackCardSelected
      )}
    >
      <div
        className={styles.trackHeader}
        onClick={onSelect}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onSelect();
          }
        }}
        role="button"
        tabIndex={0}
      >
        <div className={styles.trackInfo}>
          <ClosedCaption24Regular />
          <div>
            <Text className={styles.trackName}>{track.name}</Text>
            <Text className={styles.trackLanguage}> ({track.language})</Text>
          </div>
        </div>
        <div className={styles.trackActions}>
          <Tooltip content={track.visible ? 'Hide track' : 'Show track'} relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={track.visible ? <Eye24Regular /> : <EyeOff24Regular />}
              onClick={(e) => {
                e.stopPropagation();
                onToggleVisibility();
              }}
            />
          </Tooltip>
          <Tooltip content={track.locked ? 'Unlock track' : 'Lock track'} relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={track.locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
              onClick={(e) => {
                e.stopPropagation();
                onToggleLock();
              }}
            />
          </Tooltip>
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <Button
                appearance="subtle"
                size="small"
                icon={<MoreHorizontal24Regular />}
                onClick={(e) => e.stopPropagation()}
              />
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem icon={<Copy24Regular />} onClick={onDuplicate}>
                  Duplicate Track
                </MenuItem>
                <MenuItem icon={<ArrowDownload24Regular />} onClick={onExportSRT}>
                  Export as SRT
                </MenuItem>
                <MenuItem icon={<ArrowDownload24Regular />} onClick={onExportVTT}>
                  Export as VTT
                </MenuItem>
                <Divider />
                <MenuItem icon={<Delete24Regular />} onClick={onDelete}>
                  Delete Track
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      </div>

      {isSelected && (
        <div className={styles.trackCaptions}>
          {track.captions.length === 0 ? (
            <div className={styles.emptyTrack}>
              No captions yet. Add a caption or import from file.
            </div>
          ) : (
            track.captions.map((caption) => (
              <div
                key={caption.id}
                className={mergeClasses(
                  styles.captionItem,
                  styles.captionItemHover,
                  selectedCaptionId === caption.id && styles.captionItemSelected
                )}
                onClick={() => onCaptionSelect(caption.id)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onCaptionSelect(caption.id);
                  }
                }}
                role="button"
                tabIndex={0}
              >
                <Text className={styles.captionTime}>
                  {formatTimecode(caption.startTime)} - {formatTimecode(caption.endTime)}
                </Text>
                <Text className={styles.captionText}>{caption.text}</Text>
                <div className={mergeClasses(styles.captionActions, 'caption-actions')}>
                  <Tooltip content="Delete caption" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<Delete24Regular />}
                      onClick={(e) => handleDeleteCaption(caption.id, e)}
                    />
                  </Tooltip>
                </div>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
};

export const CaptionsPanel: FC<CaptionsPanelProps> = ({ className }) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [isAddTrackDialogOpen, setIsAddTrackDialogOpen] = useState(false);
  const [isAddCaptionDialogOpen, setIsAddCaptionDialogOpen] = useState(false);
  const [newTrackName, setNewTrackName] = useState('');
  const [newTrackLanguage, setNewTrackLanguage] = useState('en-US');
  const [newCaptionText, setNewCaptionText] = useState('');
  const [newCaptionDuration, setNewCaptionDuration] = useState('3');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();

  const filteredTracks = useMemo(() => {
    if (!searchQuery) return captionsStore.tracks;

    const query = searchQuery.toLowerCase();
    return captionsStore.tracks.filter(
      (track) =>
        track.name.toLowerCase().includes(query) ||
        track.language.toLowerCase().includes(query) ||
        track.captions.some((c) => c.text.toLowerCase().includes(query))
    );
  }, [captionsStore.tracks, searchQuery]);

  const handleAddTrack = useCallback(() => {
    if (newTrackName.trim()) {
      captionsStore.addTrack(newTrackName.trim(), newTrackLanguage);
      setNewTrackName('');
      setNewTrackLanguage('en-US');
      setIsAddTrackDialogOpen(false);
    }
  }, [captionsStore, newTrackName, newTrackLanguage]);

  const handleAddCaption = useCallback(() => {
    const trackId = captionsStore.selectedTrackId;
    if (!trackId || !newCaptionText.trim()) return;

    const currentTime = playbackStore.currentTime;
    const duration = parseFloat(newCaptionDuration) || 3;

    captionsStore.addCaption(trackId, currentTime, currentTime + duration, newCaptionText.trim());
    setNewCaptionText('');
    setNewCaptionDuration('3');
    setIsAddCaptionDialogOpen(false);
  }, [captionsStore, playbackStore.currentTime, newCaptionText, newCaptionDuration]);

  const handleImportFile = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0];

      // Reset file input immediately to allow selecting the same file again
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }

      if (!file) return;

      let trackId = captionsStore.selectedTrackId;
      if (!trackId) {
        // Create a new track if none selected
        const newTrackId = captionsStore.addTrack(file.name.replace(/\.(srt|vtt)$/i, ''));
        if (!newTrackId) return;
        captionsStore.selectTrack(newTrackId);
        trackId = newTrackId;
      }

      const reader = new FileReader();
      reader.onload = (e) => {
        const content = e.target?.result as string;
        const targetTrackId = captionsStore.selectedTrackId;
        if (!targetTrackId) return;

        if (file.name.toLowerCase().endsWith('.srt')) {
          captionsStore.importSRT(targetTrackId, content);
        } else if (file.name.toLowerCase().endsWith('.vtt')) {
          captionsStore.importVTT(targetTrackId, content);
        }
      };
      reader.readAsText(file);
    },
    [captionsStore]
  );

  const handleExportSRT = useCallback(
    (trackId: string) => {
      const content = captionsStore.exportToSRT(trackId);
      const track = captionsStore.getTrackById(trackId);
      if (!content || !track) return;

      const blob = new Blob([content], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${track.name}.srt`;
      a.click();
      URL.revokeObjectURL(url);
    },
    [captionsStore]
  );

  const handleExportVTT = useCallback(
    (trackId: string) => {
      const content = captionsStore.exportToVTT(trackId);
      const track = captionsStore.getTrackById(trackId);
      if (!content || !track) return;

      const blob = new Blob([content], { type: 'text/vtt' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${track.name}.vtt`;
      a.click();
      URL.revokeObjectURL(url);
    },
    [captionsStore]
  );

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <ClosedCaption24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Captions
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            ({captionsStore.tracks.length})
          </Text>
        </div>
        <div className={styles.headerActions}>
          <Tooltip content="Import SRT/VTT file" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<ArrowUpload24Regular />}
              onClick={() => fileInputRef.current?.click()}
            />
          </Tooltip>
          <input
            ref={fileInputRef}
            type="file"
            accept=".srt,.vtt"
            style={{ display: 'none' }}
            onChange={handleImportFile}
          />
          <Dialog
            open={isAddTrackDialogOpen}
            onOpenChange={(_, data) => setIsAddTrackDialogOpen(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Tooltip content="Add caption track" relationship="label">
                <Button appearance="subtle" size="small" icon={<Add24Regular />} />
              </Tooltip>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Add Caption Track</DialogTitle>
                <DialogContent className={styles.dialogContent}>
                  <Field label="Track Name" required>
                    <Input
                      value={newTrackName}
                      onChange={(_, data) => setNewTrackName(data.value)}
                      placeholder="e.g., English Subtitles"
                    />
                  </Field>
                  <Field label="Language">
                    <Select
                      value={newTrackLanguage}
                      onChange={(_, data) => setNewTrackLanguage(data.value)}
                    >
                      <option value="en-US">English (US)</option>
                      <option value="en-GB">English (UK)</option>
                      <option value="es-ES">Spanish</option>
                      <option value="fr-FR">French</option>
                      <option value="de-DE">German</option>
                      <option value="it-IT">Italian</option>
                      <option value="pt-BR">Portuguese (Brazil)</option>
                      <option value="ja-JP">Japanese</option>
                      <option value="ko-KR">Korean</option>
                      <option value="zh-CN">Chinese (Simplified)</option>
                      <option value="ar-SA">Arabic</option>
                      <option value="hi-IN">Hindi</option>
                    </Select>
                  </Field>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Cancel</Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleAddTrack}
                    disabled={!newTrackName.trim()}
                  >
                    Add Track
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        </div>
      </div>

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search captions..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        {captionsStore.selectedTrackId && (
          <Dialog
            open={isAddCaptionDialogOpen}
            onOpenChange={(_, data) => setIsAddCaptionDialogOpen(data.open)}
          >
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="primary" size="small" icon={<Add24Regular />}>
                Add Caption
              </Button>
            </DialogTrigger>
            <DialogSurface>
              <DialogBody>
                <DialogTitle>Add Caption</DialogTitle>
                <DialogContent className={styles.dialogContent}>
                  <Field label="Caption Text" required>
                    <Textarea
                      value={newCaptionText}
                      onChange={(_, data) => setNewCaptionText(data.value)}
                      placeholder="Enter caption text..."
                      rows={3}
                    />
                  </Field>
                  <Field label="Duration (seconds)">
                    <Input
                      type="number"
                      value={newCaptionDuration}
                      onChange={(_, data) => setNewCaptionDuration(data.value)}
                      min="0.5"
                      step="0.5"
                    />
                  </Field>
                  <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
                    Caption will start at current playhead position (
                    {formatTimecode(playbackStore.currentTime)})
                  </Text>
                </DialogContent>
                <DialogActions>
                  <DialogTrigger disableButtonEnhancement>
                    <Button appearance="secondary">Cancel</Button>
                  </DialogTrigger>
                  <Button
                    appearance="primary"
                    onClick={handleAddCaption}
                    disabled={!newCaptionText.trim()}
                  >
                    Add Caption
                  </Button>
                </DialogActions>
              </DialogBody>
            </DialogSurface>
          </Dialog>
        )}
      </div>

      <div className={styles.content}>
        {filteredTracks.length === 0 ? (
          <EmptyState
            icon={<ClosedCaption24Regular />}
            title="No caption tracks"
            description="Add a caption track to get started, or import from an SRT/VTT file."
            size="medium"
          />
        ) : (
          filteredTracks.map((track) => (
            <TrackCard
              key={track.id}
              track={track}
              isSelected={captionsStore.selectedTrackId === track.id}
              selectedCaptionId={captionsStore.selectedCaptionId}
              onSelect={() => captionsStore.selectTrack(track.id)}
              onCaptionSelect={(captionId) => captionsStore.selectCaption(captionId)}
              onToggleVisibility={() => captionsStore.toggleTrackVisibility(track.id)}
              onToggleLock={() => captionsStore.toggleTrackLock(track.id)}
              onDelete={() => captionsStore.removeTrack(track.id)}
              onDuplicate={() => captionsStore.duplicateTrack(track.id)}
              onExportSRT={() => handleExportSRT(track.id)}
              onExportVTT={() => handleExportVTT(track.id)}
            />
          ))
        )}
      </div>
    </div>
  );
};

export default CaptionsPanel;
