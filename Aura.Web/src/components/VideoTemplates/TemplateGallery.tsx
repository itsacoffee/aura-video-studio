/**
 * TemplateGallery - Displays a grid of video structure templates
 * Allows users to browse, search, and select templates for script generation
 */

import {
  makeStyles,
  tokens,
  Title2,
  Text,
  SearchBox,
  Dropdown,
  Option,
  Button,
  Card,
  Badge,
  Spinner,
} from '@fluentui/react-components';
import { ArrowLeft24Regular } from '@fluentui/react-icons';
import React, { useState, useEffect, useMemo } from 'react';
import { apiUrl } from '../../config/api';
import type { VideoTemplate } from '../../types/videoTemplates';
import { TemplateIconMap, formatDuration, getCategoryColor } from '../../types/videoTemplates';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
    padding: tokens.spacingHorizontalL,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalM,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  filters: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  searchBox: {
    minWidth: '250px',
  },
  categoryDropdown: {
    minWidth: '150px',
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingHorizontalL,
  },
  templateCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'transform 0.2s ease, box-shadow 0.2s ease',
    ':hover': {
      transform: 'translateY(-4px)',
      boxShadow: tokens.shadow16,
    },
  },
  cardHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  iconContainer: {
    width: '48px',
    height: '48px',
    borderRadius: tokens.borderRadiusMedium,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '24px',
  },
  cardTitleContainer: {
    flex: 1,
  },
  cardTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    marginBottom: '4px',
  },
  cardCategory: {
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  cardDescription: {
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase300,
    marginBottom: tokens.spacingVerticalM,
    display: '-webkit-box',
    WebkitLineClamp: 2,
    WebkitBoxOrient: 'vertical',
    overflow: 'hidden',
  },
  cardFooter: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cardMeta: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
  },
  tags: {
    display: 'flex',
    gap: tokens.spacingHorizontalXS,
    flexWrap: 'wrap',
    marginTop: tokens.spacingVerticalS,
  },
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    padding: tokens.spacingVerticalXXL,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    textAlign: 'center',
    color: tokens.colorNeutralForeground3,
  },
});

interface TemplateGalleryProps {
  onSelectTemplate: (template: VideoTemplate) => void;
  onBack?: () => void;
}

const ALL_CATEGORIES = 'All';

export function TemplateGallery({ onSelectTemplate, onBack }: TemplateGalleryProps) {
  const styles = useStyles();
  const [templates, setTemplates] = useState<VideoTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>(ALL_CATEGORIES);

  // Fetch templates on mount
  useEffect(() => {
    async function fetchTemplates() {
      try {
        setLoading(true);
        const response = await fetch(apiUrl('/api/video-templates'));
        if (!response.ok) {
          throw new Error('Failed to fetch templates');
        }
        const data = await response.json();
        setTemplates(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load templates');
      } finally {
        setLoading(false);
      }
    }

    fetchTemplates();
  }, []);

  // Get unique categories
  const categories = useMemo(() => {
    const cats = new Set(templates.map((t) => t.category));
    return [ALL_CATEGORIES, ...Array.from(cats)];
  }, [templates]);

  // Filter templates based on search and category
  const filteredTemplates = useMemo(() => {
    return templates.filter((template) => {
      // Category filter
      if (selectedCategory !== ALL_CATEGORIES && template.category !== selectedCategory) {
        return false;
      }

      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        return (
          template.name.toLowerCase().includes(query) ||
          template.description.toLowerCase().includes(query) ||
          template.metadata.tags.some((tag) => tag.toLowerCase().includes(query))
        );
      }

      return true;
    });
  }, [templates, selectedCategory, searchQuery]);

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <Spinner label="Loading templates..." />
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.emptyState}>
        <Text size={400}>Error loading templates</Text>
        <Text size={300}>{error}</Text>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          {onBack && (
            <Button icon={<ArrowLeft24Regular />} appearance="subtle" onClick={onBack}>
              Back
            </Button>
          )}
          <Title2>Choose a Template</Title2>
        </div>
        <div className={styles.filters}>
          <SearchBox
            className={styles.searchBox}
            placeholder="Search templates..."
            value={searchQuery}
            onChange={(_, data) => setSearchQuery(data.value || '')}
          />
          <Dropdown
            className={styles.categoryDropdown}
            value={selectedCategory}
            onOptionSelect={(_, data) => setSelectedCategory(data.optionValue as string)}
          >
            {categories.map((cat) => (
              <Option key={cat} value={cat}>
                {cat}
              </Option>
            ))}
          </Dropdown>
        </div>
      </div>

      {filteredTemplates.length === 0 ? (
        <div className={styles.emptyState}>
          <Text size={400}>No templates found</Text>
          <Text size={300}>Try adjusting your search or filters</Text>
        </div>
      ) : (
        <div className={styles.grid}>
          {filteredTemplates.map((template) => (
            <Card
              key={template.id}
              className={styles.templateCard}
              onClick={() => onSelectTemplate(template)}
            >
              <div className={styles.cardHeader}>
                <div
                  className={styles.iconContainer}
                  style={{
                    backgroundColor:
                      template.thumbnail?.accentColor || getCategoryColor(template.category),
                    color: 'white',
                  }}
                >
                  {template.thumbnail ? TemplateIconMap[template.thumbnail.iconName] || '📄' : '📄'}
                </div>
                <div className={styles.cardTitleContainer}>
                  <Text className={styles.cardTitle}>{template.name}</Text>
                  <Text className={styles.cardCategory}>{template.category}</Text>
                </div>
              </div>

              <Text className={styles.cardDescription}>{template.description}</Text>

              <div className={styles.cardFooter}>
                <div className={styles.cardMeta}>
                  <Text>⏱️ {formatDuration(template.structure.estimatedDurationSeconds)}</Text>
                  <Text>•</Text>
                  <Text>📹 {template.structure.recommendedSceneCount} scenes</Text>
                </div>
              </div>

              <div className={styles.tags}>
                {template.metadata.tags.slice(0, 3).map((tag) => (
                  <Badge key={tag} size="small" appearance="tint">
                    {tag}
                  </Badge>
                ))}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

export default TemplateGallery;
