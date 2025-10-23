import React, { useState } from 'react';
import { makeStyles, tokens, Text, Button, Spinner } from '@fluentui/react-components';
import { LightbulbRegular, LightbulbFilamentRegular } from '@fluentui/react-icons';
import { BrainstormInput, BrainstormOptions } from '../../components/ideation/BrainstormInput';
import { ConceptCard } from '../../components/ideation/ConceptCard';
import {
  ideationService,
  type ConceptIdea,
  type BrainstormRequest,
} from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalXXL,
  },
  title: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
    marginBottom: tokens.spacingVerticalM,
  },
  icon: {
    fontSize: '32px',
    color: tokens.colorBrandForeground1,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
    maxWidth: '800px',
  },
  content: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXXL,
  },
  conceptsSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  conceptsHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  conceptsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(400px, 1fr))',
    gap: tokens.spacingVerticalL,
  },
  emptyState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: '64px',
  },
  errorContainer: {
    padding: tokens.spacingVerticalL,
    backgroundColor: tokens.colorPaletteRedBackground1,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorPaletteRedForeground1,
  },
});

export const IdeationDashboard: React.FC = () => {
  const styles = useStyles();
  const [concepts, setConcepts] = useState<ConceptIdea[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [originalTopic, setOriginalTopic] = useState<string>('');

  const handleBrainstorm = async (topic: string, options: BrainstormOptions) => {
    setLoading(true);
    setError(null);
    setOriginalTopic(topic);

    try {
      const request: BrainstormRequest = {
        topic,
        ...options,
      };

      const response = await ideationService.brainstorm(request);
      setConcepts(response.concepts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate concepts');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectConcept = (_concept: ConceptIdea) => {
    // TODO: Navigate to concept explorer or show detail modal
  };

  const handleExpandConcept = (_concept: ConceptIdea) => {
    // TODO: Navigate to detailed view or show modal
  };

  const handleRefresh = () => {
    if (originalTopic) {
      handleBrainstorm(originalTopic, {});
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.title}>
          <LightbulbRegular className={styles.icon} />
          <Text size={800} weight="bold">
            AI Ideation & Brainstorming
          </Text>
        </div>
        <Text className={styles.subtitle} size={400}>
          Transform your ideas into fully-fleshed video concepts. Enter a topic and let AI generate
          creative variations with different storytelling approaches, target audiences, and
          production angles.
        </Text>
      </div>

      <div className={styles.content}>
        <BrainstormInput onBrainstorm={handleBrainstorm} loading={loading} />

        {error && (
          <div className={styles.errorContainer}>
            <Text weight="semibold">Error:</Text>
            <Text>{error}</Text>
          </div>
        )}

        {concepts.length > 0 && (
          <div className={styles.conceptsSection}>
            <div className={styles.conceptsHeader}>
              <Text size={600} weight="semibold">
                {concepts.length} Creative Concepts for &quot;{originalTopic}&quot;
              </Text>
              <Button appearance="subtle" onClick={handleRefresh}>
                Generate More
              </Button>
            </div>

            <div className={styles.conceptsGrid}>
              {concepts.map((concept) => (
                <ConceptCard
                  key={concept.conceptId}
                  concept={concept}
                  onSelect={handleSelectConcept}
                  onExpand={handleExpandConcept}
                />
              ))}
            </div>
          </div>
        )}

        {!loading && concepts.length === 0 && !error && (
          <div className={styles.emptyState}>
            <LightbulbFilamentRegular className={styles.emptyIcon} />
            <Text size={500} weight="semibold">
              Ready to brainstorm?
            </Text>
            <Text>Enter a video topic above to get started with AI-powered concept generation</Text>
          </div>
        )}

        {loading && (
          <div className={styles.emptyState}>
            <Spinner size="extra-large" />
            <Text size={500} weight="semibold">
              Generating creative concepts...
            </Text>
            <Text>Our AI is analyzing multiple creative angles for your topic</Text>
          </div>
        )}
      </div>
    </div>
  );
};
