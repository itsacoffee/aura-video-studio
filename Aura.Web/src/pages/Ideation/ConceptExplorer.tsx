import {
  makeStyles,
  tokens,
  Text,
  Button,
  Tab,
  TabList,
  Spinner,
} from '@fluentui/react-components';
import {
  ArrowLeftRegular,
  LightbulbRegular,
  BookInformationRegular,
  VideoRegular,
  EditRegular,
} from '@fluentui/react-icons';
import type { FC } from 'react';
import React, { useState, useCallback, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { ConceptCard } from '../../components/ideation/ConceptCard';
import { ConceptRefiner } from '../../components/ideation/ConceptRefiner';
import { QuestionFlow } from '../../components/ideation/QuestionFlow';
import { ResearchPanel } from '../../components/ideation/ResearchPanel';
import { StoryboardScene } from '../../components/ideation/StoryboardScene';
import { ErrorState } from '../../components/Loading';
import {
  ideationService,
  type ConceptIdea,
  type ResearchFinding,
  type StoryboardScene as StoryboardSceneType,
  type ClarifyingQuestion,
} from '../../services/ideationService';

const useStyles = makeStyles({
  container: {
    padding: tokens.spacingVerticalXXL,
    maxWidth: '1400px',
    margin: '0 auto',
    minHeight: '100vh',
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalXL,
    flexWrap: 'wrap',
    gap: tokens.spacingVerticalM,
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalM,
  },
  backButton: {
    minWidth: 'auto',
  },
  headerContent: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  title: {
    fontSize: tokens.fontSizeHero800,
    fontWeight: tokens.fontWeightSemibold,
  },
  subtitle: {
    color: tokens.colorNeutralForeground3,
  },
  headerActions: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    flexWrap: 'wrap',
  },
  tabsContainer: {
    marginBottom: tokens.spacingVerticalXL,
  },
  tabContent: {
    marginTop: tokens.spacingVerticalL,
  },
  loadingState: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXXL,
    gap: tokens.spacingVerticalM,
  },
  conceptPreview: {
    marginBottom: tokens.spacingVerticalXL,
  },
});

type TabValue = 'overview' | 'refine' | 'research' | 'storyboard' | 'questions';

