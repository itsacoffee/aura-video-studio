/**
 * MediaPanel Component
 *
 * Media library panel with professional features:
 * - Grid and list view toggle
 * - Search/filter functionality
 * - Sort options (name, date, type, duration)
 * - Right-click context menu
 * - Drag to timeline support
 * - Larger thumbnail previews
 */

import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tooltip,
  mergeClasses,
  Input,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  MenuDivider,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Video24Regular,
  MusicNote224Regular,
  Image24Regular,
  Folder24Regular,
  Search24Regular,
  Grid24Regular,
  TextBulletListSquare24Regular,
  Delete24Regular,
  Info24Regular,
  MoreHorizontal24Regular,
} from '@fluentui/react-icons';
import { useRef, useState, useCallback, useMemo } from 'react';
import type { FC, DragEvent, MouseEvent as ReactMouseEvent } from 'react';
import { useOpenCutMediaStore, type OpenCutMediaFile } from '../../stores/opencutMedia';
import { useOpenCutTimelineStore } from '../../stores/opencutTimeline';
import { openCutTokens } from '../../styles/designTokens';
import { EmptyState } from './EmptyState';

export interface MediaPanelProps {
  className?: string;
}

type ViewMode = 'grid' | 'list';
type SortBy = 'name' | 'date' | 'type' | 'duration';
type SortOrder = 'asc' | 'desc';

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
    padding: `${openCutTokens.spacing.md} ${openCutTokens.spacing.lg}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    minHeight: openCutTokens.layout.panelHeaderHeight,
    gap: openCutTokens.spacing.md,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
  },
  headerIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '18px',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.xs,
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    padding: `${openCutTokens.spacing.sm} ${openCutTokens.spacing.md}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke3}`,
    gap: openCutTokens.spacing.sm,
    minHeight: openCutTokens.layout.toolbarHeight,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  searchInput: {
    flex: 1,
    minWidth: '80px',
    maxWidth: '180px',
  },
  viewControls: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    padding: '2px',
  },
  viewButton: {
    minWidth: openCutTokens.layout.iconButtonSize,
    minHeight: openCutTokens.layout.iconButtonSize,
    padding: '4px',
    borderRadius: tokens.borderRadiusSmall,
  },
  viewButtonActive: {
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: tokens.shadow2,
  },
  sortDropdown: {
    minWidth: '90px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: openCutTokens.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.md,
  },
  mediaGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))',
    gap: openCutTokens.spacing.sm,
  },
  mediaGridLarge: {
    gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
  },
  mediaList: {
    display: 'flex',
    flexDirection: 'column',
    gap: openCutTokens.spacing.xs,
  },
  mediaItem: {
    aspectRatio: '16 / 9',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    cursor: 'grab',
    border: `1px solid transparent`,
    transition: `all ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    overflow: 'hidden',
    position: 'relative',
    ':hover': {
      border: `1px solid ${tokens.colorBrandStroke1}`,
      backgroundColor: tokens.colorNeutralBackground3,
      transform: 'scale(1.02)',
      boxShadow: tokens.shadow4,
    },
    ':focus-visible': {
      outline: `2px solid ${tokens.colorBrandStroke1}`,
      outlineOffset: '2px',
    },
    ':active': {
      cursor: 'grabbing',
    },
  },
  mediaItemSelected: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    boxShadow: tokens.shadow8,
  },
  mediaItemDragging: {
    opacity: 0.5,
    transform: 'scale(0.95)',
  },
  mediaListItem: {
    display: 'flex',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    padding: openCutTokens.spacing.sm,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'grab',
    transition: `all ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3,
    },
  },
  mediaListItemSelected: {
    backgroundColor: tokens.colorBrandBackground2,
  },
  mediaListThumbnail: {
    width: '56px',
    height: '32px',
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground4,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    flexShrink: 0,
  },
  mediaListInfo: {
    flex: 1,
    minWidth: 0,
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
  },
  mediaListName: {
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  mediaListMeta: {
    display: 'flex',
    gap: openCutTokens.spacing.sm,
    color: tokens.colorNeutralForeground3,
  },
  mediaItemImage: {
    width: '100%',
    height: '100%',
    objectFit: 'cover',
  },
  mediaItemIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  mediaItemOverlay: {
    position: 'absolute',
    inset: 0,
    background: 'linear-gradient(transparent 50%, rgba(0,0,0,0.6) 100%)',
    opacity: 0,
    transition: `opacity ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
    display: 'flex',
    alignItems: 'flex-end',
    justifyContent: 'space-between',
    padding: openCutTokens.spacing.xs,
    ':hover': {
      opacity: 1,
    },
  },
  mediaItemName: {
    fontSize: openCutTokens.typography.fontSize.xs,
    color: 'white',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    flex: 1,
    textShadow: '0 1px 2px rgba(0,0,0,0.5)',
  },
  mediaItemDuration: {
    backgroundColor: 'rgba(0, 0, 0, 0.75)',
    color: 'white',
    padding: `2px ${openCutTokens.spacing.xs}`,
    borderRadius: tokens.borderRadiusSmall,
    fontSize: openCutTokens.typography.fontSize.xs,
    fontFamily: openCutTokens.typography.fontFamily.mono,
  },
  mediaItemActions: {
    position: 'absolute',
    top: openCutTokens.spacing.xs,
    right: openCutTokens.spacing.xs,
    opacity: 0,
    transition: `opacity ${openCutTokens.animation.duration.fast} ${openCutTokens.animation.easing.easeOut}`,
  },
  mediaItemActionsVisible: {
    opacity: 1,
  },
  dropZone: {
    border: `2px dashed ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge,
    padding: openCutTokens.spacing.lg,
    textAlign: 'center',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: openCutTokens.spacing.sm,
    transition: `all ${openCutTokens.animation.duration.normal} ${openCutTokens.animation.easing.easeOut}`,
    backgroundColor: tokens.colorNeutralBackground3,
    minHeight: '72px',
    marginTop: 'auto',
  },
  dropZoneActive: {
    border: `2px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
    transform: 'scale(1.01)',
  },
  dropZoneIcon: {
    color: tokens.colorNeutralForeground3,
    fontSize: '20px',
  },
  importButton: {
    minWidth: openCutTokens.layout.controlButtonSizeCompact,
    minHeight: openCutTokens.layout.controlButtonSizeCompact,
  },
  emptyMessage: {
    textAlign: 'center',
    padding: openCutTokens.spacing.lg,
    color: tokens.colorNeutralForeground3,
  },
});

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

