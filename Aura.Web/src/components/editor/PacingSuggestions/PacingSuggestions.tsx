/**
 * Pacing Suggestions Component
 * Displays AI-driven pacing and rhythm optimization recommendations
 */

import {
  Button,
  Card,
  makeStyles,
  tokens,
  Spinner,
  Badge,
  Divider,
  ProgressBar,
  Dropdown,
  Option,
  Field,
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  Body1,
  Body1Strong,
  Caption1,
  Title3,
} from '@fluentui/react-components';
import {
  ArrowSync24Regular,
  Clock24Regular,
  ChartMultiple24Regular,
  Warning24Regular,
  Lightbulb24Regular,
} from '@fluentui/react-icons';
import React, { useState, useCallback, useEffect } from 'react';
import {
  pacingAnalysisService,
  VideoFormat,
  PacingAnalysisResult,
  VideoRetentionAnalysis,
  Priority,
} from '../../../services/analysis/PacingAnalysisService';
import { Scene } from '../../../types';

interface PacingSuggestionsProps {
  scenes: Scene[];
  audioPath?: string;
  onApplySuggestion?: (sceneIndex: number, newDuration: string) => void;
}

const useStyles = makeStyles({
  container: {
    width: '100%',
    padding: tokens.spacingVerticalM,
  },
  header: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: tokens.spacingVerticalM,
  },
  section: {
    marginTop: tokens.spacingVerticalL,
  },
  warningBox: {
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderRadius: tokens.borderRadiusMedium,
    marginTop: tokens.spacingVerticalM,
  },
  sceneCard: {
    padding: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    borderRadius: tokens.borderRadiusMedium,
  },
});

