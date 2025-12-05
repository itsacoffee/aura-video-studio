/**
 * CaptionsPanel Component
 *
 * Caption management panel with:
 * - Caption track list with language tags
 * - Caption list for selected track
 * - Add/Edit/Delete controls
 * - Import/Export buttons (SRT, VTT)
 * - Default style editor
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Divider,
  Badge,
  Input,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Delete24Regular,
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Eye24Regular,
  EyeOff24Regular,
  LockClosed24Regular,
  LockOpen24Regular,
  ChevronDown16Regular,
  Edit24Regular,
  ClosedCaption24Regular,
} from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback, useRef } from 'react';
import type { FC, ChangeEvent } from 'react';
import { useOpenCutCaptionsStore } from '../../../stores/opencutCaptions';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { openCutTokens, motionVariants } from '../../../styles/designTokens';
import { CaptionEditor } from './CaptionEditor';
import { CaptionStyleEditor } from './CaptionStyleEditor';

export interface CaptionsPanelProps {
  className?: string;
}

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  section: {
    marginBottom: tokens.spacingVerticalL,
  },
  sectionHeader: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  sectionTitle: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  trackList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  trackItem: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalS}`,
    borderRadius: openCutTokens.radius.sm,
    backgroundColor: tokens.colorNeutralBackground3,
    cursor: 'pointer',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  trackItemSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    boxShadow: `inset 0 0 0 1px ${tokens.colorBrandStroke1}`,
  },
  trackInfo: {
    flex: 1,
    minWidth: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  trackName: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightMedium,
    color: tokens.colorNeutralForeground1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  trackMeta: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  trackControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
  },
  captionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  captionItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalS}`,
    borderRadius: openCutTokens.radius.sm,
    backgroundColor: tokens.colorNeutralBackground3,
    cursor: 'pointer',
    transition: 'background-color 150ms ease-out',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  captionItemSelected: {
    backgroundColor: tokens.colorNeutralBackground1Selected,
    boxShadow: `inset 0 0 0 1px ${tokens.colorBrandStroke1}`,
  },
  captionTime: {
    minWidth: '80px',
    fontSize: tokens.fontSizeBase100,
    fontFamily: 'ui-monospace, SFMono-Regular, monospace',
    color: tokens.colorNeutralForeground3,
  },
  captionText: {
    flex: 1,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground1,
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground4,
  },
  hiddenInput: {
    display: 'none',
  },
  controlButton: {
    minWidth: '24px',
    minHeight: '24px',
    padding: '2px',
  },
});

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

export const CaptionsPanel: FC<CaptionsPanelProps> = ({ className }) => {
  const styles = useStyles();
  const captionsStore = useOpenCutCaptionsStore();
  const playbackStore = useOpenCutPlaybackStore();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [importFormat, setImportFormat] = useState<'srt' | 'vtt'>('srt');
  const [isAddingTrack, setIsAddingTrack] = useState(false);
  const [newTrackName, setNewTrackName] = useState('');

  const {
    tracks,
    selectedTrackId,
    selectedCaptionId,
    addTrack,
    removeTrack,
    selectTrack,
    selectCaption,
    setTrackVisibility,
    setTrackLocked,
    addCaption,
    removeCaption,
    importSRT,
    importVTT,
    exportSRT,
    exportVTT,
    getCaptionsForTrack,
    getTrackById,
  } = captionsStore;

  const selectedTrack = selectedTrackId ? getTrackById(selectedTrackId) : undefined;
  const captions = selectedTrackId ? getCaptionsForTrack(selectedTrackId) : [];

  const handleAddTrack = useCallback(() => {
    if (newTrackName.trim()) {
      const trackId = addTrack(newTrackName.trim());
      selectTrack(trackId);
      setNewTrackName('');
      setIsAddingTrack(false);
    }
  }, [newTrackName, addTrack, selectTrack]);

  const handleAddCaption = useCallback(() => {
    if (selectedTrackId) {
      const currentTime = playbackStore.currentTime;
      const captionId = addCaption(selectedTrackId, currentTime, currentTime + 3, 'New caption');
      selectCaption(captionId);
    }
  }, [selectedTrackId, playbackStore.currentTime, addCaption, selectCaption]);

  const handleImportClick = useCallback((format: 'srt' | 'vtt') => {
    setImportFormat(format);
    fileInputRef.current?.click();
  }, []);

  const handleFileChange = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file && selectedTrackId) {
        const reader = new FileReader();
        reader.onload = (event) => {
          const content = event.target?.result as string;
          if (importFormat === 'srt') {
            importSRT(selectedTrackId, content);
          } else {
            importVTT(selectedTrackId, content);
          }
        };
        reader.readAsText(file);
      }
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    },
    [selectedTrackId, importFormat, importSRT, importVTT]
  );

  const handleExport = useCallback(
    (format: 'srt' | 'vtt') => {
      if (!selectedTrackId) return;

      const content = format === 'srt' ? exportSRT(selectedTrackId) : exportVTT(selectedTrackId);
      const blob = new Blob([content], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `captions.${format}`;
      a.click();
      URL.revokeObjectURL(url);
    },
    [selectedTrackId, exportSRT, exportVTT]
  );

  const handleCaptionClick = useCallback(
    (captionId: string, startTime: number) => {
      selectCaption(captionId);
      playbackStore.seek(startTime);
    },
    [selectCaption, playbackStore]
  );

  return (
    <div className={mergeClasses(styles.root, className)}>
      <input
        ref={fileInputRef}
        type="file"
        accept=".srt,.vtt"
        className={styles.hiddenInput}
        onChange={handleFileChange}
      />

      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <ClosedCaption24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Captions
          </Text>
        </div>
        <div className={styles.headerActions}>
          <Tooltip content="Add track" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              icon={<Add24Regular />}
              onClick={() => setIsAddingTrack(true)}
              className={styles.controlButton}
            />
          </Tooltip>
        </div>
      </div>

      <div className={styles.content}>
        {/* Tracks Section */}
        <div className={styles.section}>
          <div className={styles.sectionHeader}>
            <Text className={styles.sectionTitle}>Tracks</Text>
          </div>

          <AnimatePresence mode="popLayout">
            {isAddingTrack && (
              <motion.div
                {...motionVariants.slideUp}
                style={{ marginBottom: tokens.spacingVerticalS }}
              >
                <Input
                  autoFocus
                  placeholder="Track name..."
                  value={newTrackName}
                  onChange={(_, data) => setNewTrackName(data.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleAddTrack();
                    if (e.key === 'Escape') setIsAddingTrack(false);
                  }}
                  onBlur={() => {
                    if (!newTrackName.trim()) setIsAddingTrack(false);
                  }}
                  contentAfter={
                    <Button
                      appearance="transparent"
                      size="small"
                      icon={<Add24Regular />}
                      onClick={handleAddTrack}
                    />
                  }
                />
              </motion.div>
            )}
          </AnimatePresence>

          <div className={styles.trackList}>
            {tracks.map((track) => (
              <motion.div
                key={track.id}
                className={mergeClasses(
                  styles.trackItem,
                  selectedTrackId === track.id && styles.trackItemSelected
                )}
                onClick={() => selectTrack(track.id)}
                layout
              >
                <div className={styles.trackInfo}>
                  <span className={styles.trackName}>{track.name}</span>
                  <div className={styles.trackMeta}>
                    <Badge appearance="outline" size="small">
                      {track.language.toUpperCase()}
                    </Badge>
                    <Text size={100} style={{ color: tokens.colorNeutralForeground4 }}>
                      {track.captions.length} caption{track.captions.length !== 1 ? 's' : ''}
                    </Text>
                    {track.isDefault && (
                      <Badge appearance="filled" size="small" color="brand">
                        Default
                      </Badge>
                    )}
                  </div>
                </div>
                <div className={styles.trackControls}>
                  <Tooltip content={track.visible ? 'Hide' : 'Show'} relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      className={styles.controlButton}
                      icon={track.visible ? <Eye24Regular /> : <EyeOff24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        setTrackVisibility(track.id, !track.visible);
                      }}
                    />
                  </Tooltip>
                  <Tooltip content={track.locked ? 'Unlock' : 'Lock'} relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      className={styles.controlButton}
                      icon={track.locked ? <LockClosed24Regular /> : <LockOpen24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        setTrackLocked(track.id, !track.locked);
                      }}
                    />
                  </Tooltip>
                  <Tooltip content="Delete track" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      className={styles.controlButton}
                      icon={<Delete24Regular />}
                      onClick={(e) => {
                        e.stopPropagation();
                        removeTrack(track.id);
                      }}
                    />
                  </Tooltip>
                </div>
              </motion.div>
            ))}

            {tracks.length === 0 && !isAddingTrack && (
              <div className={styles.emptyState}>
                <ClosedCaption24Regular style={{ width: 32, height: 32, opacity: 0.5 }} />
                <Text size={200}>No caption tracks</Text>
                <Button appearance="primary" size="small" onClick={() => setIsAddingTrack(true)}>
                  Add Track
                </Button>
              </div>
            )}
          </div>
        </div>

        {/* Captions Section - only show when a track is selected */}
        {selectedTrack && (
          <>
            <Divider />

            <div className={styles.section} style={{ marginTop: tokens.spacingVerticalM }}>
              <div className={styles.sectionHeader}>
                <Text className={styles.sectionTitle}>Captions ({captions.length})</Text>
                <div style={{ display: 'flex', gap: tokens.spacingHorizontalXS }}>
                  <Menu>
                    <MenuTrigger disableButtonEnhancement>
                      <Tooltip content="Import" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<ArrowDownload24Regular />}
                          className={styles.controlButton}
                        >
                          <ChevronDown16Regular />
                        </Button>
                      </Tooltip>
                    </MenuTrigger>
                    <MenuPopover>
                      <MenuList>
                        <MenuItem onClick={() => handleImportClick('srt')}>Import SRT</MenuItem>
                        <MenuItem onClick={() => handleImportClick('vtt')}>Import VTT</MenuItem>
                      </MenuList>
                    </MenuPopover>
                  </Menu>

                  <Menu>
                    <MenuTrigger disableButtonEnhancement>
                      <Tooltip content="Export" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<ArrowUpload24Regular />}
                          className={styles.controlButton}
                          disabled={captions.length === 0}
                        >
                          <ChevronDown16Regular />
                        </Button>
                      </Tooltip>
                    </MenuTrigger>
                    <MenuPopover>
                      <MenuList>
                        <MenuItem onClick={() => handleExport('srt')}>Export SRT</MenuItem>
                        <MenuItem onClick={() => handleExport('vtt')}>Export VTT</MenuItem>
                      </MenuList>
                    </MenuPopover>
                  </Menu>

                  <Tooltip content="Add caption at playhead" relationship="label">
                    <Button
                      appearance="subtle"
                      size="small"
                      icon={<Add24Regular />}
                      onClick={handleAddCaption}
                      className={styles.controlButton}
                    />
                  </Tooltip>
                </div>
              </div>

              <div className={styles.captionList}>
                {captions.map((caption) => (
                  <motion.div
                    key={caption.id}
                    className={mergeClasses(
                      styles.captionItem,
                      selectedCaptionId === caption.id && styles.captionItemSelected
                    )}
                    onClick={() => handleCaptionClick(caption.id, caption.startTime)}
                    layout
                  >
                    <span className={styles.captionTime}>
                      {formatTime(caption.startTime)} - {formatTime(caption.endTime)}
                    </span>
                    <span className={styles.captionText}>{caption.text}</span>
                    <div style={{ display: 'flex', gap: '2px' }}>
                      <Tooltip content="Edit" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<Edit24Regular />}
                          className={styles.controlButton}
                          onClick={(e) => {
                            e.stopPropagation();
                            selectCaption(caption.id);
                          }}
                        />
                      </Tooltip>
                      <Tooltip content="Delete" relationship="label">
                        <Button
                          appearance="subtle"
                          size="small"
                          icon={<Delete24Regular />}
                          className={styles.controlButton}
                          onClick={(e) => {
                            e.stopPropagation();
                            removeCaption(caption.id);
                          }}
                        />
                      </Tooltip>
                    </div>
                  </motion.div>
                ))}

                {captions.length === 0 && (
                  <div className={styles.emptyState}>
                    <Text size={200}>No captions in this track</Text>
                    <Button appearance="primary" size="small" onClick={handleAddCaption}>
                      Add Caption
                    </Button>
                  </div>
                )}
              </div>
            </div>

            {/* Caption Editor - show when a caption is selected */}
            {selectedCaptionId && (
              <>
                <Divider />
                <div className={styles.section} style={{ marginTop: tokens.spacingVerticalM }}>
                  <div className={styles.sectionHeader}>
                    <Text className={styles.sectionTitle}>Edit Caption</Text>
                  </div>
                  <CaptionEditor captionId={selectedCaptionId} />
                </div>
              </>
            )}

            {/* Style Editor */}
            <Divider />
            <div className={styles.section} style={{ marginTop: tokens.spacingVerticalM }}>
              <div className={styles.sectionHeader}>
                <Text className={styles.sectionTitle}>Default Style</Text>
              </div>
              <CaptionStyleEditor />
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default CaptionsPanel;