export const MediaPanel: FC<MediaPanelProps> = ({ className }) => {
  const styles = useStyles();
  const [isDragging, setIsDragging] = useState(false);
  const [draggingMediaId, setDraggingMediaId] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortBy>('name');
  const [sortOrder] = useState<SortOrder>('asc');
  const [contextMenuMedia, setContextMenuMedia] = useState<OpenCutMediaFile | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const mediaStore = useOpenCutMediaStore();
  const timelineStore = useOpenCutTimelineStore();

  // Filter and sort media files
  const filteredMedia = useMemo(() => {
    let files = [...mediaStore.mediaFiles];

    // Filter by search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      files = files.filter(
        (f) => f.name.toLowerCase().includes(query) || f.type.toLowerCase().includes(query)
      );
    }

    // Sort files
    files.sort((a, b) => {
      let comparison = 0;
      switch (sortBy) {
        case 'name':
          comparison = a.name.localeCompare(b.name);
          break;
        case 'type':
          comparison = a.type.localeCompare(b.type);
          break;
        case 'duration':
          comparison = (a.duration || 0) - (b.duration || 0);
          break;
        case 'date':
          comparison = 0; // No date available in current implementation
          break;
      }
      return sortOrder === 'asc' ? comparison : -comparison;
    });

    return files;
  }, [mediaStore.mediaFiles, searchQuery, sortBy, sortOrder]);

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
    async (e: DragEvent) => {
      e.preventDefault();
      setIsDragging(false);
      if (e.dataTransfer.files.length > 0) {
        await handleFileSelect(e.dataTransfer.files);
      }
    },
    [handleFileSelect]
  );

  const handleDragOver = useCallback((e: DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleImportClick = useCallback(() => {
    fileInputRef.current?.click();
  }, []);

  const handleMediaDragStart = useCallback((mediaId: string, e: DragEvent) => {
    setDraggingMediaId(mediaId);
    e.dataTransfer.setData('application/x-opencut-media', mediaId);
    e.dataTransfer.effectAllowed = 'copy';
  }, []);

  const handleMediaDragEnd = useCallback(() => {
    setDraggingMediaId(null);
  }, []);

  const handleContextMenu = useCallback((media: OpenCutMediaFile, e: ReactMouseEvent) => {
    e.preventDefault();
    setContextMenuMedia(media);
  }, []);

  const handleDeleteMedia = useCallback(
    (mediaId: string) => {
      mediaStore.removeMediaFile(mediaId);
      setContextMenuMedia(null);
    },
    [mediaStore]
  );

  const handleAddToTimeline = useCallback(
    (media: OpenCutMediaFile) => {
      const trackType =
        media.type === 'video' ? 'video' : media.type === 'audio' ? 'audio' : 'image';
      const track = timelineStore.tracks.find((t) => t.type === trackType);

      if (track) {
        const existingClips = timelineStore.clips.filter((c) => c.trackId === track.id);
        const startTime =
          existingClips.length > 0
            ? Math.max(...existingClips.map((c) => c.startTime + c.duration))
            : 0;

        timelineStore.addClip({
          trackId: track.id,
          type: trackType,
          name: media.name,
          mediaId: media.id,
          startTime,
          duration: media.duration || 5,
          inPoint: 0,
          outPoint: media.duration || 5,
          thumbnailUrl: media.thumbnailUrl,
          transform: {
            scaleX: 100,
            scaleY: 100,
            positionX: 0,
            positionY: 0,
            rotation: 0,
            opacity: 100,
            anchorX: 50,
            anchorY: 50,
          },
          blendMode: 'normal',
          speed: 1,
          reversed: false,
          timeRemapEnabled: false,
          locked: false,
        });
      }
      setContextMenuMedia(null);
    },
    [timelineStore]
  );

  const renderMediaItem = (file: OpenCutMediaFile) => {
    const isSelected = mediaStore.selectedMediaId === file.id;
    const isDraggingThis = draggingMediaId === file.id;

    if (viewMode === 'list') {
      return (
        <div
          key={file.id}
          className={mergeClasses(styles.mediaListItem, isSelected && styles.mediaListItemSelected)}
          onClick={() => mediaStore.selectMedia(file.id)}
          onContextMenu={(e) => handleContextMenu(file, e)}
          draggable
          onDragStart={(e) => handleMediaDragStart(file.id, e)}
          onDragEnd={handleMediaDragEnd}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              mediaStore.selectMedia(file.id);
            }
          }}
          aria-label={file.name}
          aria-pressed={isSelected}
        >
          <div className={styles.mediaListThumbnail}>
            {file.thumbnailUrl ? (
              <img src={file.thumbnailUrl} alt={file.name} className={styles.mediaItemImage} />
            ) : file.type === 'video' ? (
              <Video24Regular className={styles.mediaItemIcon} />
            ) : file.type === 'audio' ? (
              <MusicNote224Regular className={styles.mediaItemIcon} />
            ) : (
              <Image24Regular className={styles.mediaItemIcon} />
            )}
          </div>
          <div className={styles.mediaListInfo}>
            <Text size={200} weight="medium" className={styles.mediaListName}>
              {file.name}
            </Text>
            <div className={styles.mediaListMeta}>
              <Text size={100}>{file.type}</Text>
              {file.duration !== undefined && (
                <Text size={100}>{formatDuration(file.duration)}</Text>
              )}
              {file.file && <Text size={100}>{formatBytes(file.file.size)}</Text>}
            </div>
          </div>
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
                <MenuItem icon={<Add24Regular />} onClick={() => handleAddToTimeline(file)}>
                  Add to Timeline
                </MenuItem>
                <MenuDivider />
                <MenuItem icon={<Delete24Regular />} onClick={() => handleDeleteMedia(file.id)}>
                  Delete
                </MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
        </div>
      );
    }

    return (
      <Tooltip key={file.id} content={file.name} relationship="label">
        <div
          className={mergeClasses(
            styles.mediaItem,
            isSelected && styles.mediaItemSelected,
            isDraggingThis && styles.mediaItemDragging
          )}
          onClick={() => mediaStore.selectMedia(file.id)}
          onContextMenu={(e) => handleContextMenu(file, e)}
          onDoubleClick={() => handleAddToTimeline(file)}
          draggable
          onDragStart={(e) => handleMediaDragStart(file.id, e)}
          onDragEnd={handleMediaDragEnd}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              mediaStore.selectMedia(file.id);
            }
          }}
          aria-label={file.name}
          aria-pressed={isSelected}
        >
          {file.thumbnailUrl ? (
            <img src={file.thumbnailUrl} alt={file.name} className={styles.mediaItemImage} />
          ) : file.type === 'video' ? (
            <Video24Regular className={styles.mediaItemIcon} />
          ) : file.type === 'audio' ? (
            <MusicNote224Regular className={styles.mediaItemIcon} />
          ) : (
            <Image24Regular className={styles.mediaItemIcon} />
          )}

          <div className={styles.mediaItemOverlay}>
            <span className={styles.mediaItemName}>{file.name}</span>
          </div>

          {file.duration !== undefined && (
            <span
              className={styles.mediaItemDuration}
              style={{
                position: 'absolute',
                bottom: tokens.spacingVerticalXS,
                right: tokens.spacingHorizontalXS,
              }}
            >
              {formatDuration(file.duration)}
            </span>
          )}
        </div>
      </Tooltip>
    );
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Folder24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Media
          </Text>
          {mediaStore.mediaFiles.length > 0 && (
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              ({mediaStore.mediaFiles.length})
            </Text>
          )}
        </div>
        <div className={styles.headerActions}>
          <Button
            appearance="subtle"
            icon={<Add24Regular />}
            size="small"
            className={styles.importButton}
            onClick={handleImportClick}
          />
        </div>
        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="video/*,audio/*,image/*"
          style={{ display: 'none' }}
          onChange={(e) => handleFileSelect(e.target.files)}
        />
      </div>

      {/* Toolbar */}
      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        <div className={styles.viewControls}>
          <Tooltip content="Grid view" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(
                styles.viewButton,
                viewMode === 'grid' && styles.viewButtonActive
              )}
              icon={<Grid24Regular />}
              onClick={() => setViewMode('grid')}
            />
          </Tooltip>
          <Tooltip content="List view" relationship="label">
            <Button
              appearance="subtle"
              size="small"
              className={mergeClasses(
                styles.viewButton,
                viewMode === 'list' && styles.viewButtonActive
              )}
              icon={<TextBulletListSquare24Regular />}
              onClick={() => setViewMode('list')}
            />
          </Tooltip>
        </div>
        <Dropdown
          className={styles.sortDropdown}
          value={sortBy.charAt(0).toUpperCase() + sortBy.slice(1)}
          onOptionSelect={(_, data) => setSortBy(data.optionValue as SortBy)}
          size="small"
        >
          <Option value="name">Name</Option>
          <Option value="type">Type</Option>
          <Option value="duration">Duration</Option>
        </Dropdown>
      </div>

      <div
        className={styles.content}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        role="region"
        aria-label="Media files drop zone"
      >
        {mediaStore.mediaFiles.length === 0 ? (
          <EmptyState
            icon={<Video24Regular />}
            title="No media files"
            description="Import videos, audio, or images to get started"
            action={{
              label: 'Import Media',
              onClick: handleImportClick,
              icon: <Add24Regular />,
            }}
            size="medium"
          />
        ) : filteredMedia.length === 0 ? (
          <div className={styles.emptyMessage}>
            <Text size={200}>No media matches your search</Text>
          </div>
        ) : viewMode === 'grid' ? (
          <div className={styles.mediaGrid}>{filteredMedia.map(renderMediaItem)}</div>
        ) : (
          <div className={styles.mediaList}>{filteredMedia.map(renderMediaItem)}</div>
        )}

        <div className={mergeClasses(styles.dropZone, isDragging && styles.dropZoneActive)}>
          <Add24Regular className={styles.dropZoneIcon} />
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            Drop media files here
          </Text>
        </div>
      </div>

      {/* Context Menu */}
      {contextMenuMedia && (
        <Menu open onOpenChange={() => setContextMenuMedia(null)}>
          <MenuPopover>
            <MenuList>
              <MenuItem
                icon={<Add24Regular />}
                onClick={() => handleAddToTimeline(contextMenuMedia)}
              >
                Add to Timeline
              </MenuItem>
              <MenuItem icon={<Info24Regular />}>Properties</MenuItem>
              <MenuDivider />
              <MenuItem
                icon={<Delete24Regular />}
                onClick={() => handleDeleteMedia(contextMenuMedia.id)}
              >
                Delete
              </MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
      )}
    </div>
  );
};

export default MediaPanel;