const PacingSuggestions: React.FC<PacingSuggestionsProps> = ({
  scenes,
  audioPath,
  onApplySuggestion,
}) => {
  const styles = useStyles();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedFormat, setSelectedFormat] = useState<VideoFormat>(VideoFormat.Explainer);
  const [pacingAnalysis, setPacingAnalysis] = useState<PacingAnalysisResult | null>(null);
  const [retentionAnalysis, setRetentionAnalysis] = useState<VideoRetentionAnalysis | null>(null);

  const analyzePacing = useCallback(async () => {
    if (scenes.length === 0) {
      setError('No scenes available for analysis');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const [pacing, retention] = await Promise.all([
        pacingAnalysisService.analyzePacing(scenes, audioPath || null, selectedFormat),
        pacingAnalysisService.predictRetention(scenes, audioPath || null, selectedFormat),
      ]);

      setPacingAnalysis(pacing);
      setRetentionAnalysis(retention);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to analyze pacing');
      console.error('Pacing analysis error:', err);
    } finally {
      setLoading(false);
    }
  }, [scenes, audioPath, selectedFormat]);

  useEffect(() => {
    // Auto-analyze when scenes change
    if (scenes.length > 0) {
      analyzePacing();
    }
  }, [scenes, analyzePacing]);

  const getPriorityColor = (
    priority: Priority
  ): 'danger' | 'warning' | 'informative' | 'subtle' => {
    switch (priority) {
      case Priority.Critical:
      case Priority.High:
        return 'danger';
      case Priority.Medium:
        return 'warning';
      case Priority.Low:
        return 'informative';
      default:
        return 'subtle';
    }
  };

  const formatDuration = (durationStr: string): string => {
    const seconds = pacingAnalysisService.durationToSeconds(durationStr);
    if (seconds < 60) {
      return `${seconds.toFixed(1)}s`;
    }
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}m ${secs}s`;
  };

  if (scenes.length === 0) {
    return (
      <Card style={{ padding: tokens.spacingVerticalM }}>
        <Body1>Add scenes to get pacing suggestions</Body1>
      </Card>
    );
  }

  return (
    <div className={styles.container}>
      <Card>
        <div style={{ padding: tokens.spacingVerticalM }}>
          <div className={styles.header}>
            <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
              <Clock24Regular />
              <Title3>Pacing Optimization</Title3>
            </div>
            <Field label="Video Format">
              <Dropdown
                value={selectedFormat}
                selectedOptions={[selectedFormat]}
                onOptionSelect={(_e, data) => setSelectedFormat(data.optionValue as VideoFormat)}
              >
                <Option value={VideoFormat.Explainer}>Explainer</Option>
                <Option value={VideoFormat.Tutorial}>Tutorial</Option>
                <Option value={VideoFormat.Vlog}>Vlog</Option>
                <Option value={VideoFormat.Review}>Review</Option>
                <Option value={VideoFormat.Educational}>Educational</Option>
                <Option value={VideoFormat.Entertainment}>Entertainment</Option>
              </Dropdown>
            </Field>
          </div>

          <Button
            appearance="primary"
            onClick={analyzePacing}
            disabled={loading}
            icon={loading ? <Spinner size="tiny" /> : <ArrowSync24Regular />}
          >
            {loading ? 'Analyzing...' : 'Analyze Pacing'}
          </Button>

          {error && (
            <div className={styles.warningBox}>
              <div
                style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}
              >
                <Warning24Regular />
                <Body1Strong>{error}</Body1Strong>
              </div>
            </div>
          )}

          {pacingAnalysis && (
            <>
              <Divider
                style={{
                  marginTop: tokens.spacingVerticalL,
                  marginBottom: tokens.spacingVerticalL,
                }}
              />

              <div className={styles.section}>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: tokens.spacingHorizontalS,
                    marginBottom: tokens.spacingVerticalS,
                  }}
                >
                  <ChartMultiple24Regular />
                  <Body1Strong>Engagement Score</Body1Strong>
                </div>
                <ProgressBar
                  value={pacingAnalysis.engagementScore / 100}
                  thickness="large"
                  style={{ marginBottom: tokens.spacingVerticalS }}
                />
                <Body1>{pacingAnalysis.engagementScore.toFixed(1)}%</Body1>
              </div>

              <div className={styles.section}>
                <Caption1>
                  Optimal Duration: {formatDuration(pacingAnalysis.optimalDuration)}
                </Caption1>
              </div>

              <div className={styles.section}>
                <Body1Strong>Narrative Structure</Body1Strong>
                <Body1>{pacingAnalysis.narrativeArcAssessment}</Body1>
              </div>

              {pacingAnalysis.warnings.length > 0 && (
                <div className={styles.warningBox}>
                  <div
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: tokens.spacingHorizontalS,
                      marginBottom: tokens.spacingVerticalS,
                    }}
                  >
                    <Warning24Regular />
                    <Body1Strong>Pacing Warnings</Body1Strong>
                  </div>
                  {pacingAnalysis.warnings.map((warning, index) => (
                    <Caption1 key={index} block style={{ marginTop: tokens.spacingVerticalXS }}>
                      • {warning}
                    </Caption1>
                  ))}
                </div>
              )}

              {pacingAnalysis.sceneRecommendations.length > 0 && (
                <div className={styles.section}>
                  <Accordion collapsible>
                    <AccordionItem value="recommendations">
                      <AccordionHeader>
                        <Body1Strong>
                          Scene Recommendations ({pacingAnalysis.sceneRecommendations.length})
                        </Body1Strong>
                      </AccordionHeader>
                      <AccordionPanel>
                        {pacingAnalysis.sceneRecommendations.map((rec) => (
                          <div key={rec.sceneIndex} className={styles.sceneCard}>
                            <div
                              style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                marginBottom: tokens.spacingVerticalXS,
                              }}
                            >
                              <Body1Strong>Scene {rec.sceneIndex + 1}</Body1Strong>
                              <Badge appearance="tint" color="informative">
                                Complexity: {(rec.complexityScore * 100).toFixed(0)}%
                              </Badge>
                            </div>
                            <Caption1 block>
                              Importance: {(rec.importanceScore * 100).toFixed(0)}%
                            </Caption1>
                            <Caption1 block style={{ marginTop: tokens.spacingVerticalXS }}>
                              {rec.reasoning}
                            </Caption1>
                            <Caption1 block style={{ marginTop: tokens.spacingVerticalXS }}>
                              Current: {formatDuration(rec.currentDuration)} → Recommended:{' '}
                              {formatDuration(rec.recommendedDuration)}
                            </Caption1>
                            {rec.recommendedDuration && (
                              <div style={{ marginTop: tokens.spacingVerticalS }}>
                                <Button
                                  size="small"
                                  onClick={() =>
                                    onApplySuggestion?.(rec.sceneIndex, rec.recommendedDuration)
                                  }
                                >
                                  Apply Duration: {formatDuration(rec.recommendedDuration)}
                                </Button>
                              </div>
                            )}
                          </div>
                        ))}
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                </div>
              )}

              {retentionAnalysis && (
                <div className={styles.section}>
                  <Accordion collapsible>
                    <AccordionItem value="retention">
                      <AccordionHeader>
                        <Body1Strong>Retention Analysis</Body1Strong>
                      </AccordionHeader>
                      <AccordionPanel>
                        <Caption1 block>
                          Overall Retention Score:{' '}
                          {retentionAnalysis.retentionPrediction.overallRetentionScore.toFixed(1)}%
                        </Caption1>
                        <Caption1 block style={{ marginTop: tokens.spacingVerticalXS }}>
                          Average Engagement:{' '}
                          {retentionAnalysis.attentionCurve.averageEngagement.toFixed(1)}%
                        </Caption1>
                        {retentionAnalysis.retentionPrediction.highDropRiskPoints.length > 0 && (
                          <>
                            <Body1Strong style={{ marginTop: tokens.spacingVerticalM }}>
                              High Risk Drop Points
                            </Body1Strong>
                            {retentionAnalysis.retentionPrediction.highDropRiskPoints.map(
                              (point, index) => (
                                <Caption1
                                  key={index}
                                  block
                                  style={{ marginTop: tokens.spacingVerticalXS }}
                                >
                                  • {point}
                                </Caption1>
                              )
                            )}
                          </>
                        )}
                        {retentionAnalysis.recommendations.length > 0 && (
                          <>
                            <Body1Strong style={{ marginTop: tokens.spacingVerticalM }}>
                              <Lightbulb24Regular
                                style={{
                                  verticalAlign: 'middle',
                                  marginRight: tokens.spacingHorizontalXS,
                                }}
                              />
                              Recommendations
                            </Body1Strong>
                            {retentionAnalysis.recommendations.map((recommendation, index) => (
                              <div key={index} style={{ marginTop: tokens.spacingVerticalS }}>
                                <Body1Strong>{recommendation.title}</Body1Strong>
                                <Caption1 block style={{ marginTop: tokens.spacingVerticalXS }}>
                                  {recommendation.description}
                                </Caption1>
                                <Badge
                                  appearance="tint"
                                  color={getPriorityColor(recommendation.priority)}
                                  size="small"
                                >
                                  {Priority[recommendation.priority]}
                                </Badge>
                              </div>
                            ))}
                          </>
                        )}
                      </AccordionPanel>
                    </AccordionItem>
                  </Accordion>
                </div>
              )}
            </>
          )}
        </div>
      </Card>
    </div>
  );
};

export default PacingSuggestions;
