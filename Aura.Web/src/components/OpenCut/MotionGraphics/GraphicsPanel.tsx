/**
 * GraphicsPanel Component
 *
 * Graphics browser panel with category tabs, search functionality,
 * and drag-to-timeline support for motion graphics assets.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  mergeClasses,
  TabList,
  Tab,
} from '@fluentui/react-components';
import {
  Search24Regular,
  TextEffects24Regular,
  ArrowTrendingLines24Regular,
  Heart24Regular,
  TextT24Regular,
  ShapeOrganic24Regular,
  Apps24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import { useMotionGraphicsStore, BUILTIN_GRAPHICS } from '../../../stores/opencutMotionGraphics';
import { useOpenCutPlaybackStore } from '../../../stores/opencutPlayback';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { openCutTokens } from '../../../styles/designTokens';
import type { GraphicCategory } from '../../../types/motionGraphics';
import { EmptyState } from '../EmptyState';
import { GraphicCard } from './GraphicCard';

export interface GraphicsPanelProps {
  className?: string;
  onGraphicSelect?: (assetId: string) => void;
}

type CategoryTab = 'all' | GraphicCategory;

/** Category metadata with icons and labels */
const CATEGORY_CONFIG: Record<CategoryTab, { label: string; icon: JSX.Element }> = {
  all: { label: 'All', icon: <Apps24Regular /> },
  'lower-thirds': { label: 'Lower Thirds', icon: <TextEffects24Regular /> },
  callouts: { label: 'Callouts', icon: <ArrowTrendingLines24Regular /> },
  social: { label: 'Social', icon: <Heart24Regular /> },
  titles: { label: 'Titles', icon: <TextT24Regular /> },
  shapes: { label: 'Shapes', icon: <ShapeOrganic24Regular /> },
  'kinetic-text': { label: 'Kinetic', icon: <TextT24Regular /> },
  overlays: { label: 'Overlays', icon: <Apps24Regular /> },
  transitions: { label: 'Transitions', icon: <Apps24Regular /> },
  badges: { label: 'Badges', icon: <Apps24Regular /> },
  counters: { label: 'Counters', icon: <Apps24Regular /> },
};

/** Categories to show in tabs (only ones with content) */
const VISIBLE_CATEGORIES: CategoryTab[] = [
  'all',
  'lower-thirds',
  'callouts',
  'social',
  'titles',
  'shapes',
];

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
  tabList: {
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
  },
  tab: {
    minWidth: 'auto',
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    fontSize: tokens.fontSizeBase200,
  },
  content: {
    flex: 1,
    overflow: 'auto',
    padding: tokens.spacingHorizontalM,
  },
  graphicsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
    gap: tokens.spacingHorizontalS,
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalL,
  },
  categoryHeader: {
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
});

export const GraphicsPanel: FC<GraphicsPanelProps> = ({ className, onGraphicSelect }) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<CategoryTab>('all');
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(null);

  const graphicsStore = useMotionGraphicsStore();
  const timelineStore = useOpenCutTimelineStore();
  const playbackStore = useOpenCutPlaybackStore();

  // Filter graphics based on search and category
  const filteredGraphics = useMemo(() => {
    return BUILTIN_GRAPHICS.filter((asset) => {
      // Filter by category
      if (selectedCategory !== 'all' && asset.category !== selectedCategory) {
        return false;
      }

      // Filter by search query
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          asset.name.toLowerCase().includes(query) ||
          asset.description.toLowerCase().includes(query) ||
          asset.tags.some((tag) => tag.toLowerCase().includes(query))
        );
      }

      return true;
    });
  }, [searchQuery, selectedCategory]);

  // Group graphics by category when showing all
  const groupedGraphics = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredGraphics };
    }

    const groups: Record<string, typeof filteredGraphics> = {};
    filteredGraphics.forEach((asset) => {
      if (!groups[asset.category]) {
        groups[asset.category] = [];
      }
      groups[asset.category].push(asset);
    });
    return groups;
  }, [filteredGraphics, selectedCategory]);

  const handleCategoryChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedCategory(data.value as CategoryTab);
  }, []);

  const handleAssetClick = useCallback(
    (assetId: string) => {
      setSelectedAssetId(assetId);
      onGraphicSelect?.(assetId);
    },
    [onGraphicSelect]
  );

  const handleAddGraphic = useCallback(
    (assetId: string) => {
      // Find or create a graphics track
      let graphicsTrack = timelineStore.tracks.find((t) => t.type === 'video');

      if (!graphicsTrack) {
        // If no video track exists, add one
        const newTrackId = timelineStore.addTrack('video', 'Graphics');
        graphicsTrack = timelineStore.tracks.find((t) => t.id === newTrackId);
      }

      if (graphicsTrack) {
        const currentTime = playbackStore.currentTime;
        graphicsStore.addGraphic(assetId, graphicsTrack.id, currentTime);
      }
    },
    [graphicsStore, timelineStore, playbackStore]
  );

  // Get available categories that have graphics
  const availableCategories = useMemo(() => {
    const categoriesWithContent = new Set(BUILTIN_GRAPHICS.map((g) => g.category));
    return VISIBLE_CATEGORIES.filter(
      (cat) => cat === 'all' || categoriesWithContent.has(cat as GraphicCategory)
    );
  }, []);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <TextEffects24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Graphics
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            ({BUILTIN_GRAPHICS.length})
          </Text>
        </div>
      </div>

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search graphics..."
          size="small"
          value={searchQuery}
          onChange={(_, data) => setSearchQuery(data.value)}
        />
        <TabList
          className={styles.tabList}
          selectedValue={selectedCategory}
          onTabSelect={handleCategoryChange}
          size="small"
        >
          {availableCategories.map((cat) => (
            <Tab key={cat} value={cat} className={styles.tab}>
              {CATEGORY_CONFIG[cat].label}
            </Tab>
          ))}
        </TabList>
      </div>

      <div className={styles.content}>
        {filteredGraphics.length === 0 ? (
          <EmptyState
            icon={<TextEffects24Regular />}
            title="No graphics found"
            description={
              searchQuery ? 'Try a different search term' : 'No graphics available in this category'
            }
            size="small"
          />
        ) : selectedCategory === 'all' ? (
          // Show grouped by category
          Object.entries(groupedGraphics).map(([category, graphics]) => (
            <div key={category} className={styles.categorySection}>
              <Text weight="semibold" size={200} className={styles.categoryHeader}>
                {CATEGORY_CONFIG[category as CategoryTab]?.label || category}
              </Text>
              <div className={styles.graphicsGrid}>
                {graphics.map((asset) => (
                  <GraphicCard
                    key={asset.id}
                    asset={asset}
                    isSelected={selectedAssetId === asset.id}
                    onClick={() => handleAssetClick(asset.id)}
                    onDoubleClick={() => handleAddGraphic(asset.id)}
                    onAdd={() => handleAddGraphic(asset.id)}
                  />
                ))}
              </div>
            </div>
          ))
        ) : (
          // Show flat list for specific category
          <div className={styles.graphicsGrid}>
            {filteredGraphics.map((asset) => (
              <GraphicCard
                key={asset.id}
                asset={asset}
                isSelected={selectedAssetId === asset.id}
                onClick={() => handleAssetClick(asset.id)}
                onDoubleClick={() => handleAddGraphic(asset.id)}
                onAdd={() => handleAddGraphic(asset.id)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default GraphicsPanel;
