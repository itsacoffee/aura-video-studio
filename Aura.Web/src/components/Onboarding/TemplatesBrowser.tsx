/**
 * Templates Browser Component
 * 
 * Allows users to browse and select video templates during first-run setup
 */

import {
  Card,
  makeStyles,
  tokens,
  Text,
  Title3,
  Button,
  Badge,
  Input,
  Dropdown,
  Option,
} from '@fluentui/react-components';
import {
  Video24Regular,
  Play24Regular,
  Info24Regular,
} from '@fluentui/react-icons';
import { useState, useMemo } from 'react';
import type { VideoTemplate } from '../../services/templatesAndSamplesService';
import {
  getVideoTemplates,
  getBeginnerTemplates,
  searchTemplates,
} from '../../services/templatesAndSamplesService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalL,
  },
  filterBar: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  templatesGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  templateCard: {
    padding: tokens.spacingVerticalL,
    cursor: 'pointer',
    transition: 'transform 0.2s ease, box-shadow 0.2s ease',
    ':hover': {
      transform: 'translateY(-2px)',
      boxShadow: tokens.shadow8,
    },
  },
  templateCardSelected: {
    borderColor: tokens.colorBrandStroke1,
    borderWidth: '2px',
  },
  templateHeader: {
    display: 'flex',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  templateIcon: {
    width: '32px',
    height: '32px',
    color: tokens.colorBrandForeground1,
  },
  templateInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  templateTags: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: tokens.spacingHorizontalXS,
    marginTop: tokens.spacingVerticalS,
  },
  templatePromptExample: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  selectedTemplateCard: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteBlueBackground1,
    borderLeft: `4px solid ${tokens.colorPaletteBlueBorder1}`,
  },
});

export interface TemplatesBrowserProps {
  onTemplateSelect?: (template: VideoTemplate) => void;
  selectedTemplateId?: string;
  showOnlyBeginner?: boolean;
}

export function TemplatesBrowser({
  onTemplateSelect,
  selectedTemplateId,
  showOnlyBeginner = false,
}: TemplatesBrowserProps) {
  const styles = useStyles();
  const [searchQuery, setSearchQuery] = useState('');
  const [difficultyFilter, setDifficultyFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');

  const allTemplates = useMemo(() => {
    return showOnlyBeginner ? getBeginnerTemplates() : getVideoTemplates();
  }, [showOnlyBeginner]);

  const filteredTemplates = useMemo(() => {
    let templates = allTemplates;

    // Apply search
    if (searchQuery) {
      templates = searchTemplates(searchQuery);
    }

    // Apply difficulty filter
    if (difficultyFilter !== 'all') {
      templates = templates.filter((t) => t.difficulty === difficultyFilter);
    }

    // Apply category filter
    if (categoryFilter !== 'all') {
      templates = templates.filter((t) => t.category === categoryFilter);
    }

    return templates;
  }, [allTemplates, searchQuery, difficultyFilter, categoryFilter]);

  const selectedTemplate = allTemplates.find((t) => t.id === selectedTemplateId);

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'beginner':
        return 'success';
      case 'intermediate':
        return 'warning';
      case 'advanced':
        return 'danger';
      default:
        return 'informative';
    }
  };

  const formatDuration = (seconds: number) => {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return secs > 0 ? `${minutes}m ${secs}s` : `${minutes}m`;
  };

  return (
    <div className={styles.container}>
      {!showOnlyBeginner && (
        <div className={styles.filterBar}>
          <Input
            placeholder="Search templates..."
            value={searchQuery}
            onChange={(_, data) => setSearchQuery(data.value)}
            style={{ flex: 1, minWidth: '200px' }}
          />
          <Dropdown
            placeholder="Difficulty"
            value={difficultyFilter}
            selectedOptions={[difficultyFilter]}
            onOptionSelect={(_, data) => setDifficultyFilter(data.optionValue as string)}
          >
            <Option value="all">All Levels</Option>
            <Option value="beginner">Beginner</Option>
            <Option value="intermediate">Intermediate</Option>
            <Option value="advanced">Advanced</Option>
          </Dropdown>
          <Dropdown
            placeholder="Category"
            value={categoryFilter}
            selectedOptions={[categoryFilter]}
            onOptionSelect={(_, data) => setCategoryFilter(data.optionValue as string)}
          >
            <Option value="all">All Categories</Option>
            <Option value="tutorial">Tutorial</Option>
            <Option value="social-media">Social Media</Option>
            <Option value="marketing">Marketing</Option>
            <Option value="educational">Educational</Option>
            <Option value="entertainment">Entertainment</Option>
          </Dropdown>
        </div>
      )}

      {selectedTemplate && (
        <Card className={styles.selectedTemplateCard}>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: tokens.spacingHorizontalM }}>
            <Info24Regular style={{ width: '24px', height: '24px', color: tokens.colorPaletteBlueForeground1, flexShrink: 0 }} />
            <div style={{ flex: 1 }}>
              <Text weight="semibold" size={400}>Selected Template: {selectedTemplate.name}</Text>
              <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS }}>
                {selectedTemplate.description}
              </Text>
              <div style={{ marginTop: tokens.spacingVerticalM }}>
                <Text size={200} weight="semibold">Example Prompt:</Text>
                <Text size={200} style={{ display: 'block', marginTop: tokens.spacingVerticalXS, fontStyle: 'italic' }}>
                  "{selectedTemplate.promptExample}"
                </Text>
              </div>
            </div>
          </div>
        </Card>
      )}

      <div className={styles.templatesGrid}>
        {filteredTemplates.map((template) => (
          <Card
            key={template.id}
            className={`${styles.templateCard} ${template.id === selectedTemplateId ? styles.templateCardSelected : ''}`}
            onClick={() => onTemplateSelect?.(template)}
          >
            <div className={styles.templateHeader}>
              <Video24Regular className={styles.templateIcon} />
              <Badge color={getDifficultyColor(template.difficulty)} appearance="filled">
                {template.difficulty}
              </Badge>
            </div>

            <div className={styles.templateInfo}>
              <Title3>{template.name}</Title3>
              <Text size={300}>{template.description}</Text>
              
              <div style={{ display: 'flex', gap: tokens.spacingHorizontalM, marginTop: tokens.spacingVerticalS }}>
                <Text size={200}>
                  <strong>Duration:</strong> {formatDuration(template.duration)}
                </Text>
                <Text size={200}>
                  <strong>Time:</strong> {template.estimatedTime}
                </Text>
              </div>

              <div className={styles.templateTags}>
                {template.tags.map((tag) => (
                  <Badge key={tag} size="small" appearance="outline">
                    {tag}
                  </Badge>
                ))}
              </div>
            </div>

            <div className={styles.templatePromptExample}>
              <Text size={200} weight="semibold" style={{ display: 'block', marginBottom: tokens.spacingVerticalXS }}>
                Example:
              </Text>
              <Text size={200} style={{ fontStyle: 'italic' }}>
                "{template.promptExample}"
              </Text>
            </div>

            {template.id === selectedTemplateId && (
              <Button
                appearance="primary"
                icon={<Play24Regular />}
                style={{ marginTop: tokens.spacingVerticalM, width: '100%' }}
              >
                Use This Template
              </Button>
            )}
          </Card>
        ))}
      </div>

      {filteredTemplates.length === 0 && (
        <Card style={{ padding: tokens.spacingVerticalXXL, textAlign: 'center' }}>
          <Text>No templates found matching your search criteria.</Text>
        </Card>
      )}
    </div>
  );
}
