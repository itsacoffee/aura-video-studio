/**
 * TransitionsPanel Component
 *
 * Transition browser panel with category tabs, search functionality,
 * and drag-to-timeline support for applying transitions.
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
import { Search24Regular, Blur24Regular } from '@fluentui/react-icons';
import { useState, useCallback, useMemo } from 'react';
import type { FC } from 'react';
import {
  BUILTIN_TRANSITIONS,
  type ExtendedTransitionCategory,
  type ExtendedTransitionDefinition,
} from '../../../stores/opencutTransitions';
import { openCutTokens } from '../../../styles/designTokens';
import { EmptyState } from '../EmptyState';
import { TransitionThumbnail } from './TransitionThumbnail';

export interface TransitionsPanelProps {
  className?: string;
  onTransitionSelect?: (transitionId: string) => void;
}

type CategoryTab = 'all' | ExtendedTransitionCategory;

const CATEGORY_LABELS: Record<CategoryTab, string> = {
  all: 'All',
  basic: 'Basic',
  dissolve: 'Dissolve',
  wipe: 'Wipe',
  slide: 'Slide',
  zoom: 'Zoom',
  '3d': '3D',
  blur: 'Blur',
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
  transitionsGrid: {
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
  emptyMessage: {
    textAlign: 'center',
    padding: tokens.spacingVerticalL,
    color: tokens.colorNeutralForeground3,
  },
});

export const TransitionsPanel: FC<TransitionsPanelProps> = ({ className, onTransitionSelect }) => {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<CategoryTab>('all');

  // Filter transitions based on search and category
  const filteredTransitions = useMemo(() => {
    return BUILTIN_TRANSITIONS.filter((t) => {
      // Filter by category
      if (selectedCategory !== 'all' && t.category !== selectedCategory) {
        return false;
      }

      // Filter by search query
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          t.name.toLowerCase().includes(query) ||
          t.description.toLowerCase().includes(query) ||
          t.category.toLowerCase().includes(query)
        );
      }

      return true;
    });
  }, [searchQuery, selectedCategory]);

  // Group transitions by category when showing all
  const groupedTransitions = useMemo(() => {
    if (selectedCategory !== 'all') {
      return { [selectedCategory]: filteredTransitions };
    }

    const groups: Record<string, ExtendedTransitionDefinition[]> = {};
    filteredTransitions.forEach((t) => {
      if (!groups[t.category]) {
        groups[t.category] = [];
      }
      groups[t.category].push(t);
    });
    return groups;
  }, [filteredTransitions, selectedCategory]);

  const handleCategoryChange = useCallback((_: unknown, data: { value: string }) => {
    setSelectedCategory(data.value as CategoryTab);
  }, []);

  const handleTransitionClick = useCallback(
    (transition: ExtendedTransitionDefinition) => {
      onTransitionSelect?.(transition.id);
    },
    [onTransitionSelect]
  );

  // Get categories that have transitions
  const availableCategories = useMemo(() => {
    const cats = new Set(BUILTIN_TRANSITIONS.map((t) => t.category));
    return ['all', ...Array.from(cats)] as CategoryTab[];
  }, []);

  return (
    <div className={mergeClasses(styles.container, className)}>
      <div className={styles.header}>
        <div className={styles.headerTitle}>
          <Blur24Regular className={styles.headerIcon} />
          <Text weight="semibold" size={400}>
            Transitions
          </Text>
          <Text size={200} style={{ color: tokens.colorNeutralForeground3 }}>
            ({BUILTIN_TRANSITIONS.length})
          </Text>
        </div>
      </div>

      <div className={styles.toolbar}>
        <Input
          className={styles.searchInput}
          contentBefore={<Search24Regular />}
          placeholder="Search transitions..."
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
        {filteredTransitions.length === 0 ? (
          <EmptyState
            icon={<Blur24Regular />}
            title="No transitions found"
            description={
              searchQuery
                ? 'Try a different search term'
                : 'No transitions available in this category'
            }
            size="small"
          />
        ) : selectedCategory === 'all' ? (
          // Show grouped by category
          Object.entries(groupedTransitions).map(([category, transitions]) => (
            <div key={category} className={styles.categorySection}>
              <Text weight="semibold" size={200} className={styles.categoryHeader}>
                {CATEGORY_LABELS[category as CategoryTab] || category}
              </Text>
              <div className={styles.transitionsGrid}>
                {transitions.map((transition) => (
                  <TransitionThumbnail
                    key={transition.id}
                    transition={transition}
                    onClick={() => handleTransitionClick(transition)}
                  />
                ))}
              </div>
            </div>
          ))
        ) : (
          // Show flat list for specific category
          <div className={styles.transitionsGrid}>
            {filteredTransitions.map((transition) => (
              <TransitionThumbnail
                key={transition.id}
                transition={transition}
                onClick={() => handleTransitionClick(transition)}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default TransitionsPanel;
