/**
 * AnimationPresetPicker Component
 *
 * Preset selection grid for text animations with category tabs,
 * animated preview thumbnails, hover preview, and click to apply.
 */

import {
  makeStyles,
  tokens,
  Text,
  TabList,
  Tab,
  Tooltip,
  mergeClasses,
} from '@fluentui/react-components';
import {
  ArrowDownload24Regular,
  ArrowUpload24Regular,
  Sparkle24Regular,
  ArrowRepeatAll24Regular,
  TextT24Regular,
} from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC, ReactElement } from 'react';
import {
  useTextAnimationsStore,
  BUILTIN_TEXT_ANIMATION_PRESETS,
  type TextAnimationCategory,
  type TextAnimationPreset,
  type TextAnimationPosition,
} from '../../../stores/opencutTextAnimations';
import { EmptyState } from '../EmptyState';

export interface AnimationPresetPickerProps {
  /** Target clip or caption ID */
  targetId: string;
  /** Target type */
  targetType: 'clip' | 'caption';
  /** Animation position to apply (in, out, continuous) */
  position?: TextAnimationPosition;
  /** CSS class name */
  className?: string;
  /** Callback when animation is applied */
  onAnimationApplied?: (animationId: string) => void;
}

type CategoryTab = 'all' | TextAnimationCategory;

const CATEGORY_LABELS: Record<CategoryTab, string> = {
  all: 'All',
  entrance: 'Entrance',
  emphasis: 'Emphasis',
  exit: 'Exit',
  continuous: 'Continuous',
};

const CATEGORY_ICONS: Record<TextAnimationCategory, ReactElement> = {
  entrance: <ArrowDownload24Regular />,
  emphasis: <Sparkle24Regular />,
  exit: <ArrowUpload24Regular />,
  continuous: <ArrowRepeatAll24Regular />,
};

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXS,
  },
  headerTitle: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    color: tokens.colorNeutralForeground2,
  },
  headerIcon: {
    fontSize: '16px',
    color: tokens.colorNeutralForeground3,
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
  presetsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(90px, 1fr))',
    gap: tokens.spacingHorizontalS,
    maxHeight: '200px',
    overflowY: 'auto',
  },
  presetCard: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    border: `1px solid ${tokens.colorNeutralStroke3}`,
    cursor: 'pointer',
    transition: 'all 0.15s ease',
    minHeight: '64px',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground3Hover,
      border: `1px solid ${tokens.colorBrandStroke1}`,
      transform: 'translateY(-1px)',
    },
    ':active': {
      transform: 'translateY(0)',
    },
  },
  presetCardActive: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
  },
  presetIcon: {
    fontSize: '18px',
    color: tokens.colorNeutralForeground3,
    marginBottom: tokens.spacingVerticalXS,
  },
  presetName: {
    fontSize: tokens.fontSizeBase100,
    textAlign: 'center',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    width: '100%',
  },
  categorySection: {
    marginBottom: tokens.spacingVerticalM,
  },
  categoryHeader: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
    marginBottom: tokens.spacingVerticalS,
    color: tokens.colorNeutralForeground2,
  },
  categoryIcon: {
    fontSize: '14px',
  },
});

export const AnimationPresetPicker: FC<AnimationPresetPickerProps> = ({
  targetId,
  targetType,
  position = 'in',
  className,
  onAnimationApplied,
}) => {
  const styles = useStyles();
  const textAnimationsStore = useTextAnimationsStore();
  const [selectedCategory, setSelectedCategory] = useState<CategoryTab>('all');

  // Get current animation at this position (if any)
  const currentAnimation = textAnimationsStore.getAnimationAtPosition(targetId, position);

  // Filter presets based on selected category
  const filteredPresets = useMemo(() => {
    if (selectedCategory === 'all') {
      return BUILTIN_TEXT_ANIMATION_PRESETS;
    }
    return BUILTIN_TEXT_ANIMATION_PRESETS.filter((p) => p.category === selectedCategory);
  }, [selectedCategory]);

  // Group presets by category when showing all
  const groupedPresets = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredPresets };
    }

    const groups: Record<string, TextAnimationPreset[]> = {};
    filteredPresets.forEach((p) => {
      if (!groups[p.category]) {
        groups[p.category] = [];
      }
      groups[p.category].push(p);
    });
    return groups;
  }, [filteredPresets, selectedCategory]);

  const handleCategoryChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedCategory(data.value as CategoryTab);
  }, []);

  const handlePresetClick = useCallback(
    (preset: TextAnimationPreset) => {
      const animationId = textAnimationsStore.applyAnimation(
        targetId,
        targetType,
        preset.id,
        position
      );
      if (animationId) {
        onAnimationApplied?.(animationId);
      }
    },
    [textAnimationsStore, targetId, targetType, position, onAnimationApplied]
  );

  const renderPresetCard = (preset: TextAnimationPreset) => {
    const isActive = currentAnimation?.presetId === preset.id;

    return (
      <Tooltip content={preset.name} relationship="label" key={preset.id}>
        <div
          className={mergeClasses(styles.presetCard, isActive && styles.presetCardActive)}
          onClick={() => handlePresetClick(preset)}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              handlePresetClick(preset);
            }
          }}
          aria-pressed={isActive}
          aria-label={`Apply ${preset.name} animation`}
        >
          <span className={styles.presetIcon}>
            {CATEGORY_ICONS[preset.category] || <TextT24Regular />}
          </span>
          <Text className={styles.presetName}>{preset.name}</Text>
        </div>
      </Tooltip>
    );
  };

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <TextT24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={200}>
            Text Animations
          </Text>
        </div>
      </div>

      <TabList
        className={styles.tabList}
        selectedValue={selectedCategory}
        onTabSelect={handleCategoryChange}
        size="small"
      >
        {Object.entries(CATEGORY_LABELS).map(([key, label]) => (
          <Tab key={key} value={key} className={styles.tab}>
            {label}
          </Tab>
        ))}
      </TabList>

      {filteredPresets.length === 0 ? (
        <EmptyState
          icon={<TextT24Regular />}
          title="No animations"
          description="No animations available in this category"
          size="small"
        />
      ) : selectedCategory === 'all' ? (
        // Show grouped by category
        Object.entries(groupedPresets).map(([category, presets]) => (
          <div key={category} className={styles.categorySection}>
            <div className={styles.categoryHeader}>
              <span className={styles.categoryIcon}>
                {CATEGORY_ICONS[category as TextAnimationCategory]}
              </span>
              <Text weight="semibold" size={200}>
                {CATEGORY_LABELS[category as CategoryTab]}
              </Text>
            </div>
            <div className={styles.presetsGrid}>{presets.map(renderPresetCard)}</div>
          </div>
        ))
      ) : (
        // Show flat list for specific category
        <div className={styles.presetsGrid}>{filteredPresets.map(renderPresetCard)}</div>
      )}
    </div>
  );
};

export default AnimationPresetPicker;