export const ConceptExplorer: FC = () => {
  const styles = useStyles();
  const navigate = useNavigate();
  const { conceptId } = useParams<{ conceptId: string }>();
  const location = useLocation();

  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');
  const [concept, setConcept] = useState<ConceptIdea | null>(location.state?.concept || null);
  const [isLoading, setIsLoading] = useState(!concept);
  const [error, setError] = useState<string | null>(null);

  const [research, setResearch] = useState<ResearchFinding[]>([]);
  const [storyboard, setStoryboard] = useState<StoryboardSceneType[]>([]);
  const [questions, setQuestions] = useState<ClarifyingQuestion[]>([]);
  const [isLoadingData, setIsLoadingData] = useState(false);

  useEffect(() => {
    if (conceptId && !concept) {
      setIsLoading(false);
      setError('Concept not found. Please select a concept from the ideation dashboard.');
    }
  }, [conceptId, concept]);

  const handleRefine = useCallback(
    async (direction: 'expand' | 'simplify' | 'adjust-audience' | 'merge', feedback?: string) => {
      if (!concept) return;

      setIsLoadingData(true);
      setError(null);

      try {
        const response = await ideationService.refineConcept({
          conceptId: concept.conceptId,
          direction,
          feedback,
        });

        if (response.success && response.refinedConcept) {
          setConcept(response.refinedConcept);
        }
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'Failed to refine concept';
        setError(errorMessage);
      } finally {
        setIsLoadingData(false);
      }
    },
    [concept]
  );

  const handleLoadResearch = useCallback(async () => {
    if (!concept) return;

    setIsLoadingData(true);
    setError(null);

    try {
      const response = await ideationService.gatherResearch({
        topic: concept.title,
        depth: 'comprehensive',
      });

      if (response.success && response.findings) {
        setResearch(response.findings);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to gather research';
      setError(errorMessage);
    } finally {
      setIsLoadingData(false);
    }
  }, [concept]);

  const handleLoadStoryboard = useCallback(async () => {
    if (!concept) return;

    setIsLoadingData(true);
    setError(null);

    try {
      const response = await ideationService.generateStoryboard({
        conceptId: concept.conceptId,
        title: concept.title,
        description: concept.description,
        targetDuration: 60,
      });

      if (response.success && response.scenes) {
        setStoryboard(response.scenes);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to generate storyboard';
      setError(errorMessage);
    } finally {
      setIsLoadingData(false);
    }
  }, [concept]);

  const handleLoadQuestions = useCallback(async () => {
    if (!concept) return;

    setIsLoadingData(true);
    setError(null);

    try {
      const response = await ideationService.getClarifyingQuestions({
        projectId: concept.conceptId,
        context: concept.description,
      });

      if (response.success && response.questions) {
        setQuestions(response.questions);
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to generate questions';
      setError(errorMessage);
    } finally {
      setIsLoadingData(false);
    }
  }, [concept]);

  const handleAnswerQuestion = useCallback((questionId: string, answer: string) => {
    // Store answer for later processing
    console.info('Question answered:', { questionId, answer });
  }, []);

  const handleSaveConcept = useCallback((updatedConcept: ConceptIdea) => {
    setConcept(updatedConcept);
  }, []);

  const handleUseForVideo = useCallback(() => {
    if (!concept) return;
    navigate('/create', {
      state: {
        brief: {
          topic: concept.title,
          audience: concept.targetAudience,
          goal: concept.description,
        },
      },
    });
  }, [concept, navigate]);

  useEffect(() => {
    if (selectedTab === 'research' && research.length === 0 && !isLoadingData) {
      handleLoadResearch();
    } else if (selectedTab === 'storyboard' && storyboard.length === 0 && !isLoadingData) {
      handleLoadStoryboard();
    } else if (selectedTab === 'questions' && questions.length === 0 && !isLoadingData) {
      handleLoadQuestions();
    }
  }, [
    selectedTab,
    research.length,
    storyboard.length,
    questions.length,
    isLoadingData,
    handleLoadResearch,
    handleLoadStoryboard,
    handleLoadQuestions,
  ]);

  if (isLoading) {
    return (
      <div className={styles.loadingState}>
        <Spinner size="extra-large" />
        <Text size={400}>Loading concept...</Text>
      </div>
    );
  }

  if (error || !concept) {
    return (
      <div className={styles.container}>
        <ErrorState
          title="Error loading concept"
          message={error || 'Concept not found'}
          onRetry={() => navigate('/ideation')}
          retryLabel="Back to Ideation"
        />
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <Button
            appearance="subtle"
            icon={<ArrowLeftRegular />}
            onClick={() => navigate('/ideation')}
            className={styles.backButton}
            aria-label="Back to ideation"
          />
          <div className={styles.headerContent}>
            <Text className={styles.title}>Concept Explorer</Text>
            <Text className={styles.subtitle}>Explore and refine: {concept.title}</Text>
          </div>
        </div>
        <div className={styles.headerActions}>
          <Button appearance="primary" onClick={handleUseForVideo}>
            Use for Video
          </Button>
        </div>
      </div>

      {selectedTab === 'overview' && (
        <div className={styles.conceptPreview}>
          <ConceptCard concept={concept} onSelect={() => {}} isSelected={false} />
        </div>
      )}

      <div className={styles.tabsContainer}>
        <TabList
          selectedValue={selectedTab}
          onTabSelect={(_, data) => setSelectedTab(data.value as TabValue)}
        >
          <Tab value="overview" icon={<LightbulbRegular />}>
            Overview
          </Tab>
          <Tab value="refine" icon={<EditRegular />}>
            Refine
          </Tab>
          <Tab value="research" icon={<BookInformationRegular />}>
            Research
          </Tab>
          <Tab value="storyboard" icon={<VideoRegular />}>
            Storyboard
          </Tab>
          <Tab value="questions" icon={<BookInformationRegular />}>
            Questions
          </Tab>
        </TabList>
      </div>

      <div className={styles.tabContent}>
        {selectedTab === 'overview' && (
          <div>
            <Text
              size={400}
              weight="semibold"
              as="div"
              style={{ marginBottom: tokens.spacingVerticalM }}
            >
              Concept Details
            </Text>
            <Text size={300} as="div" style={{ lineHeight: tokens.lineHeightBase400 }}>
              Use the tabs above to refine this concept, gather research, generate a storyboard, or
              answer clarifying questions.
            </Text>
          </div>
        )}

        {selectedTab === 'refine' && (
          <ConceptRefiner
            concept={concept}
            onRefine={handleRefine}
            isRefining={isLoadingData}
            onSave={handleSaveConcept}
          />
        )}

        {selectedTab === 'research' && (
          <ResearchPanel
            findings={research}
            isLoading={isLoadingData}
            onRefresh={handleLoadResearch}
          />
        )}

        {selectedTab === 'storyboard' && (
          <StoryboardScene scenes={storyboard} showActions={false} />
        )}

        {selectedTab === 'questions' && (
          <QuestionFlow
            questions={questions}
            onAnswerSubmit={handleAnswerQuestion}
            isLoading={isLoadingData}
          />
        )}
      </div>
    </div>
  );
};

export default ConceptExplorer;
