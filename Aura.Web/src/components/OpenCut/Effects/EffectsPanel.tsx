/**
 * EffectsPanel Component
 *
 * Effects browser panel with category tabs, search functionality,
 * and drag-to-timeline support for applying effects to clips.
 */

import {
  makeStyles,
  tokens,
  Text,
  Input,
  mergeClasses,
  TabList,
  Tab,
  Tooltip,
  Button,
} from '@fluentui/react-components';
import { Search24Regular, Color24Regular, Add24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import {
  BUILTIN_EFFECTS,
  useOpenCutEffectsStore,
  type EffectDefinition,
} from '../../../stores/opencutEffects';
import { useOpenCutTimelineStore } from '../../../stores/opencutTimeline';
import { openCutTokens } from '../../../styles/designTokens';
import type { EffectCategory } from '../../../types/opencut';
import { EmptyState } from '../EmptyState';

export interface EffectsPanelProps {
  className?: string;
  onEffectSelect?: (effectId: string) => void;
}

type CategoryTab = 'all' | EffectCategory;

const CATEGORY_LABELS: Record<CategoryTab, string> = {
  all: 'All',
  color: 'Color',
  blur: 'Blur',
  distort: 'Distort',
  stylize: 'Stylize',
  keying: 'Keying',
  motion: 'Motion',
  audio: 'Audio',
  custom: 'Custom',
};

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
  effectsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))',
    gap: tokens.spacingHorizontalS,
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalL,
  },
  categoryHeader: {
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
  effectCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    cursor: 'pointer',
    transition: 'background-color 0.15s ease, transform 0.15s ease',
    border: `1px solid transparent`,
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      transform: 'translateY(-2px)',
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },
  effectCardSelected: {
    border: `1px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorNeutralBackground3Hover,
  },
  effectIcon: {
    width: '48px',
    height: '48px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: tokens.colorNeutralBackground4,
    borderRadius: tokens.borderRadiusSmall,
    marginBottom: tokens.spacingVerticalXS,
    color: tokens.colorNeutralForeground3,
    fontSize: '24px',
  },
  effectIconColor: {
    backgroundColor: 'rgba(59, 130, 246, 0.2)',
    color: '#3B82F6',
  },
  effectIconBlur: {
    backgroundColor: 'rgba(139, 92, 246, 0.2)',
    color: '#8B5CF6',
  },
  effectIconStylize: {
    backgroundColor: 'rgba(236, 72, 153, 0.2)',
    color: '#EC4899',
  },
  effectName: {
    fontSize: tokens.fontSizeBase200,
    textAlign: 'center',
    wordBreak: 'break-word',
  },
  effectDescription: {
    fontSize: tokens.fontSizeBase100,
    color: tokens.colorNeutralForeground3,
    textAlign: 'center',
    marginTop: tokens.spacingVerticalXXS,
  },
  addButton: {
    position: 'absolute',
    top: '4px',
    right: '4px',
    opacity: 0,
    transition: 'opacity 0.15s ease',
  },
  effectCardWrapper: {
    position: 'relative',
    ':hover .add-button': {
      opacity: 1,
    },
  },
});

const EffectThumbnail: FC<{
  effect: EffectDefinition;
  onClick: () => void;
  onApply: () => void;
  isSelected?: boolean;
}> = ({ effect, onClick, onApply, isSelected }) => {
  const styles = useStyles();

  const getCategoryIconClass = (category: EffectCategory): string => {
    switch (category) {
      case 'color':
        return styles.effectIconColor;
      case 'blur':
        return styles.effectIconBlur;
      case 'stylize':
        return styles.effectIconStylize;
      default:
        return '';
    }
  };

  const handleApplyClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onApply();
  };

  return (
    <div className={styles.effectCardWrapper}>
      <Tooltip content={effect.description} relationship="description" positioning="below">
        <div
          className={mergeClasses(styles.effectCard, isSelected && styles.effectCardSelected)}
          onClick={onClick}
          onDoubleClick={onApply}
          role="button"
          tabIndex={0}
          aria-label={`${effect.name} effect`}
        >
          <div className={mergeClasses(styles.effectIcon, getCategoryIconClass(effect.category))}>
            <Color24Regular />
          </div>
          <Text className={styles.effectName}>{effect.name}</Text>
        </div>
      </Tooltip>
      <Tooltip content="Apply to selected clip" relationship="label">
        <Button
          className={mergeClasses(styles.addButton, 'add-button')}
          appearance="subtle"
          size="small"
          icon={<Add24Regular />}
          onClick={handleApplyClick}
          aria-label={`Apply ${effect.name} to selected clip`}
        />
      </Tooltip>
    </div>
  );
};

export const EffectsPanel: FC<EffectsPanelProps> = ({ className, onEffectSelect }) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<CategoryTab>('all');
  const [selectedEffectId, setSelectedEffectId] = useState<string | null>(null);

  const effectsStore = useOpenCutEffectsStore();
  const timelineStore = useOpenCutTimelineStore();

  // Filter effects based on search and category
  const filteredEffects = useMemo(() => {
    return BUILTIN_EFFECTS.filter((e) => {
      // Filter by category
      if (selectedCategory !== 'all' && e.category !== selectedCategory) {
        return false;
      }

      // Filter by search query
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          e.name.toLowerCase().includes(query) ||
          e.description.toLowerCase().includes(query) ||
          e.category.toLowerCase().includes(query)
        );
      }

      return true;
    });
  }, [searchQuery, selectedCategory]);

  // Group effects by category when showing all
  const groupedEffects = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredEffects };
    }

    const groups: Record<string, EffectDefinition[]> = {};
    filteredEffects.forEach((e) => {
      if (!groups[e.category]) {
        groups[e.category] = [];
      }
      groups[e.category].push(e);
    });
    return groups;
  }, [filteredEffects, selectedCategory]);

  const handleCategoryChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedCategory(data.value as CategoryTab);
  }, []);

  const handleEffectClick = useCallback(
    (effect: EffectDefinition) => {
      setSelectedEffectId(effect.id);
      onEffectSelect?.(effect.id);
    },
    [onEffectSelect]
  );

  const handleApplyEffect = useCallback(
    (effectId: string) => {
      const selectedClips = timelineStore.getSelectedClips();
      if (selectedClips.length > 0) {
        // Apply to first selected clip
        effectsStore.applyEffect(effectId, selectedClips[0].id);
      }
    },
    [effectsStore, timelineStore]
  );

  // Get categories that have effects
  const availableCategories = useMemo(() => {
    const cats = new Set(BUILTIN_EFFECTS.map((e) => e.category));
    return ['all', ...Array.from(cats)] as CategoryTab[];
  }, []);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Color24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Effects
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            ({BUILTIN_EFFECTS.length})
          </Text>
        </div>
      </div>

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search effects..."
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
              {CATEGORY_LABELS[cat]}
            </Tab>
          ))}
        </TabList>
      </div>

      <div className={styles.content}>
        {filteredEffects.length === 0 ? (
          <EmptyState
            icon={<Color24Regular />}
            title="No effects found"
            description={
              searchQuery ? 'Try a different search term' : 'No effects available in this category'
            }
            size="small"
          />
        ) : selectedCategory === 'all' ? (
          // Show grouped by category
          Object.entries(groupedEffects).map(([category, effects]) => (
            <div key={category} className={styles.categorySection}>
              <Text weight="semibold" size={200} className={styles.categoryHeader}>
                {CATEGORY_LABELS[category as CategoryTab] || category}
              </Text>
              <div className={styles.effectsGrid}>
                {effects.map((effect) => (
                  <EffectThumbnail
                    key={effect.id}
                    effect={effect}
                    onClick={() => handleEffectClick(effect)}
                    onApply={() => handleApplyEffect(effect.id)}
                    isSelected={selectedEffectId === effect.id}
                  />
                ))}
              </div>
            </div>
          ))
        ) : (
          // Show flat list for specific category
          <div className={styles.effectsGrid}>
            {filteredEffects.map((effect) => (
              <EffectThumbnail
                key={effect.id}
                effect={effect}
                onClick={() => handleEffectClick(effect)}
                onApply={() => handleApplyEffect(effect.id)}
                isSelected={selectedEffectId === effect.id}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default EffectsPanel;
