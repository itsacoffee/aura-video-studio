import { makeStyles, tokens, Text, Button, ToolbarButton } from '@fluentui/react-components';
import { Add24Regular, Grid24Regular, List24Regular, Apps24Regular } from '@fluentui/react-icons';
import React, { useState, useRef, useMemo, useCallback } from 'react';
import { Virtuoso } from 'react-virtuoso';
import { useMediaAssetContextMenu } from '../../hooks/useMediaAssetContextMenu';
import { AssetPreviewModal } from './AssetPreviewModal';
import { AssetPropertiesDialog } from './AssetPropertiesDialog';
import { MediaThumbnail } from './MediaThumbnail';
import { SmartCollections, MediaCollection } from './SmartCollections';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    backgroundColor: tokens.colorNeutralBackground1,
    overflow: 'hidden',
  },
  header: {
    padding: tokens.spacingVerticalM,
    borderBottom: `1px solid ${tokens.colorNeutralStroke1}`,
    backgroundColor: tokens.colorNeutralBackground2,
  },
  titleRow: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
  },
  toolbar: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
  },
  content: {
    flex: 1,
    overflow: 'hidden', // Changed from 'auto' to work with Virtuoso
  },
  mediaGrid: {
    padding: tokens.spacingVerticalM,
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(120px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  mediaList: {
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
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

export interface MediaAsset {
  id: string;
  name: string;
  type: 'video' | 'audio' | 'image';
  file: File;
  preview?: string;
  duration?: number;
  fileSize?: number;
  filePath?: string;
  resolution?: string;
  frameRate?: number;
  codec?: string;
  creationDate?: string;
  isFavorite?: boolean;
  tags?: string[];
}

interface ProjectBinProps {
  assets: MediaAsset[];
  onAddAssets?: () => void;
  onRemoveAsset?: (assetId: string) => void;
  onAssetDragStart?: (asset: MediaAsset) => void;
  onAssetDragEnd?: () => void;
  onAssetSelect?: (asset: MediaAsset) => void;
  onAssetUpdate?: (assetId: string, updates: Partial<MediaAsset>) => void;
  onAddToTimeline?: (assetId: string) => void;
}

type ViewMode = 'thumbnail' | 'list' | 'grid';

export const ProjectBin: React.FC<ProjectBinProps> = ({
  assets,
  onAddAssets,
  onRemoveAsset,
  onAssetDragStart,
  onAssetDragEnd,
  onAssetSelect,
  onAssetUpdate,
  onAddToTimeline,
}) => {
  const styles = useStyles();
  const [viewMode, setViewMode] = useState<ViewMode>('thumbnail');
  const [selectedCollection, setSelectedCollection] = useState<string>('all');
  const virtuosoRef = useRef(null);

  // State for preview modal
  const [previewAssetId, setPreviewAssetId] = useState<string | null>(null);
  const [showPreviewModal, setShowPreviewModal] = useState(false);

  // State for properties dialog
  const [propertiesAssetId, setPropertiesAssetId] = useState<string | null>(null);
  const [showPropertiesDialog, setShowPropertiesDialog] = useState(false);

  // Get asset by ID for dialogs
  const previewAsset = useMemo(
    () => assets.find((a) => a.id === previewAssetId) || null,
    [assets, previewAssetId]
  );
  const propertiesAsset = useMemo(
    () => assets.find((a) => a.id === propertiesAssetId) || null,
    [assets, propertiesAssetId]
  );

  // Use virtual scrolling for large asset lists (> 100 items)
  const useVirtualScrolling = useMemo(() => assets.length > 100, [assets.length]);

  // Create smart collections based on assets
  const collections: MediaCollection[] = [
    {
      id: 'all',
      name: 'All Media',
      type: 'all',
      count: assets.length,
    },
    {
      id: 'video',
      name: 'Video Clips',
      type: 'video',
      count: assets.filter((a) => a.type === 'video').length,
    },
    {
      id: 'audio',
      name: 'Audio Files',
      type: 'audio',
      count: assets.filter((a) => a.type === 'audio').length,
    },
    {
      id: 'image',
      name: 'Images',
      type: 'image',
      count: assets.filter((a) => a.type === 'image').length,
    },
  ];

  // Filter assets based on selected collection
  const filteredAssets =
    selectedCollection === 'all' ? assets : assets.filter((a) => a.type === selectedCollection);

  // Context menu action handlers
  const handleAddToTimeline = useCallback(
    (assetId: string) => {
      console.info('Adding asset to timeline:', assetId);
      onAddToTimeline?.(assetId);
    },
    [onAddToTimeline]
  );

  const handlePreview = useCallback((assetId: string) => {
    console.info('Previewing asset:', assetId);
    setPreviewAssetId(assetId);
    setShowPreviewModal(true);
  }, []);

  const handleRename = useCallback(
    (assetId: string, newName: string) => {
      console.info('Renaming asset:', assetId, 'to', newName);
      onAssetUpdate?.(assetId, { name: newName });
    },
    [onAssetUpdate]
  );

  const handleToggleFavorite = useCallback(
    (assetId: string) => {
      const asset = assets.find((a) => a.id === assetId);
      if (asset) {
        console.info('Toggling favorite for asset:', assetId);
        onAssetUpdate?.(assetId, { isFavorite: !asset.isFavorite });
      }
    },
    [assets, onAssetUpdate]
  );

  const handleDelete = useCallback(
    (assetId: string) => {
      console.info('Deleting asset:', assetId);
      onRemoveAsset?.(assetId);
    },
    [onRemoveAsset]
  );

  const handleShowProperties = useCallback((assetId: string) => {
    console.info('Showing properties for asset:', assetId);
    setPropertiesAssetId(assetId);
    setShowPropertiesDialog(true);
  }, []);

  const handlePropertiesSave = useCallback(
    (assetId: string, updates: { name: string; tags: string[] }) => {
      console.info('Saving properties for asset:', assetId, updates);
      onAssetUpdate?.(assetId, updates);
    },
    [onAssetUpdate]
  );

  // Set up context menu hook
  const handleContextMenu = useMediaAssetContextMenu({
    onAddToTimeline: handleAddToTimeline,
    onPreview: handlePreview,
    onRename: handleRename,
    onToggleFavorite: handleToggleFavorite,
    onDelete: handleDelete,
    onShowProperties: handleShowProperties,
  });

  const handleRevealInFinder = useCallback((asset: MediaAsset) => {
    // In a real implementation, this would call a native API to reveal the file
    // Uses Electron's contextMenu.revealInOS API if available
    if (window.electron?.contextMenu?.revealInOS && asset.filePath) {
      window.electron.contextMenu.revealInOS(asset.filePath).catch((error: unknown) => {
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error('Failed to reveal in finder:', errorMessage);
      });
    } else {
      // Fallback for demo purposes
      console.info(`File location: ${asset.filePath || 'Not available'}`);
    }
  }, []);

  const handleAssetClick = useCallback(
    (asset: MediaAsset) => {
      onAssetSelect?.(asset);
    },
    [onAssetSelect]
  );

  // Memoize item renderer for virtual scrolling
  const renderAsset = useCallback(
    (index: number) => {
      const asset = filteredAssets[index];
      return (
        <MediaThumbnail
          key={asset.id}
          id={asset.id}
          name={asset.name}
          type={asset.type}
          preview={asset.preview}
          duration={asset.duration}
          fileSize={asset.fileSize}
          isFavorite={asset.isFavorite}
          onDragStart={(e) => {
            e.dataTransfer.effectAllowed = 'copy';
            e.dataTransfer.setData(
              'application/json',
              JSON.stringify({
                type: 'media-asset',
                assetId: asset.id,
                assetType: asset.type,
                filePath: asset.filePath,
                duration: asset.duration,
              })
            );
            onAssetDragStart?.(asset);
          }}
          onDragEnd={onAssetDragEnd}
          onRemove={() => onRemoveAsset?.(asset.id)}
          onRevealInFinder={() => handleRevealInFinder(asset)}
          onClick={() => handleAssetClick(asset)}
          onContextMenu={(e) => handleContextMenu(e, asset)}
        />
      );
    },
    [
      filteredAssets,
      onAssetDragStart,
      onAssetDragEnd,
      onRemoveAsset,
      handleRevealInFinder,
      handleAssetClick,
      handleContextMenu,
    ]
  );

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.titleRow}>
          <Text className={styles.title}>Project Bin</Text>
          <div className={styles.toolbar}>
            <ToolbarButton
              appearance={viewMode === 'thumbnail' ? 'primary' : 'subtle'}
              icon={<Apps24Regular />}
              onClick={() => setViewMode('thumbnail')}
              aria-label="Thumbnail view"
            />
            <ToolbarButton
              appearance={viewMode === 'grid' ? 'primary' : 'subtle'}
              icon={<Grid24Regular />}
              onClick={() => setViewMode('grid')}
              aria-label="Grid view"
            />
            <ToolbarButton
              appearance={viewMode === 'list' ? 'primary' : 'subtle'}
              icon={<List24Regular />}
              onClick={() => setViewMode('list')}
              aria-label="List view"
            />
            <Button appearance="primary" icon={<Add24Regular />} onClick={onAddAssets}>
              Add
            </Button>
          </div>
        </div>
      </div>

      <SmartCollections
        collections={collections}
        selectedCollectionId={selectedCollection}
        onCollectionSelect={setSelectedCollection}
      />

      <div className={styles.content}>
        {filteredAssets.length === 0 ? (
          <div className={styles.emptyState}>
            <Text>No media in this collection</Text>
            <Text size={200}>Add files to get started</Text>
          </div>
        ) : useVirtualScrolling && viewMode === 'list' ? (
          // Use virtual scrolling for large lists
          <Virtuoso
            ref={virtuosoRef}
            totalCount={filteredAssets.length}
            itemContent={renderAsset}
            style={{ height: '100%' }}
          />
        ) : (
          <div
            className={viewMode === 'list' ? styles.mediaList : styles.mediaGrid}
            style={{ overflow: 'auto', height: '100%' }}
          >
            {filteredAssets.map((asset) => (
              <MediaThumbnail
                key={asset.id}
                id={asset.id}
                name={asset.name}
                type={asset.type}
                preview={asset.preview}
                duration={asset.duration}
                fileSize={asset.fileSize}
                isFavorite={asset.isFavorite}
                onDragStart={(e) => {
                  e.dataTransfer.effectAllowed = 'copy';
                  e.dataTransfer.setData(
                    'application/json',
                    JSON.stringify({
                      type: 'media-asset',
                      assetId: asset.id,
                      assetType: asset.type,
                      filePath: asset.filePath,
                      duration: asset.duration,
                    })
                  );
                  onAssetDragStart?.(asset);
                }}
                onDragEnd={onAssetDragEnd}
                onRemove={() => onRemoveAsset?.(asset.id)}
                onRevealInFinder={() => handleRevealInFinder(asset)}
                onClick={() => handleAssetClick(asset)}
                onContextMenu={(e) => handleContextMenu(e, asset)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Preview Modal */}
      <AssetPreviewModal
        isOpen={showPreviewModal}
        onClose={() => setShowPreviewModal(false)}
        asset={previewAsset}
      />

      {/* Properties Dialog */}
      <AssetPropertiesDialog
        isOpen={showPropertiesDialog}
        onClose={() => setShowPropertiesDialog(false)}
        onSave={handlePropertiesSave}
        asset={propertiesAsset}
      />
    </div>
  );
};
