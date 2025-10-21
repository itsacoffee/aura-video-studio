/**
 * Editing Assistant Component
 * Main AI editing intelligence panel for timeline optimization
 */

import React, { useState, useCallback } from 'react';
import {
  Button,
  Card,
  makeStyles,
  tokens,
  Spinner,
  Divider,
  TabList,
  Tab,
  TabValue,
  Body1,
  Body1Strong,
  Caption1,
  Title3,
  MessageBar,
  MessageBarBody,
  MessageBarTitle,
} from '@fluentui/react-components';
import {
  Cut24Regular,
  DocumentBulletList24Regular,
  SlideTransition24Regular,
  Eye24Regular,
  Shield24Regular,
  Sparkle24Regular,
} from '@fluentui/react-icons';
import {
  analyzeTimeline,
  TimelineAnalysisResult,
} from '../../../services/editingIntelligenceService';
import { CutPointPanel } from './CutPointPanel';
import { PacingPanel } from './PacingPanel';
import { TransitionPanel } from './TransitionPanel';
import { EngagementPanel } from './EngagementPanel';
import { QualityPanel } from './QualityPanel';

interface EditingAssistantProps {
  jobId: string;
  onApplySuggestion?: (type: string, data: any) => void;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    padding: tokens.spacingVerticalM,
    gap: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  content: {
    flex: 1,
    overflowY: 'auto',
  },
  analysisCard: {
    marginBottom: tokens.spacingVerticalM,
  },
  scoreContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    marginTop: tokens.spacingVerticalM,
  },
  scoreCard: {
    flex: 1,
    padding: tokens.spacingVerticalM,
    textAlign: 'center',
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  loadingContainer: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    padding: tokens.spacingVerticalXXL,
    gap: tokens.spacingVerticalL,
  },
});

export const EditingAssistant: React.FC<EditingAssistantProps> = ({
  jobId,
  onApplySuggestion,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [analysis, setAnalysis] = useState<TimelineAnalysisResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [selectedTab, setSelectedTab] = useState<TabValue>('overview');

  const runAnalysis = useCallback(async () => {
    if (!jobId) return;

    setLoading(true);
    setError(null);

    try {
      const result = await analyzeTimeline({
        jobId,
        includeCutPoints: true,
        includePacing: true,
        includeEngagement: true,
        includeQuality: true,
      });
      setAnalysis(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze timeline');
    } finally {
      setLoading(false);
    }
  }, [jobId]);

  const renderOverview = () => {
    if (!analysis) return null;

    const cutPointCount = analysis.cutPoints?.length || 0;
    const qualityIssueCount = analysis.qualityIssues?.length || 0;
    const criticalIssues = analysis.qualityIssues?.filter(i => i.severity === 'Critical').length || 0;

    return (
      <div>
        <Card className={styles.analysisCard}>
          <Title3>Analysis Overview</Title3>
          
          <div className={styles.scoreContainer}>
            <div className={styles.scoreCard}>
              <Sparkle24Regular />
              <Body1Strong>
                {Math.round((analysis.pacingAnalysis?.overallEngagementScore || 0) * 100)}%
              </Body1Strong>
              <Caption1>Engagement Score</Caption1>
            </div>
            
            <div className={styles.scoreCard}>
              <Cut24Regular />
              <Body1Strong>{cutPointCount}</Body1Strong>
              <Caption1>Cut Suggestions</Caption1>
            </div>
            
            <div className={styles.scoreCard}>
              <Shield24Regular />
              <Body1Strong>{qualityIssueCount}</Body1Strong>
              <Caption1>Quality Issues</Caption1>
            </div>
          </div>
        </Card>

        {criticalIssues > 0 && (
          <MessageBar intent="error">
            <MessageBarBody>
              <MessageBarTitle>Critical Issues Found</MessageBarTitle>
              {criticalIssues} critical quality issues must be resolved before rendering.
            </MessageBarBody>
          </MessageBar>
        )}

        <Card className={styles.analysisCard}>
          <Body1Strong>Recommendations</Body1Strong>
          <Divider />
          {analysis.generalRecommendations.map((rec, idx) => (
            <Body1 key={idx} style={{ marginTop: tokens.spacingVerticalS }}>
              • {rec}
            </Body1>
          ))}
        </Card>
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <Title3>AI Editing Assistant</Title3>
        <Button
          appearance="primary"
          icon={<Sparkle24Regular />}
          onClick={runAnalysis}
          disabled={loading || !jobId}
        >
          {loading ? 'Analyzing...' : 'Analyze Timeline'}
        </Button>
      </div>

      {error && (
        <MessageBar intent="error">
          <MessageBarBody>{error}</MessageBarBody>
        </MessageBar>
      )}

      {loading && (
        <div className={styles.loadingContainer}>
          <Spinner size="huge" label="Analyzing timeline..." />
        </div>
      )}

      {!loading && analysis && (
        <>
          <TabList selectedValue={selectedTab} onTabSelect={(_, data) => setSelectedTab(data.value)}>
            <Tab icon={<DocumentBulletList24Regular />} value="overview">Overview</Tab>
            <Tab icon={<Cut24Regular />} value="cuts">Cut Points</Tab>
            <Tab icon={<DocumentBulletList24Regular />} value="pacing">Pacing</Tab>
            <Tab icon={<SlideTransition24Regular />} value="transitions">Transitions</Tab>
            <Tab icon={<Eye24Regular />} value="engagement">Engagement</Tab>
            <Tab icon={<Shield24Regular />} value="quality">Quality</Tab>
          </TabList>

          <div className={styles.content}>
            {selectedTab === 'overview' && renderOverview()}
            {selectedTab === 'cuts' && (
              <CutPointPanel
                cutPoints={analysis.cutPoints || []}
                onApply={onApplySuggestion}
              />
            )}
            {selectedTab === 'pacing' && (
              <PacingPanel
                analysis={analysis.pacingAnalysis}
              />
            )}
            {selectedTab === 'transitions' && (
              <TransitionPanel
                jobId={jobId}
                onApply={onApplySuggestion}
              />
            )}
            {selectedTab === 'engagement' && (
              <EngagementPanel
                curve={analysis.engagementAnalysis}
                jobId={jobId}
              />
            )}
            {selectedTab === 'quality' && (
              <QualityPanel
                issues={analysis.qualityIssues || []}
                jobId={jobId}
              />
            )}
          </div>
        </>
      )}
    </div>
  );
};
