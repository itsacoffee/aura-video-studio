import { useState, useRef, useMemo, useCallback } from 'react';
import { Virtuoso } from 'react-virtuoso';
import {
  makeStyles,
  tokens,
  Text,
  Button,
  ToolbarButton,
} from '@fluentui/react-components';
import {
  Add24Regular,
  Grid24Regular,
  List24Regular,
  Apps24Regular,
} from '@fluentui/react-icons';
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
}

interface ProjectBinProps {
  assets: MediaAsset[];
  onAddAssets?: () => void;
  onRemoveAsset?: (assetId: string) => void;
  onAssetDragStart?: (asset: MediaAsset) => void;
  onAssetDragEnd?: () => void;
  onAssetSelect?: (asset: MediaAsset) => void;
}

type ViewMode = 'thumbnail' | 'list' | 'grid';

export const ProjectBin: React.FC<ProjectBinProps> = ({
  assets,
  onAddAssets,
  onRemoveAsset,
  onAssetDragStart,
  onAssetDragEnd,
  onAssetSelect,
}) => {
  const styles = useStyles();
  const [viewMode, setViewMode] = useState<ViewMode>('thumbnail');
  const [selectedCollection, setSelectedCollection] = useState<string>('all');
  const virtuosoRef = useRef(null);

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
    selectedCollection === 'all'
      ? assets
      : assets.filter((a) => a.type === selectedCollection);

  const handleRevealInFinder = useCallback((asset: MediaAsset) => {
    // In a real implementation, this would call a native API to reveal the file
    console.log('Reveal in finder:', asset.filePath || asset.file.name);
    // For demo purposes, show an alert with the path
    // This would be replaced with a proper notification system
    alert(`File location: ${asset.filePath || 'Not available'}`);
  }, []);

  const handleAssetClick = useCallback((asset: MediaAsset) => {
    onAssetSelect?.(asset);
  }, [onAssetSelect]);

  // Memoize item renderer for virtual scrolling
  const renderAsset = useCallback((index: number) => {
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
        onDragStart={(e) => {
          e.dataTransfer.effectAllowed = 'copy';
          e.dataTransfer.setData('application/json', JSON.stringify(asset));
          onAssetDragStart?.(asset);
        }}
        onDragEnd={onAssetDragEnd}
        onRemove={() => onRemoveAsset?.(asset.id)}
        onRevealInFinder={() => handleRevealInFinder(asset)}
        onClick={() => handleAssetClick(asset)}
      />
    );
  }, [filteredAssets, onAssetDragStart, onAssetDragEnd, onRemoveAsset, handleRevealInFinder, handleAssetClick]);

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
            <Button
              appearance="primary"
              icon={<Add24Regular />}
              onClick={onAddAssets}
            >
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
          <div className={viewMode === 'list' ? styles.mediaList : styles.mediaGrid} style={{ overflow: 'auto', height: '100%' }}>
            {filteredAssets.map((asset) => (
              <MediaThumbnail
                key={asset.id}
                id={asset.id}
                name={asset.name}
                type={asset.type}
                preview={asset.preview}
                duration={asset.duration}
                fileSize={asset.fileSize}
                onDragStart={(e) => {
                  e.dataTransfer.effectAllowed = 'copy';
                  e.dataTransfer.setData('application/json', JSON.stringify(asset));
                  onAssetDragStart?.(asset);
                }}
                onDragEnd={onAssetDragEnd}
                onRemove={() => onRemoveAsset?.(asset.id)}
                onRevealInFinder={() => handleRevealInFinder(asset)}
                onClick={() => handleAssetClick(asset)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
