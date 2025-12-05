/**
 * GraphicsPanel Component
 *
 * Graphics browser panel with category tabs, search, grid display,
 * hover-to-preview, and drag-to-timeline support.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  TabList,
  Tab,
  mergeClasses,
} from '@fluentui/react-components';
import {
  Search24Regular,
  Sparkle24Regular,
  TextT24Regular,
  ShapeSubtract24Regular,
  Video24Regular,
  Heart24Regular,
  CursorClick24Regular,
} from '@fluentui/react-icons';
import { motion, AnimatePresence } from 'framer-motion';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore } from '../../../stores/opencutMotionGraphics';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { openCutTokens } from '../../../styles/designTokens';
import type { GraphicCategory } from '../../../types/motionGraphics';
import { EmptyState } from '../EmptyState';
import { GraphicCard } from './GraphicCard';

export interface GraphicsPanelProps {
  className?: string;
}

type TabValue = GraphicCategory | 'all';

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
  tabs: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '2px',
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  graphicsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  categorySection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  categoryTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground1,
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
  categoryCount: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
  },
  emptyMessage: {
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
});

const categoryIcons: Record<TabValue, React.ReactNode> = {
  all: <Sparkle24Regular />,
  'lower-thirds': <TextT24Regular />,
  callouts: <CursorClick24Regular />,
  social: <Heart24Regular />,
  titles: <TextT24Regular />,
  'kinetic-text': <TextT24Regular />,
  shapes: <ShapeSubtract24Regular />,
  overlays: <Video24Regular />,
  transitions: <Video24Regular />,
  badges: <Sparkle24Regular />,
  counters: <Sparkle24Regular />,
};

const categoryLabels: Record<TabValue, string> = {
  all: 'All',
  'lower-thirds': 'Lower Thirds',
  callouts: 'Callouts',
  social: 'Social',
  titles: 'Titles',
  'kinetic-text': 'Kinetic',
  shapes: 'Shapes',
  overlays: 'Overlays',
  transitions: 'Transitions',
  badges: 'Badges',
  counters: 'Counters',
};

export const GraphicsPanel: FC<GraphicsPanelProps> = ({ className }) => {
  const styles = useStyles();
  const [selectedTab, setSelectedTab] = useState<TabValue>('all');
  const [searchQuery, setSearchQuery] = useState('');

  const graphicsStore = useMotionGraphicsStore();
  const playbackStore = useOpenCutPlaybackStore();

  // Filter assets based on search and category
  const filteredAssets = useMemo(() => {
    let assets = graphicsStore.assets;

    // Filter by category
    if (selectedTab !== 'all') {
      assets = assets.filter((a) => a.category === selectedTab);
    }

    // Filter by search
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      assets = assets.filter(
        (a) =>
          a.name.toLowerCase().includes(query) ||
          a.description.toLowerCase().includes(query) ||
          a.tags.some((t) => t.toLowerCase().includes(query))
      );
    }

    return assets;
  }, [graphicsStore.assets, selectedTab, searchQuery]);

  // Group assets by category for "All" view
  const groupedAssets = useMemo(() => {
    if (selectedTab !== 'all') return null;

    const groups: Record<GraphicCategory, typeof filteredAssets> = {
      'lower-thirds': [],
      callouts: [],
      social: [],
      titles: [],
      'kinetic-text': [],
      shapes: [],
      overlays: [],
      transitions: [],
      badges: [],
      counters: [],
    };

    filteredAssets.forEach((asset) => {
      groups[asset.category].push(asset);
    });

    return Object.entries(groups).filter(([, assets]) => assets.length > 0);
  }, [filteredAssets, selectedTab]);

  const handleTabChange = useCallback((_: unknown, data: { value: unknown }) => {
    setSelectedTab(data.value as TabValue);
  }, []);

  const handleSearchChange = useCallback((_: unknown, data: { value: string }) => {
    setSearchQuery(data.value);
  }, []);

  const handleAddGraphic = useCallback(
    (assetId: string) => {
      // Add to a graphics track at the current playhead position
      const trackId = 'track-text-1'; // Use text track by default
      const startTime = playbackStore.currentTime;
      graphicsStore.addGraphic(assetId, trackId, startTime);
    },
    [graphicsStore, playbackStore]
  );

  const handleSelectGraphic = useCallback(
    (assetId: string) => {
      graphicsStore.setPreviewingAsset(assetId);
    },
    [graphicsStore]
  );

  const handlePreviewGraphic = useCallback(
    (assetId: string | null) => {
      graphicsStore.setPreviewingAsset(assetId);
    },
    [graphicsStore]
  );

  // Available categories that have graphics
  const availableCategories = useMemo(() => {
    const categories = new Set<GraphicCategory>();
    graphicsStore.assets.forEach((a) => categories.add(a.category));
    return Array.from(categories);
  }, [graphicsStore.assets]);

  return (
    <div className={mergeClasses(styles.container, className)}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Sparkle24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Graphics
          </Text>
          {filteredAssets.length > 0 && (
            <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
              ({filteredAssets.length})
            </Text>
          )}
        </div>
      </div>

      {/* Toolbar with search and category tabs */}
      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search graphics..."
          size="small"
          value={searchQuery}
          onChange={handleSearchChange}
        />
        <TabList
          selectedValue={selectedTab}
          onTabSelect={handleTabChange}
          size="small"
          className={styles.tabs}
        >
          <Tab value="all" icon={categoryIcons['all']}>
            All
          </Tab>
          {availableCategories.map((category) => (
            <Tab key={category} value={category} icon={categoryIcons[category]}>
              {categoryLabels[category]}
            </Tab>
          ))}
        </TabList>
      </div>

      {/* Content */}
      <div className={styles.content}>
        {filteredAssets.length === 0 ? (
          searchQuery ? (
            <div className={styles.emptyMessage}>
              <Text size={200}>No graphics match your search</Text>
            </div>
          ) : (
            <EmptyState
              icon={<Sparkle24Regular />}
              title="No graphics available"
              description="Graphics library is empty"
              size="medium"
            />
          )
        ) : selectedTab === 'all' && groupedAssets ? (
          // Grouped view for "All" tab
          <AnimatePresence mode="wait">
            {groupedAssets.map(([category, assets]) => (
              <motion.div
                key={category}
                className={styles.categorySection}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                transition={{ duration: 0.2 }}
              >
                <div className={styles.categoryTitle}>
                  {categoryIcons[category as GraphicCategory]}
                  {categoryLabels[category as GraphicCategory]}
                  <span className={styles.categoryCount}>({assets.length})</span>
                </div>
                <div className={styles.graphicsGrid}>
                  {assets.map((asset) => (
                    <GraphicCard
                      key={asset.id}
                      asset={asset}
                      isSelected={graphicsStore.previewingAssetId === asset.id}
                      onSelect={handleSelectGraphic}
                      onAdd={handleAddGraphic}
                      onPreview={handlePreviewGraphic}
                    />
                  ))}
                </div>
              </motion.div>
            ))}
          </AnimatePresence>
        ) : (
          // Flat grid view for specific category
          <motion.div
            className={styles.graphicsGrid}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ duration: 0.2 }}
          >
            {filteredAssets.map((asset) => (
              <GraphicCard
                key={asset.id}
                asset={asset}
                isSelected={graphicsStore.previewingAssetId === asset.id}
                onSelect={handleSelectGraphic}
                onAdd={handleAddGraphic}
                onPreview={handlePreviewGraphic}
              />
            ))}
          </motion.div>
        )}
      </div>
    </div>
  );
};

export default GraphicsPanel;
